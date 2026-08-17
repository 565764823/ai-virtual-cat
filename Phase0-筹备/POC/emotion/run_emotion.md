# ▶️ 运行说明（T0-4 情绪识别 POC）

## 0. 前置

- 模型：`models/qwen2.5-7b-instruct-q4_k_m-00001-of-00002.gguf` + `…-00002-of-00002.gguf`（共约 4.46GB，分片，需先下载）
- 运行时：复用 `Phase0-筹备/POC/runtime/llama-server.exe`

## 1. 启动 7B 模型服务（llama-server，端口 8081）

在**项目根目录**执行（传**第一个分片**，llama.cpp 会自动加载同目录的第二个分片）：

```bash
Phase0-筹备/POC/runtime/llama-server.exe \
  -m models/qwen2.5-7b-instruct-q4_k_m-00001-of-00002.gguf \
  --port 8081 \
  -c 4096
```

- 首次加载约 30~60 秒，看到 `server is listening on http://127.0.0.1:8081` 即就绪。
- 7B 在 CPU 上推理，每条分类约 5~15 秒，30 条评测集约 3~6 分钟，属正常。
- 若 8081 被占用，改端口并在 `emotion_poc.py` 同步 `BASE_URL`。

## 2. 运行情绪识别测试

```bash
cd Phase0-筹备/POC/emotion
python emotion_poc.py
```

## 3. 结果判定

对照 [`emotion_test_cases.md`](emotion_test_cases.md)：
- 类别准确率 ≥ 80%（16/20）
- 强度（±1 容差）≥ 80%
- 易混词区分（焦虑/压力/疲惫）≥ 75%

## 4. 停止服务

在 llama-server 终端按 `Ctrl+C`。
