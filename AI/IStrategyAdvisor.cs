// INTERFAZ DEL ESTRATEGA: CUALQUIER PROVEEDOR DE LLM DEBE CUMPLIRLA
// STRATEGIST INTERFACE: ANY LLM PROVIDER MUST IMPLEMENT IT

using System.Threading;
using System.Threading.Tasks;

namespace PenguinWars.AI;

// Permite cambiar de proveedor (Ollama, una API en la nube, etc.) sin tocar la IA.
// Allows switching providers (Ollama, a cloud API, etc.) without touching the AI.
public interface IStrategyAdvisor
{
    // Nombre del modelo o proveedor, para mostrar en pantalla.
    // Model or provider name, to display on screen.
    string Name { get; }

    // Precarga el modelo para que la primera respuesta sea más rápida.
    // Preloads the model so the first response is faster.
    void WarmUp();

    // Pide una decisión. Devuelve null si falla o la respuesta no sirve.
    // Requests a decision. Returns null if it fails or the response is unusable.
    Task<StrategyDecision> AdviseAsync(StrategyContext context, CancellationToken cancellationToken);
}