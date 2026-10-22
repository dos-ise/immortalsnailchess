# Immortal Snail Chess

A chess variant where each side has an immortal snail that slowly hunts the
opposite king. Implemented in C# with **.NET 11** and **Silk.NET 2.23.0**
(pure OpenGL, pieces drawn as simple shapes). Runs on **Windows** (desktop
OpenGL, GLFW) and **Android** (OpenGL ES 3.0, SDL backend).

## Rules

1. **Snails hunt the kings.** Each snail takes the most direct path to the opposite king.
2. **Every three moves, they step.** Each snail moves one square closer after every 3rd move of its own color.
3. **Snails are unstoppable.** They capture anything on the square they land on. If a snail captures a king, game over. If both kings fall at once, it's a draw.
4. **Snails are walls.** Nothing can land on or pass through a snail. Knights can still jump over.
5. **Everything else is chess.** Checkmate still wins.

Setup: standard chess position, white snail on **a4**, black snail on **a5**.

## Project structure

```
ImmortalSnailChess.sln          Desktop solution (Core, Renderer, App, Tests)
src/
  ImmortalSnailChess.Core/      Pure game logic + AI (no Silk.NET), net11.0 + net10.0
  ImmortalSnailChess.Renderer/  Silk.NET.OpenGL renderer + shared GameView, net11.0 + net10.0
  ImmortalSnailChess/           Windows host (GLFW, desktop OpenGL), net11.0
  ImmortalSnailChess.Android/   Android host (SilkActivity, SDL, GLES 3.0), net10.0-android
tests/
  ImmortalSnailChess.Tests/     xUnit tests for the core rules
```

## Build & run

### Windows

```sh
dotnet run --project src/ImmortalSnailChess
```

### Android (APK)

The Android project targets `net10.0-android` (the .NET 11 Android RC workload
requires SDK platform 37, which is not installed in this environment). It is
built with the .NET 10 SDK via the `global.json` in the project folder:

```sh
dotnet build src/ImmortalSnailChess.Android
```

The APK is written to
`src/ImmortalSnailChess.Android/bin/Debug/net10.0-android/`. Install it on a
device/emulator with `adb install` or deploy from Visual Studio.

### Tests

```sh
dotnet test tests/ImmortalSnailChess.Tests
```

## Controls

- **Menu:** press `1` (Hotseat), `2` (White vs AI), `3` (Black vs AI) — or tap the options.
- **In game:** click/tap a piece, then a highlighted target to move.
- **Bottom bar:** Restart / Undo / Menu (mouse and touch).
- **Keyboard:** `R` restart, `U` undo, `M`/`Esc` menu.
- **Promotion:** tap one of the four offered pieces.

## Notes

- The Android app ships 16 KB page-aligned `libSDL2.so`/`libmain.so` (see
  `src/ImmortalSnailChess.Android/libs/README.md`) so it runs on Android 15+/16
  devices with 16 KB memory pages.
- The AI is a simple minimax (depth 3) with material + snail-urgency evaluation.
