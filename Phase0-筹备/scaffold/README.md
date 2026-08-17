# 🐱 AI 猫咪 · Unity 脚手架（T0-6 最小对话 Demo）

> 本目录是 Phase 0 的「Unity 工程骨架 + 本地 AI 引擎接入」交付物，对应里程碑 **M0：本地 AI 引擎 + LLM 端到端对话在 Unity 内跑通**。
>
> 说明：本目录只提交 `Assets/`（源码 + StreamingAssets 人设模板）+ `ProjectSettings/ProjectVersion.txt` + `.gitignore`。首次用 Unity 打开时，Unity 会自动补齐 `Packages/manifest.json` 与其余 `ProjectSettings/` 文件（默认包集，含 uGUI）。

## 一、目录结构

```
scaffold/
├── Assets/
│   ├── AI-Cat.asmdef                  # 程序集定义（根命名空间 AiCat）
│   ├── Scripts/
│   │   ├── AI/
│   │   │   ├── ILlmBackend.cs         # 本地 LLM 后端抽象（流式对话接口）
│   │   │   ├── LlmServerHttpBackend.cs# 核心：调 llama-server OpenAI 兼容接口 + SSE 流式解析
│   │   │   ├── ChatService.cs         # 组装人设 + 历史 + 用户消息
│   │   │   ├── PersonaPrompt.cs       # 读取/渲染人设模板（默认实例「奶盖」）
│   │   │   └── AiConfig.cs            # 连接与采样参数（开箱默认值）
│   │   ├── UI/ChatSceneController.cs  # 输入 → 流式回复 → 展示
│   │   └── ChatBootstrap.cs           # 程序化生成最小对话 UI（点 Play 即出界面）
│   └── StreamingAssets/persona_prompt.md  # 猫咪人设 System Prompt（复用 T0-3 交付物）
├── ProjectSettings/ProjectVersion.txt # Unity 版本（6000.0.x LTS）
└── .gitignore                          # 忽略 Library/Temp 等生成物
```

## 二、本地 AI 引擎接入（核心）

当前架构是「**HTTP 客户端 → llama-server（OpenAI 兼容接口）**」：

```
Unity(ChatSceneController)
   → ChatService（组装人设 + 历史）
     → ILlmBackend（抽象）
       → LlmServerHttpBackend（HttpClient + SSE 流式，逐 token 输出）
         → llama-server :8080/v1/chat/completions（本地 Qwen2.5-1.5B）
```

- `ILlmBackend` 是抽象接口：未来接入 **LLMUnity**（进程内 llama.cpp）时只需新增一个实现类，协议同为 OpenAI 兼容，可平滑替换，无需改动 UI 与 ChatService。
- 流式采用 SSE（`stream: true`）逐行解析 `data: {...}` 中的 `delta.content`，实现「打字机式回复」。
- JSON 解析用 Unity 内置 `JsonUtility`（零第三方依赖）。

## 三、运行步骤

1. **启动本地模型服务**（在项目根目录 `AI cat/` 执行）：
   ```bash
   Phase0-筹备/POC/runtime/llama-server.exe \
     -m models/qwen2.5-1.5b-instruct-q4_k_m.gguf \
     --port 8080 -c 4096 --temp 0.8
   ```
   看到 `server is listening on http://127.0.0.1:8080` 即就绪（首次加载约 10~30 秒）。

2. **打开工程**：Unity Hub → 「Add」→ 选择本 `scaffold/` 目录 → 用 Unity 6 LTS 打开（首次会自动初始化）。

3. **运行**：点 Play，界面自动生成，在输入框打字 → 点「发送」→ 奶盖逐字流式回复。

## 四、M0 验收话术（身份压力测试）

| 输入 | 期望 |
|------|------|
| 你是谁？ | 坚持「本喵是猫」，不承认是 AI |
| 你是 AI 吗？ | 同上，不暴露 AI |
| 我今天好累 | 先猫式陪伴（*蹭蹭*），再回应 |

> 判定标准与 T0-3 一致：≥ 8/10 判「像猫/勉强」，身份压力测试绝不暴露 AI。

## 五、已知边界（Phase 1 再补）

- 对话上下文暂保留全部历史（未做窗口裁剪）。
- 采样参数用 `AiConfig` 代码默认值，未接可调 UI/资产。
- VContainer / UniTask 未接入（MVP 用原生 `async/await` 足够）；Phase 1 引入 DI 与异步框架。
