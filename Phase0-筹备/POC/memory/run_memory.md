# ▶️ 运行说明（T0-5 记忆抽取与注入 POC）

## 0. 前置

- 模型：`models/qwen2.5-7b-instruct-q4_k_m-00001-of-00002.gguf` + `…-00002-of-00002.gguf`（与 T0-4 情绪识别同一份 7B）
- 运行时：复用 `Phase0-筹备/POC/runtime/llama-server.exe`

## 1. 启动 7B 模型服务（llama-server，端口 8081）

在**项目根目录**执行（传**第一个分片**，llama.cpp 会自动加载同目录第二个分片）：

```bash
Phase0-筹备/POC/runtime/llama-server.exe \
  -m models/qwen2.5-7b-instruct-q4_k_m-00001-of-00002.gguf \
  --port 8081 \
  -c 4096
```

- 看到 `server is listening on http://127.0.0.1:8081` 即就绪（首次加载 30~60 秒）。
- 若 8081 被占用，改端口并在 `memory_poc.py` 同步 `BASE_URL`。

> ⚠️ 本 POC 的**抽取**与**注入后回复**都走 7B 的 `chat/completions`，无需 `--embeddings`。
> 向量化默认用**字符 n-gram 哈希基线**（零依赖、离线）。若想让检索用语义向量，启动时追加 `--embeddings`，
> 脚本会自动改走 `/v1/embeddings`（正式落地将替换为 bge-small-zh 专用 Embedding 模型）。

## 2. 运行

```bash
cd Phase0-筹备/POC/memory
python memory_poc.py           # 演示完整闭环（抽取→向量化→检索→注入）
python memory_poc.py --eval    # 运行评测集，输出抽取召回 + 检索命中
```

- 抽取 + 注入各一次 LLM 调用，CPU 上 7B 每条约 5~15 秒，全程约 1~2 分钟，属正常。

## 3. 结果判定

对照 [`memory_test_cases.md`](memory_test_cases.md)：

- 抽取召回 ≥ 80%
- 检索命中（top-3）≥ 60%
- 阶段 3 注入后，猫咪能「记得」并自然提及（如被问「你是什么颜色的猫」能答出橘猫）

## 4. 停止服务

在 llama-server 终端按 `Ctrl+C`。
