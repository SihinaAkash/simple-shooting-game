# SimpleShooter — 3D Android Shooter in C# (MonoGame)

A first-person 3D shooting game for Android, written in **pure C# with MonoGame**.
No Unity, no Godot — just raw MonoGame and procedural geometry.

---

## 🎮 Gameplay

| | |
|---|---|
| **Goal**     | Survive as long as possible — kill red cube enemies before they reach you |
| **Controls** | **Left side** of screen → Tap to shoot · **Right side** → Drag to look around |
| **Scoring**  | +10 points per enemy killed |
| **Damage**   | Each enemy takes **3 hits**; touching you drains your HP |
| **Difficulty** | Spawn rate increases over time; enemies appear in groups as your score grows |

---

## 🗂️ Project Structure

```
SimpleShooter/
├── SimpleShooter.csproj      – Android project (targets net8.0-android)
├── Activity1.cs              – Android Activity entry point
├── Game1.cs                  – Main game loop, camera, input, rendering, HUD
├── Enemy.cs                  – Enemy data struct
├── Bullet.cs                 – Bullet data struct
├── CubeMesh.cs               – Procedural unit-cube geometry (no model files needed)
└── Content/
    ├── Content.mgcb          – MonoGame content pipeline config
    └── Font.spritefont       – HUD font definition (baked at build time)
```

---

## 🛠️ Prerequisites

| Tool | Version | Notes |
|---|---|---|
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.x | Required for `net8.0-android` |
| [MonoGame 3.8.1](https://www.monogame.net/downloads/) | 3.8.1 | Installs project templates & MGCB editor |
| Android SDK & NDK | API 21+ | Install via Android Studio or Visual Studio |
| Visual Studio 2022 or Rider | Latest | Either works; VS has the best Android tooling |

---

## 🚀 Build & Deploy

### Option A — Visual Studio 2022
1. Open `SimpleShooter.csproj`
2. Set the **target** to your connected Android device or emulator
3. Press **F5** to build, deploy, and run

### Option B — Command Line
```bash
# Restore packages
dotnet restore

# Build & deploy to a connected device
dotnet build -c Release -f net8.0-android
dotnet publish -c Release -f net8.0-android
# Then adb install the generated .apk from bin/Release/net8.0-android/
```

### Option C — MonoGame Content Pipeline (font only)
If you want to rebuild the font atlas manually:
```bash
dotnet tool install -g dotnet-mgcb
mgcb Content/Content.mgcb
```
The build task in the .csproj runs this automatically via `MonoGame.Content.Builder.Task`.

---

## 🏗️ How the 3D Rendering Works

Everything is drawn with **`BasicEffect`** and **`DrawUserPrimitives`** — no shaders, no model files.

| Feature | Technique |
|---|---|
| Cubes | 36 `VertexPositionColor` vertices per cube (12 triangles), built in `CubeMesh.cs` |
| Floor | 40×40 checkered tile grid, pre-built once in `BuildFloor()` |
| Camera | `Matrix.CreateLookAt` from player position + look direction |
| Look direction | Yaw & pitch angles → `Matrix.CreateRotationX * CreateRotationY * Vector3.Forward` |
| HUD | 2-D `SpriteBatch` drawn after the 3-D pass |

---

## 🔧 Extending the Game

| Idea | Where to start |
|---|---|
| Player movement (joystick) | Add left-stick logic in `HandleTouch()` in `Game1.cs` |
| Multiple enemy types | Expand `Enemy.cs`; add speed/health tiers in `SpawnEnemy()` |
| Skybox | Add 5 large quad faces in `BuildSky()` and draw before the floor |
| Sound effects | Add `SoundEffect` content; call `.Play()` on shoot/hit |
| High score persistence | Use `Android.App.Application.Context.GetSharedPreferences()` |
| Proper 3D models | Replace `CubeMesh.Create()` calls with `Content.Load<Model>()` |

---

## 📄 License
MIT — free to use, modify, and distribute.
