# LD-60

Ludum Dare 60 jam game. Unity 6000.6.0f1, URP.

Started from Record Collector's base with its gameplay stripped: `Game.Core`, `Game.UI`, `Game.Main`, `Game.Dev`,
`Game.Editor`, `Game.Utilities`, `StateMachine`, an empty `Game.Scripts` / `Game.Tests`, scene
`Assets/_Project/Scenes/Game.unity`, definitions in `Definitions/`.
Register game services in `Game.Main/GameAppStates/AppGameState.cs` (`// TODO: register game services here`).

## Clone

```bash
git clone --recurse-submodules https://github.com/mrkvinter/LD-60.git
```

Submodules (private, Kvinter-Games):
- `Kvintessential/` — shared packages (`file:../Kvintessential/Packages/...` in `Packages/manifest.json`).
- `ThirdParty/` — paid/third-party plugins as local packages (Odin, Animancer, Quantum Console, DOTween, ECM2,
  Asset Usage Detector). Uses Git LFS.

## Auto-deploy to itch.io

`.github/workflows/main.yml`: every push to `main` (or a manual run) builds WebGL on the self-hosted macOS runner
with `game-ci/unity-builder` (`UnityBuilderAction.BuildScript.Build`, `Game.Editor/BuildScript.cs`) and pushes
the build with butler to `<ITCH_USER>/<ITCH_GAME_ID>:webgl`. A newer push cancels a running build.

One-time setup:

| What | Where | Value |
|---|---|---|
| `UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_LICENSE` | Secrets | Same as LD-59 (`UNITY_LICENSE` = contents of the `.ulf`) |
| `BUTLER_API_KEY` | Secret | https://itch.io/user/settings/api-keys |
| `SUBMODULES_TOKEN` | Secret | Fine-grained PAT, resource owner Kvinter-Games, repos Kvintessential + ThirdParty, Contents: read |
| `ITCH_USER` | Variable | `mrkvinter` |
| `ITCH_GAME_ID` | Variable | slug of the itch page (`https://mrkvinter.itch.io/<slug>`) |
| Runner | Settings > Actions > Runners | Register the MacBook (`self-hosted`, `macOS`) for this repo |

The itch page must exist (Kind of project: HTML, "This file will be played in the browser" on the `webgl`
upload after the first push). Player settings: WebGL compression Brotli, no decompression fallback (as LD-59).
