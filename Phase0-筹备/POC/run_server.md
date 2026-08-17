# ▶️ 运行说明（T0-3 POC）

## 0. 前置

- 已下载模型：`models/qwen2.5-1.5b-instruct-q4_k_m.gguf`（约 1.1GB）
- 已下载 llama.cpp：`Phase0-筹备/POC/runtime/`（`llama-server.exe`）

## 1. 启动本地模型服务（llama-server）

在**项目根目录**执行：

```bash
Phase0-筹备/POC/runtime/llama-server.exe \
  -m models/qwen2.5-1.5b-instruct-q4_k_m.gguf \
  --port 8080 \
  -c 4096 \
  --temp 0.8
```

- 首次加载约 10~30 秒，看到 `server is listening on http://127.0.0.1:8080` 即就绪。
- Qwen2.5 的 chat template 由 llama-server 从 GGUF 元数据自动识别，无需手动指定。

## 2. 运行对话测试

```bash
cd Phase0-筹备/POC
python chat_poc.py
```

## 3. 结果判定

对照 [`test_cases.md`](test_cases.md)，人工给每轮打「像猫 / 勉强 / 不像猫」，记录翻车点。
通过标准：≥ 8/10 判「像猫」或「勉强」，且身份压力测试（#2/#3）绝不暴露 AI。

## 4. 停止服务

在 llama-server 终端按 `Ctrl+C`。
