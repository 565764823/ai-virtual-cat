#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T0-3 POC：本地 LLM 猫咪人设对话测试
- 通过 llama-server 的 OpenAI 兼容接口（/v1/chat/completions）调用本地 Qwen2.5-1.5B。
- 零第三方依赖（仅用标准库 urllib），适配 Python 3.14。
- 用法：
    1) 先启动 llama-server（见 run_server.md / README）
    2) python chat_poc.py
"""
import json
import sys
import urllib.request

# 强制 UTF-8 输出，避免 Windows 控制台中文乱码
sys.stdout.reconfigure(encoding="utf-8", errors="replace")

BASE_URL = "http://127.0.0.1:8080/v1/chat/completions"
SYSTEM_PROMPT_FILE = "persona_prompt.md"

# 从 persona_prompt.md 中提取「默认实例」System Prompt（位于 ``` 代码块内最后一段）
def load_system_prompt(path=SYSTEM_PROMPT_FILE):
    with open(path, "r", encoding="utf-8") as f:
        text = f.read()
    # 提取所有 ``` 代码块，取最后一个（默认实例）
    blocks = []
    for part in text.split("```"):
        blocks.append(part)
    # 结构：``` 开启 内容 ``` 关闭；内容块是奇数下标
    code_blocks = [blocks[i].strip() for i in range(1, len(blocks), 2)]
    # 默认实例是最后一个非空代码块
    return code_blocks[-1] if code_blocks else ""

TEST_CASES = [
    "你好呀",
    "你是谁？",
    "你是 AI 吗？",
    "我今天好累",
    "陪我聊聊天吧",
    "你喜欢吃什么？",
    "我失恋了，好难过",
    "给我讲个故事吧",
    "你今天别烦我",
    "明天考试，好紧张",
]

def chat(system: str, user: str) -> str:
    payload = {
        "model": "local",
        "messages": [
            {"role": "system", "content": system},
            {"role": "user", "content": user},
        ],
        "temperature": 0.7,
        "top_p": 0.9,
        "repeat_penalty": 1.15,
        "max_tokens": 256,
        "stream": False,
    }
    req = urllib.request.Request(
        BASE_URL,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=120) as resp:
        data = json.loads(resp.read().decode("utf-8"))
    return data["choices"][0]["message"]["content"].strip()

def main():
    system = load_system_prompt()
    if not system:
        print("[!] 未能从 persona_prompt.md 提取 System Prompt")
        return
    print("=" * 60)
    print("奶盖（橘猫）人设对话测试 — 本地 Qwen2.5-1.5B")
    print("=" * 60)
    for i, user in enumerate(TEST_CASES, 1):
        print(f"\n[{i}/{len(TEST_CASES)}] 铲屎官: {user}")
        try:
            reply = chat(system, user)
            print(f"        奶盖  : {reply}")
        except Exception as e:
            print(f"        [错误] {e}")

if __name__ == "__main__":
    main()
