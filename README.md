# RTS Unity Client — Sub-1 Network Layer

Unity 6 URP client for the deterministic-lockstep RTS server at `E:\code\_Claude\RTS` (Go).
This repo is a **bit-identical C# port** of the server's `internal/sim` + `internal/transport` + `internal/wire` packages, plus a minimal MonoBehaviour scaffold (camera, selection, commands, HUD) that drives them at 20 Hz.

**Branch:** `sub1-network-layer` — 20 commits ahead of `main`, already pushed.
**Plan:** [docs/superpowers/plans/2026-04-11-rts-client-sub1.md](docs/superpowers/plans/2026-04-11-rts-client-sub1.md)

---

## Status

| Stage | What | Who |
|---|---|---|
| Tasks 1–21 | All code files written & committed (Core / Sim / Network / Game / Input / UI) | Done |
| Task 22 | Scene GameObjects wired in Unity Editor | **You**, in Unity |
| Task 23 | Live integration against Go server (60 s, 0 desync) | **You**, after Task 22 |

Everything compilable & testable from a terminal is done. The remaining work is clicking in the Editor, because `.unity` scene files are binary-unfriendly and the plan explicitly keeps scene construction as a manual step.

---

## How to use

### Prerequisites

- **Unity 6000.0.58f2** (Unity 6 LTS). `ProjectSettings/ProjectVersion.txt` pins this.
- **Go server** built from `E:\code\_Claude\RTS` — `go build ./cmd/server` listening on `:9000`.
- Windows 10/11 (other platforms untested; UDP socket & UI Toolkit are portable in principle).

### Open & run

1. Open `E:\code\_Unity\rts-client-unity\` in Unity Hub → the project boots, imports, and compiles.
2. Open [Assets/Scenes/](Assets/Scenes/) (the default scene) — **currently empty**. Follow the Task 22 scene checklist below to populate it.
3. In a terminal: `cd E:\code\_Claude\RTS && go run ./cmd/server`.
4. Press **Play** in the Editor → ConnectPanel appears → defaults (`127.0.0.1:9000`, room `test-room`, player `Player`) work out of the box.
5. Launch a second client (second Editor instance, or `go run ./cmd/fake-client`) to fill the room. Game starts when `CurrentN` players are present.
6. Once playing:
   - **LMB drag** — box-select units.
   - **RMB** — move / **Shift+RMB** — queue waypoint.
   - **S** stop, **H** hold, **A** attack-move (stub), **Esc** cancel.
   - **Ctrl+0..9** assign control group, **0..9** recall, **Tab** cycle selection.
   - **WASD** / edge-scroll / MMB-drag pan, wheel zoom, **F3** toggle HUD.

### Task 22 — scene GameObjects (manual, ~10 min)

From plan §Task 22 Step 2 — build this hierarchy in the default scene:

| GameObject | Components |
|---|---|
| `MainCamera` | Camera, `CameraController` |
| `DirectionalLight` | Light |
| `Map` | `MapView` (a Unity Plane child is auto-created at runtime by MapView.Init) |
| `GameRoot` | `GameManager`, `LockstepRunner`, `UnitViewPool`, `SceneWiring` |
| `SelectionSystem` | `SelectionManager`, `CommandDispatcher` |
| `ConnectUI` | `UIDocument` (sourceAsset = `ConnectPanel.uxml`), `ConnectPanelController` |
| `HUD` | `UIDocument` (sourceAsset = `HUD.uxml`), `HUDController` |

Wire the serialized fields per the plan: `SceneWiring` references `MapView`, `CameraController`, `UnitViewPool`; `ConnectPanelController` & `HUDController` each reference their `UIDocument`; `UnitViewPool.unitViewPrefab` → any default prefab (runtime replaces the visuals). Save the scene.

### Task 23 — integration test

Plan §Task 23. With server + fake-client running, play for 60 s and watch:
- HUD `Hash` column matches the server's per-tick hash (peek server logs).
- `rts_desync_total` on the server stays at 0.
- Units interpolate smoothly; RMB moves land on the clicked point.

---

## How to read (suggested order)

The codebase layers cleanly — read deterministic leaves first, then transport, then MonoBehaviour glue.

```
Core ─► Sim ─► Network ─► Game ─► Input ─► UI
                                 (unity-side)
```

### 1 · Deterministic root — read these first

| File | Why |
|---|---|
| [Assets/Core/](Assets/Core/) `Fixed32.cs` | Q16.16 fixed-point. Everything downstream is built on this. |
| [Assets/Sim/](Assets/Sim/) `Trig.cs` | 1025-entry sinTable loaded from `Resources/GoldenData.json`. Matches Go byte-for-byte. |
| [Assets/Sim/](Assets/Sim/) `SimStep.cs` | The 20 Hz step function — the entire gameplay sim. |
| [Assets/Sim/](Assets/Sim/) `SimHash.cs` | FNV-1a-64 canonical hash. Desync detection depends on this matching Go exactly. |
| [Assets/Sim/](Assets/Sim/) `Snapshot.cs` | Marshal/Unmarshal for reconnect + replay. |

### 2 · Wire format

| File | Role |
|---|---|
| [Assets/Network/](Assets/Network/) `Packet.cs` | 19-byte LE header, selective-ACK bitmask. |
| [Assets/Network/](Assets/Network/) `WireCodec.cs` + `Messages.cs` | Cmd / FrameBundle / HashAck / RTTReport / NPub / Resume / Resync. |
| [Assets/Network/](Assets/Network/) `Conn.cs` | ARQ state machine, Jacobson RTT. |
| [Assets/Network/](Assets/Network/) `RtsClient.cs` | Public SDK facade — `Connect / SendCmd / SendHashAck / Update / Tick`. |

### 3 · MonoBehaviour glue

| File | Role |
|---|---|
| [Assets/Game/](Assets/Game/) `GameManager.cs` | Singleton; state machine; drives `Client.Update()` + `Client.Tick()`; loads sinTable at startup. |
| [Assets/Game/](Assets/Game/) `LockstepRunner.cs` | Tick-driven `SimStep.Step`, emits `OnTickAdvanced`, sends HashAck. |
| [Assets/Game/](Assets/Game/) `UnitViewPool.cs` | Pools `UnitView` GameObjects; called on every tick. |
| [Assets/Game/](Assets/Game/) `VisualInterpolator.cs` | Lerps between prev-tick & next-tick positions each frame for smooth rendering at 60 fps. |
| [Assets/Input/](Assets/Input/) `CameraController.cs` | WASD / edge / MMB-drag / zoom. |
| [Assets/Input/](Assets/Input/) `SelectionManager.cs` | Box-select, double-click, control groups, Tab cycle. |
| [Assets/Input/](Assets/Input/) `CommandDispatcher.cs` | RMB move, Shift+RMB queue, S/H/A/Esc; drains queued waypoints at tick boundaries. |
| [Assets/Input/](Assets/Input/) `SceneWiring.cs` | Subscribes `GameManager.OnGameStarted` → inits MapView / CameraController bounds / UnitViewPool. |
| [Assets/UI/](Assets/UI/) `ConnectPanelController.cs` + `HUD.uxml/uss` + `HUDController.cs` | UI Toolkit panels. |

### 4 · Tests

[Assets/Tests/EditMode/](Assets/Tests/EditMode/) — run from **Window → General → Test Runner → EditMode**. Includes Fixed32 math, sinTable golden verification, SimStep golden trace, WireCodec roundtrip, Snapshot roundtrip.

---

## Architecture

Assembly definitions enforce the layering:

```
RTS.Core        (noEngineReferences)  ← primitives (ByteWriter/Reader)
RTS.Sim         (noEngineReferences)  ← Fixed32, Vec2, Trig, World, SimStep, SimHash, Snapshot
RTS.Network     (noEngineReferences)  ← Packet, Conn, RtsClient
RTS.Game        ← UnityEngine           MonoBehaviours that own Sim+Network state
RTS.Input       ← UnityEngine + RTS.Game  input, camera, scene wiring
RTS.UI          ← UnityEngine + RTS.Game + RTS.Input + RTS.Sim  UI Toolkit controllers
```

The three `noEngineReferences` assemblies are the **deterministic core**. They have zero knowledge of Unity, so the same code could run in a Go fake-client, a command-line replay tool, or a headless server — which is how we preserve bit-identical parity with the Go server.

**Deviation from plan:** [Assets/Input/SceneWiring.cs](Assets/Input/SceneWiring.cs) lives in `RTS.Input` (not `RTS.Game` as the plan specified) because it needs `CameraController` (RTS.Input) + `GameManager` (RTS.Game). Moving it to RTS.Input avoids a Game↔Input circular reference. Runtime behavior is unchanged.

---

## Commit map (`sub1-network-layer`)

| Task | Commit | Summary |
|---|---|---|
| 1 | `898658d` | Unity 6 URP project skeleton |
| 2 | `80de722` | Assembly definitions |
| 3 | `69bc3c8` | Core ByteWriter/ByteReader |
| 4 | `6369f01` | Fixed32 |
| 5 | `97b3de1` | Vec2 |
| 6 | `4aef793` | Golden test data import |
| 7 | `6568118` | Trig + sinTable |
| 8 | `4f7f8b8` | SplitMix64 |
| 9 | `6a69fc1` | World / Unit / Cmd |
| 10 | `32cf637` | SimStep + SimHash |
| 11 | `b09a894` | Snapshot |
| 12 | `8c6abe2` | Packet / RetxQueue / ReorderBuffer |
| 13 | `64be505` | Messages + WireCodec |
| 14 | `09b1a69` | Conn / UdpTransport / RtsClient |
| 15 | `a1c2880` | GameManager + LockstepRunner |
| 16 | `c983f1e` | UnitView / UnitViewPool / MapView / VisualInterpolator |
| 17 | `a04147f` | CameraController |
| 18 | `0e8f2b1` | SelectionManager |
| 19 | `2ac26a5` | CommandDispatcher |
| 20–21 | `d48fc1a` | ConnectPanel + HUD (UI Toolkit) |
| 22 | `bda8a38` | SceneWiring + GoldenData resource |

---

## What's next

1. **You** do Task 22 scene setup (~10 min in Editor).
2. **You** run Task 23 integration test against the Go server.
3. After Sub-1 passes: Sub-2 = gameplay (attack-move, combat resolution, victory conditions). Plan TBD.
