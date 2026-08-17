#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T0-4 交互式情绪识别（两级结构）：自己输入一句话，实时分类。
- 复用 emotion_prompt.md 的 System Prompt，走本地 7B 服务（端口 8081）。
- 用法：python emotion_repl.py  然后逐句输入；输入 exit / 退出 结束。
"""
import json
import re
import sys
import urllib.request

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

BASE_URL = "http://127.0.0.1:8081/v1/chat/completions"
PROMPT_FILE = "emotion_prompt.md"

VALID_EMOTIONS = {"开心", "难过", "愤怒", "焦虑", "害怕", "压力", "疲惫", "平静"}
VALID_SECONDARY = {"感激", "期待", "放松", "孤独", "内疚", "烦躁", "无聊"}


def load_system_prompt(path=PROMPT_FILE):
    with open(path, "r", encoding="utf-8") as f:
        text = f.read()
    blocks = text.split("```")
    code_blocks = [blocks[i].strip() for i in range(1, len(blocks), 2)]
    return code_blocks[-1] if code_blocks else ""


def extract_json(text):
    m = re.search(r"\{.*\}", text, re.DOTALL)
    if not m:
        return None
    try:
        return json.loads(m.group(0))
    except json.JSONDecodeError:
        return None


def classify(system: str, user: str):
    payload = {
        "model": "local",
        "messages": [
            {"role": "system", "content": system},
            {"role": "user", "content": user},
        ],
        "temperature": 0.1,
        "top_p": 0.9,
        "repeat_penalty": 1.1,
        "max_tokens": 200,
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
        print("[!] 未能从 emotion_prompt.md 提取 System Prompt")
        return

    print("=" * 60)
    print("交互式情绪识别（两级结构）— 本地 Qwen2.5-7B-Instruct")
    print("输入一句话回车；输入 exit / 退出 结束")
    print("=" * 60)

    while True:
        try:
            user = input("你 > ").strip()
        except (EOFError, KeyboardInterrupt):
            print()
            break
        if not user:
            continue
        if user.lower() in ("exit", "quit", "q", "退出"):
            break

        try:
            raw = classify(system, user)
            obj = extract_json(raw)
            if obj:
                e = obj.get("情绪", "?")
                sec = obj.get("附加情绪", [])
                flag = "" if e in VALID_EMOTIONS else " [⚠非八类主情绪]"
                sec_flag = ""
                if isinstance(sec, list):
                    bad = [s for s in sec if s not in VALID_SECONDARY]
                    if bad:
                        sec_flag = f" [⚠附加含清单外词: {bad}]"
                print(f"猫咪识别 > 主情绪={e}{flag}  强度={obj.get('强度')}  附加情绪={sec}{sec_flag}  置信度={obj.get('置信度')}")
                print(f"          理由: {obj.get('理由', '')}")
            else:
                print(f"猫咪识别 > [解析失败] {raw}")
        except Exception as e:
            print(f"猫咪识别 > [错误] {e}")

    print("已退出。")


if __name__ == "__main__":
    main()
