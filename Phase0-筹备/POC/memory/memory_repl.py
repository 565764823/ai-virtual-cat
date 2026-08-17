#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T0-5 交互式记忆抽取：粘贴一段「铲屎官 + 奶盖」对话，实时抽取结构化事实。
- 复用 memory_prompt.md 的 System Prompt 与 memory_poc.py 的抽取逻辑，走本地 7B（端口 8081）。
- 用法：python memory_repl.py
      粘贴多行对话 → 空行（或输入 END/结束）提交；输入 exit / 退出 结束程序。
"""
import sys

from memory_poc import CONFIDENCE_GATE, extract_facts, load_system_prompt

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

PROMPT_FILE = "memory_prompt.md"


def read_conversation():
    print("\n粘贴对话（多行），结束后空行回车（或输入 END/结束）提交；exit/退出 退出：")
    lines = []
    while True:
        try:
            line = input()
        except (EOFError, KeyboardInterrupt):
            print()
            return None
        s = line.strip()
        if s == "":
            if lines:
                break
            continue
        if s.lower() in ("exit", "quit", "q", "退出"):
            return None
        if s.lower() in ("end", "结束", "done"):
            break
        lines.append(line)
    return "\n".join(lines)


def main():
    system = load_system_prompt(PROMPT_FILE)
    if not system:
        print(f"[!] 未能从 {PROMPT_FILE} 提取 System Prompt")
        return

    print("=" * 64)
    print("交互式记忆抽取（T0-5）— 本地 Qwen2.5-7B-Instruct")
    print("=" * 64)

    while True:
        conv = read_conversation()
        if conv is None:
            break
        if not conv.strip():
            continue

        print("\n抽取中…（CPU 上 7B 约 5~15 秒）")
        try:
            facts, _raw = extract_facts(system, conv)
        except Exception as e:
            print(f"猫咪记忆 > [错误] {e}")
            continue

        if not facts:
            print("猫咪记忆 > 没有值得长期记忆的稳定事实（未抽取）。")
            continue

        print(f"猫咪记忆 > 抽出 {len(facts)} 条：")
        for i, f in enumerate(facts, 1):
            flag = ""
            if f["置信度"] < CONFIDENCE_GATE:
                flag = " [⚠低置信→待确认]"
            if f["归属"] not in ("用户", "猫"):
                flag += " [⚠归属异常]"
            print(f"  [{i}] 归属={f['归属']}  置信度={f['置信度']:.2f}{flag}")
            print(f"       主体: {f['主体']} | 关系: {f['关系']} | 客体: {f['客体']}")
            print(f"       事实: {f['事实']}")

    print("已退出。")


if __name__ == "__main__":
    main()
