# 🐧 Penguin Wars

A turn-based 2D artillery game inspired by **Worms**, built from scratch in **C#** with **MonoGame**.

Two teams of penguins face off on a snowy battlefield: the **Earth Penguins** defending their home, and the **Alien Penguins** whose spaceship crash-landed on the ice. The player picks a side, and an AI controls the opposing team.

> **Status:** Work in progress (Phase 5 of 9 completed). This is a personal learning project focused on C#, game architecture, game physics and game AI.

---

![Penguin Wars concept art](docs/concept-art.jpg)

## Lore

An alien spaceship has crashed in the middle of a frozen wasteland. Its crew, a squad of alien penguins with strange technology, is not here in peace. Three Earth penguins stand in their way: a police officer, a samurai and a cyber penguin. The wreckage of the ship is scattered across the battlefield, and only one team will leave the ice standing.

## Teams

| Earth Penguins (Heroes) | Alien Penguins |
|---|---|
| 👮 Police | 👽 Commander |
| 🗡️ Samurai | 👽 Scout |
| 🤖 Cyber | 👽 Brute |

Each team has **3 members**. Earth penguins use earthly weapons, while alien penguins use alien technology.

## Weapons & Items

| Earth Weapons | Alien Weapons | Items |
|---|---|---|
| Bazooka | Plasma Rifle | Medkit |
| Grenade | Gravity Grenade | Ammo Crate |
| Shotgun | Ray Gun | |

## Map

**Arctic Wreck:** a frozen landscape with the remains of the crashed alien spaceship in its central valley.

## Gameplay

- **Turn-based:** on each turn, one member of the active team can move and act.
- A turn ends when the active penguin **uses an item, fires a weapon or passes**.
- **Destructible terrain:** explosions carve the snowy landscape.
- **Projectile physics:** shots are affected by angle, power, gravity and wind.
- **Health bars** for every penguin, plus fall damage and drowning in icy water.
- **Medkits and ammo crates** scattered across the map can be picked up and used.
- The match ends when **all members of a team are eliminated** or a team **surrenders**.

## Development Progress

| Phase | Description | Status |
|---|---|---|
| 1 | Project setup | ✅ Done |
| 2 | Game structure: screens, input and menu | ✅ Done |
| 3 | Destructible terrain | ✅ Done |
| 4 | Penguins: movement, physics and health | ✅ Done |
| 5 | Weapons, projectiles, wind and explosions | ✅ Done |
| 6 | Turn system, teams and win/surrender conditions | 🚧 Next |
| 7 | Items: medkits, ammo crates and weapon sets | ⏳ Planned |
| 8 | AI-controlled opponent team | ⏳ Planned |
| 9 | Art, sound, map wreckage and polish | ⏳ Planned |

### ✅ Phase 1: Project setup
- MonoGame DesktopGL project created with the .NET CLI.
- Git repository and .NET `.gitignore`.

### ✅ Phase 2: Game structure
- `ScreenManager` with an abstract `Screen` class (state pattern) to switch between screens.
- `InputManager` that tracks the current and previous frame to detect *held* vs *just pressed* keys.
- Main menu with team selection (Earth or Alien penguins).
- Custom sprite font with support for Spanish characters.

### ✅ Phase 3: Destructible terrain
- Pixel-based terrain stored as a color array: every pixel is either solid or air.
- Procedural generation by layering sine waves with random phases, using a seed.
- Central valley reserved for the spaceship wreckage.
- Depth-based coloring: snow on the surface, light ice and deep ice below.
- Circular explosions that carve craters and scorch their borders.
- Texture updates only when the terrain changes (dirty flag pattern).

### ✅ Phase 4: Penguins
- Six penguins (3 per team) spawned on opposite sides of the map.
- Pixel-perfect collisions against the terrain mask.
- Walking with automatic climbing of small steps and snapping to slopes.
- Jumping, gravity and airborne movement with sub-pixel accumulation.
- Fall damage, drowning when falling off the map, and tombstones.
- Explosion damage and knockback based on distance.
- Health bars, names and team colors above each penguin.

### ✅ Phase 5: Weapons
- Mouse aiming: the penguin faces the cursor and an aim line shows the shot direction.
- Charged shots: holding the left button fills the power (shown on the aim dots and HUD); releasing it fires, and reaching full power fires automatically.
- Projectiles with parabolic trajectories using Euler integration, affected by gravity and wind.
- Sub-stepping (pixel-by-pixel movement) to prevent fast projectiles from tunneling through terrain.
- Random wind that changes after every shot, with an on-screen indicator.
- Explosions that carve the terrain, damage and knock back penguins, with a fading flash effect.
- Data-driven weapons: each weapon only defines values (speed, gravity, wind, radius, damage), so new weapons need no new logic.
- First weapons: **Bazooka** (Earth team) and **Plasma Rifle** (Alien team).
- One projectile at a time: movement and actions are locked while a shot is in flight.

| Weapon | Speed | Gravity | Wind | Explosion radius | Max damage |
|---|---|---|---|---|---|
| Bazooka | 750 | 100% | 100% | 40 px | 45 |
| Plasma Rifle | 900 | 60% | 30% | 30 px | 35 |

### 🚧 Phase 6: Turn system (next)
- Turns alternating between teams, rotating through each team's members.
- Ending a turn by firing or passing, with a turn timer.
- Wind changing every turn.
- Win condition when a team is eliminated, surrender option and game over screen.

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
- **`InputManager`:** stores the keyboard and mouse state of the current and previous frames, allowing the game to distinguish between a key being *held*, *just pressed* and *just released*.
- **`Terrain`:** pixel mask for collisions and destruction, rendered as a texture that is only re-uploaded when it changes.
- **`Penguin`:** encapsulates movement, physics, health and rendering of each character. Health and position can only be modified through its own methods.
- **`Weapon`:** immutable weapon definitions (data only), separated from the behavior of `Projectile`.
- **`Projectile`:** simulates the flight of a shot and detects hits against the terrain and penguins.
- **`Explosion`:** short-lived visual effect that removes itself when finished.
- **`Wind`:** random horizontal acceleration applied to projectiles.
- **Content Pipeline:** fonts, sprites and sounds are processed at build time through `Content.mgcb`.

```
PenguinWars/
├── Content/        # Game assets (fonts, sprites, sounds)
├── Combat/         # Weapons, projectiles and explosions
├── Core/           # Engine-level classes (screens, input, teams)
├── Entities/       # Game characters (penguins and their profiles)
├── Screens/        # Game screens (menu, gameplay)
├── World/          # Destructible terrain and wind
├── docs/           # Concept art and documentation images
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

## Controls (current)

| Input | Action |
|---|---|
| ↑ / ↓ | Navigate menu |
| Enter | Select option (menu) |
| A / D | Walk left / right |
| Space | Jump |
| Mouse | Aim |
| Hold left click | Charge shot power |
| Release left click | Fire |
| Esc | Return to menu |

**Temporary test controls** (will be replaced by the turn system):

| Key | Action |
|---|---|
| Tab | Switch to the next penguin |
| R | Generate a new map |

## Author

**Josué Quintana Serrano**, Computer Engineering student at Tecnológico de Costa Rica (TEC).

- GitHub: [KingtanaSr](https://github.com/KingtanaSr)
- LinkedIn: [Josué Quintana](https://www.linkedin.com/in/josué-quintana-a9520827b)