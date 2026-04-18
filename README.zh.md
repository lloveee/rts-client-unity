# RTS Unity 客户端 — Sub-1 网络层

为确定性 lockstep RTS 服务器（Go 实现，位于 `E:\code\_Claude\RTS`）打造的 Unity 6 URP 客户端。
本仓库是服务端 `internal/sim` + `internal/transport` + `internal/wire` 三个包的 **逐字节等价 C# 移植**，外加一套最小化的 MonoBehaviour 脚手架（相机、选择、命令、HUD），以 20 Hz 驱动核心逻辑。

**分支：** `sub1-network-layer` — 领先 `main` 20 个 commit，已推送 origin。
**设计文档：** [docs/superpowers/plans/2026-04-11-rts-client-sub1.md](docs/superpowers/plans/2026-04-11-rts-client-sub1.md)

> English version: [README.md](README.md)

---

## 当前进度

| 阶段 | 内容 | 由谁完成 |
|---|---|---|
| Task 1–21 | 所有代码文件已写完并 commit（Core / Sim / Network / Game / Input / UI） | 已完成 |
| Task 22 | 在 Unity 编辑器里挂 GameObject 和组件 | **你**，在 Unity 里 |
| Task 23 | 对接 Go 服务器做 60 秒 0-desync 集成测试 | **你**，Task 22 完成后 |

命令行能编译/能测试的部分全部做完了。剩下的是**在编辑器里点鼠标**——因为 `.unity` 场景文件不适合程序自动生成，计划本身也明确把场景搭建留作手动步骤。

---

## 怎么使用

### 前置条件

- **Unity 6000.0.58f2**（Unity 6 LTS）。`ProjectSettings/ProjectVersion.txt` 已锁定版本。
- **Go 服务器**：在 `E:\code\_Claude\RTS` 下 `go build ./cmd/server`，监听 `:9000`。
- Windows 10/11（其他平台未测试；UDP Socket 与 UI Toolkit 原则上跨平台）。

### 打开并运行

1. 用 Unity Hub 打开 `E:\code\_Unity\rts-client-unity\` — 项目会自动导入、编译。
2. 打开 [Assets/Scenes/](Assets/Scenes/) 里的默认场景 — **目前是空的**。按下面 Task 22 的清单把 GameObject 搭起来。
3. 开一个终端：`cd E:\code\_Claude\RTS && go run ./cmd/server`。
4. 按 Unity 编辑器的 **Play** 键 → 弹出 ConnectPanel → 默认值（`127.0.0.1:9000`、房间 `test-room`、玩家 `Player`）开箱即用。
5. 再开第二个客户端（第二个 Unity 编辑器实例，或 `go run ./cmd/fake-client`）凑齐玩家。达到 `CurrentN` 人后自动开战。
6. 进入游戏后：
   - **鼠标左键拖拽** — 框选单位
   - **右键** 移动 / **Shift+右键** 追加航点
   - **S** 停止 · **H** 原地守 · **A** 攻击移动（占位） · **Esc** 取消
   - **Ctrl+0~9** 编队 · **0~9** 选中编队 · **Tab** 循环选中
   - **WASD** / 边缘滚屏 / **中键拖拽** 平移相机，滚轮缩放，**F3** 开关 HUD

### Task 22 — 场景 GameObject（手动，约 10 分钟）

对照计划文档 §Task 22 Step 2，在默认场景里搭出下面的层级：

| GameObject | 挂的组件 |
|---|---|
| `MainCamera` | Camera、`CameraController` |
| `DirectionalLight` | Light |
| `Map` | `MapView`（运行时 `MapView.Init` 会自动生成一个 Plane 子物体） |
| `GameRoot` | `GameManager`、`LockstepRunner`、`UnitViewPool`、`SceneWiring` |
| `SelectionSystem` | `SelectionManager`、`CommandDispatcher` |
| `ConnectUI` | `UIDocument`（sourceAsset = `ConnectPanel.uxml`）、`ConnectPanelController` |
| `HUD` | `UIDocument`（sourceAsset = `HUD.uxml`）、`HUDController` |

按计划把 `[SerializeField]` 字段串起来：`SceneWiring` 引用 `MapView`、`CameraController`、`UnitViewPool`；`ConnectPanelController` 和 `HUDController` 各自引用自己的 `UIDocument`；`UnitViewPool.unitViewPrefab` 指向任意默认 prefab（运行时会替换视觉）。保存场景。

### Task 23 — 集成测试

参见计划文档 §Task 23。启动服务器 + fake-client，Play 模式跑 60 秒，观察：
- HUD 里的 `Hash` 字段和服务器的每 tick hash 一致（服务器日志里能看到）
- 服务器 `rts_desync_total` 始终为 0
- 单位移动平滑；右键点地的目标点就是单位要走到的位置

---

## 怎么阅读（推荐顺序）

代码分层非常干净 — 先读最底层的**确定性核心**，再读传输，最后看 MonoBehaviour 黏合层。

```
Core ─► Sim ─► Network ─► Game ─► Input ─► UI
                                (Unity 这一侧)
```

### 1 · 确定性根基 — 先读这些

| 文件 | 为什么重要 |
|---|---|
| [Assets/Core/](Assets/Core/) `Fixed32.cs` | Q16.16 定点数。下游所有东西都基于它。 |
| [Assets/Sim/](Assets/Sim/) `Trig.cs` | 1025 项的 sinTable，从 `Resources/GoldenData.json` 加载。与 Go 逐字节一致。 |
| [Assets/Sim/](Assets/Sim/) `SimStep.cs` | 20 Hz 的 tick 步进函数 — 所有玩法逻辑的核心。 |
| [Assets/Sim/](Assets/Sim/) `SimHash.cs` | FNV-1a-64 canonical hash。Desync 检测完全靠它和 Go 的哈希完全一致。 |
| [Assets/Sim/](Assets/Sim/) `Snapshot.cs` | 重连/回放用的 Marshal/Unmarshal。 |

### 2 · 线上协议

| 文件 | 作用 |
|---|---|
| [Assets/Network/](Assets/Network/) `Packet.cs` | 19 字节 LE 包头，selective-ACK 位图。 |
| [Assets/Network/](Assets/Network/) `WireCodec.cs` + `Messages.cs` | Cmd / FrameBundle / HashAck / RTTReport / NPub / Resume / Resync。 |
| [Assets/Network/](Assets/Network/) `Conn.cs` | ARQ 状态机、Jacobson RTT 估计。 |
| [Assets/Network/](Assets/Network/) `RtsClient.cs` | 对外 SDK 门面 — `Connect / SendCmd / SendHashAck / Update / Tick`。 |

### 3 · MonoBehaviour 黏合层

| 文件 | 作用 |
|---|---|
| [Assets/Game/](Assets/Game/) `GameManager.cs` | 单例；状态机；每帧驱动 `Client.Update()` + `Client.Tick()`；启动时加载 sinTable。 |
| [Assets/Game/](Assets/Game/) `LockstepRunner.cs` | tick 驱动 `SimStep.Step`、发 `OnTickAdvanced` 事件、回送 HashAck。 |
| [Assets/Game/](Assets/Game/) `UnitViewPool.cs` | 池化 `UnitView` GameObject；每 tick 同步一次。 |
| [Assets/Game/](Assets/Game/) `VisualInterpolator.cs` | 在上一 tick 与下一 tick 的位置之间线性插值，60 fps 渲染仍然丝滑。 |
| [Assets/Input/](Assets/Input/) `CameraController.cs` | WASD / 边缘 / 中键拖拽 / 缩放。 |
| [Assets/Input/](Assets/Input/) `SelectionManager.cs` | 框选、双击、编队、Tab 循环。 |
| [Assets/Input/](Assets/Input/) `CommandDispatcher.cs` | 右键移动、Shift+右键队列、S/H/A/Esc；在 tick 边界上把队列里的航点发出去。 |
| [Assets/Input/](Assets/Input/) `SceneWiring.cs` | 订阅 `GameManager.OnGameStarted` → 初始化 MapView / CameraController 边界 / UnitViewPool。 |
| [Assets/UI/](Assets/UI/) `ConnectPanelController.cs` + `HUD.uxml/uss` + `HUDController.cs` | UI Toolkit 面板。 |

### 4 · 测试

[Assets/Tests/EditMode/](Assets/Tests/EditMode/) — 从 **Window → General → Test Runner → EditMode** 跑。覆盖：Fixed32 数学、sinTable 黄金数据校验、SimStep 黄金轨迹、WireCodec 往返、Snapshot 往返。

---

## 架构

asmdef 强制分层：

```
RTS.Core        (noEngineReferences)  ← 原语（ByteWriter/Reader）
RTS.Sim         (noEngineReferences)  ← Fixed32、Vec2、Trig、World、SimStep、SimHash、Snapshot
RTS.Network     (noEngineReferences)  ← Packet、Conn、RtsClient
RTS.Game        ← UnityEngine           拥有 Sim+Network 状态的 MonoBehaviour
RTS.Input       ← UnityEngine + RTS.Game  输入、相机、场景串接
RTS.UI          ← UnityEngine + RTS.Game + RTS.Input + RTS.Sim  UI Toolkit 控制器
```

那三个 `noEngineReferences` 程序集就是**确定性核心**，它们对 Unity 一无所知 — 同一套代码可以运行在 Go fake-client、命令行回放工具，或者无头服务器里 — 这就是为什么我们能和 Go 服务端保持逐字节一致。

**与计划的偏差：** [Assets/Input/SceneWiring.cs](Assets/Input/SceneWiring.cs) 放在 `RTS.Input`（不是计划里说的 `RTS.Game`）里，因为它同时需要 `CameraController`（RTS.Input）和 `GameManager`（RTS.Game）。放进 RTS.Input 可以避免 Game↔Input 的循环引用。运行时行为完全一致。

---

## Commit 对照表（`sub1-network-layer`）

| Task | Commit | 内容 |
|---|---|---|
| 1 | `898658d` | Unity 6 URP 项目骨架 |
| 2 | `80de722` | asmdef 分层 |
| 3 | `69bc3c8` | Core ByteWriter/ByteReader |
| 4 | `6369f01` | Fixed32 |
| 5 | `97b3de1` | Vec2 |
| 6 | `4aef793` | 导入黄金测试数据 |
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
| 20–21 | `d48fc1a` | ConnectPanel + HUD（UI Toolkit） |
| 22 | `bda8a38` | SceneWiring + GoldenData 资源 |

---

## 下一步

1. **你** 在编辑器里做 Task 22 的场景搭建（约 10 分钟）。
2. **你** 跑 Task 23 的服务器联调测试。
3. Sub-1 通过后：Sub-2 = 真正的玩法（攻击移动、战斗结算、胜负条件）。计划待定。
