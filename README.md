# 🐧 Penguin Wars

A turn-based 2D artillery game inspired by **Worms**, built from scratch in **C#** with **MonoGame**.

Two teams of penguins face off on a snowy battlefield: the **Earth Penguins** defending their home, and the **Alien Penguins** whose spaceship crash-landed on the ice. The player picks a side, and an AI controls the opposing team.

> **Status:** Work in progress. This is a personal learning project focused on C#, game architecture, game physics and game AI.

---

## Lore

An alien spaceship has crashed in the middle of a frozen wasteland. Its crew, a squad of alien penguins with strange technology, is not here in peace. Three Earth penguins stand in their way: a police officer, a samurai and a cyber penguin. The wreckage of the ship is scattered across the battlefield, and only one team will leave the ice standing.

## Teams

| Earth Penguins (Heroes) | Alien Penguins |
|---|---|
| 👮 Police Penguin | 👽 Alien Penguin A |
| 🗡️ Samurai Penguin | 👽 Alien Penguin B |
| 🤖 Cyber Penguin | 👽 Alien Penguin C |

Each team has **3 members**. Earth penguins use earthly weapons, while alien penguins use alien technology.

## Gameplay

- **Turn-based:** on each turn, one member of the active team can move and act.
- A turn ends when the active penguin **uses an item, fires a weapon or passes**.
- **Destructible terrain:** explosions carve the snowy landscape.
- **Projectile physics:** shots are affected by angle, power, gravity and wind.
- **Health bars** for every penguin.
- **Medkits** scattered across the map can be picked up and used.
- The match ends when **all members of a team are eliminated** or a team **surrenders**.

## Features & Roadmap

- [x] Project setup with MonoGame and .NET
- [x] Screen management (menu, gameplay) and input handling
- [x] Main menu with team selection
- [ ] Destructible pixel-based terrain
- [ ] Penguin movement, gravity and collisions
- [ ] Health bars and damage system
- [ ] Weapons, projectile physics, wind and explosions
- [ ] Turn system, teams and win/surrender conditions
- [ ] Items: medkits, earth weapons and alien weapons
- [ ] AI-controlled opponent team
- [ ] Snowy map with spaceship wreckage, sprites, sound and polish

## Game AI (planned)

The opposing team will be controlled by an AI that:
- **Simulates candidate shots** (different angles and power levels, accounting for wind) and picks the one that lands closest to its target.
- Chooses **which target and which weapon** to use based on distance and remaining health.
- Supports **difficulty levels** by adding controlled randomness to its aim.

## Tech Stack

- **Language:** C#
- **Framework:** [MonoGame](https://monogame.net/) (DesktopGL)
- **Runtime:** .NET SDK 9 or later
- **Editor:** Visual Studio Code with C# Dev Kit
- **Version control:** Git & GitHub

## Architecture

The game is built around MonoGame's **game loop** (`Update` for logic, `Draw` for rendering, about 60 times per second):

- **`ScreenManager`:** switches between screens (menu, gameplay, game over) using a state pattern. Each screen inherits from an abstract `Screen` class.
- **`InputManager`:** stores the keyboard and mouse state of the current and previous frames, allowing the game to distinguish between a key being *held* and a key being *just pressed*.
- **Content Pipeline:** fonts, sprites and sounds are processed at build time through `Content.mgcb`.

```
PenguinWars/
├── Content/        # Game assets (fonts, sprites, sounds)
├── Core/           # Engine-level classes (screens, input, teams)
├── Screens/        # Game screens (menu, gameplay)
├── Game1.cs        # Main game class and game loop
└── Program.cs      # Entry point
```

## Getting Started

### Prerequisites
- [.NET SDK 9 or later](https://dotnet.microsoft.com/download)
- MonoGame templates:
```
  dotnet new install MonoGame.Templates.CSharp
```

### Run the game
```
git clone https://github.com/KingtanaSr/PenguinWars.git
cd PenguinWars
dotnet run
```

## ontrols (current)

| Key | Action |
|---|---|
| ↑ / ↓ | Navigate menu |
| Enter | Select option |
| Esc | Return to menu |

*Gameplay controls will be added as features are implemented.*

## Author

**Josué Quintana Serrano**, Computer Engineering student at Tecnológico de Costa Rica (TEC).

- GitHub: [KingtanaSr](https://github.com/KingtanaSr)
- LinkedIn: [Josué Quintana](https://www.linkedin.com/in/josué-quintana-a9520827b)
