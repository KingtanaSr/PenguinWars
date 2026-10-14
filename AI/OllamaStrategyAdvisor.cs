// ESTRATEGA QUE CONSULTA UN MODELO LOCAL CON OLLAMA
// STRATEGIST THAT QUERIES A LOCAL MODEL THROUGH OLLAMA

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PenguinWars.Core;

namespace PenguinWars.AI;

public class OllamaStrategyAdvisor : IStrategyAdvisor
{
    // Direcciones locales de la API de Ollama.
    // Local addresses of the Ollama API.
    private const string ChatEndpoint = "http://localhost:11434/api/chat";
    private const string GenerateEndpoint = "http://localhost:11434/api/generate";

    // Modelo por defecto; se puede cambiar con la variable de entorno PENGUINWARS_OLLAMA_MODEL.
    // Default model; it can be changed with the PENGUINWARS_OLLAMA_MODEL environment variable.
    private const string DefaultModel = "qwen2.5:1.5b";

    // Largo máximo de la frase que se muestra en el globo.
    // Maximum length of the line shown in the bubble.
    private const int MaxTauntLength = 70;

    // Un solo HttpClient para todo el juego (crear uno por consulta agota las conexiones).
    // A single HttpClient for the whole game (creating one per request exhausts connections).
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private readonly string _model;

    public string Name => _model;

    public OllamaStrategyAdvisor()
    {
        string configured = Environment.GetEnvironmentVariable("PENGUINWARS_OLLAMA_MODEL");
        _model = string.IsNullOrWhiteSpace(configured) ? DefaultModel : configured;
    }

    // Lanza la precarga sin esperar el resultado.
    // Starts the preload without waiting for the result.
    public void WarmUp() => _ = WarmUpAsync();

    private async Task WarmUpAsync()
    {
        try
        {
            // Una petición sin prompt hace que Ollama cargue el modelo en memoria.
            // A request without a prompt makes Ollama load the model into memory.
            using HttpResponseMessage response = await Http.PostAsJsonAsync(GenerateEndpoint,
                new { model = _model, keep_alive = "30m" });
        }
        catch (Exception)
        {
            // Si Ollama no está corriendo, se ignora: la IA clásica sigue funcionando.
            // If Ollama is not running, it is ignored: the classic AI keeps working.
        }
    }

    public async Task<StrategyDecision> AdviseAsync(StrategyContext context, CancellationToken cancellationToken)
    {
        try
        {
            var request = new
            {
                model = _model,
                stream = false,

                // Obliga al modelo a responder con JSON válido.
                // Forces the model to answer with valid JSON.
                format = "json",

                // Mantiene el modelo cargado 30 minutos entre consultas.
                // Keeps the model loaded for 30 minutes between requests.
                keep_alive = "30m",

                options = new { temperature = 0.7 },
                messages = new[]
                {
                    new { role = "system", content = BuildSystemPrompt(context.Team) },
                    new { role = "user", content = context.Situation + "\nResponde solo con el JSON." }
                }
            };

            using HttpResponseMessage response = await Http.PostAsJsonAsync(ChatEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            // La respuesta de Ollama trae el texto del modelo en message.content.
            // Ollama's response contains the model's text in message.content.
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            string content = document.RootElement.GetProperty("message").GetProperty("content").GetString();

            return ParseDecision(content);
        }
        catch (Exception)
        {
            // Sin conexión, tiempo agotado o formato inesperado: la IA clásica decidirá.
            // No connection, timeout or unexpected format: the classic AI will decide.
            return null;
        }
    }

    // Instrucciones del rol, la personalidad del equipo y el formato de salida.
    // Instructions for the role, the team's personality and the output format.
    private static string BuildSystemPrompt(Team team)
    {
        string personality = team == Team.Aliens
            ? "Eres el estratega de los pingüinos alienígenas que invadieron la Tierra tras estrellar su nave en el hielo. Hablas de forma amenazante y extraterrestre."
            : "Eres el estratega de los pingüinos terrestres (policía, samurái y cyber) que defienden la Tierra de los invasores alienígenas. Hablas de forma heroica.";

        return personality +
            " Estás en un juego de artillería por turnos parecido a Worms." +
            " En cada turno eliges UNA opción de la lista que te dan." +
            " Consejos: prioriza enemigos con poca vida que puedas eliminar; los enemigos cercanos son más fáciles de acertar; cúrate solo si tu vida es baja." +
            " Además, escribe una frase corta y divertida en español (máximo 60 caracteres, sin emojis) que tu pingüino le dice al rival." +
            " Responde SOLO con un JSON con este formato: {\"choice\": \"ID de la opción\", \"taunt\": \"frase\"}.";
    }

    // Lee el JSON del modelo de forma defensiva: cualquier error devuelve null.
    // Reads the model's JSON defensively: any error returns null.
    private static StrategyDecision ParseDecision(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("choice", out JsonElement choice) || choice.ValueKind != JsonValueKind.String)
                return null;

            string taunt = root.TryGetProperty("taunt", out JsonElement tauntElement) && tauntElement.ValueKind == JsonValueKind.String
                ? Sanitize(tauntElement.GetString())
                : null;

            return new StrategyDecision(choice.GetString(), taunt);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Deja solo caracteres que la fuente puede dibujar y recorta frases largas.
    // Keeps only characters the font can draw and trims long lines.
    private static string Sanitize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        string clean = new string(text.Where(c => (c >= 32 && c <= 126) || (c >= 160 && c <= 255)).ToArray()).Trim();

        if (clean.Length > MaxTauntLength)
            clean = clean[..(MaxTauntLength - 3)] + "...";

        return clean.Length > 0 ? clean : null;
    }
}