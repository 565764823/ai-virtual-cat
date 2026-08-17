#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T0-4 POC：本地 LLM 情绪识别效果验证（两级结构：主情绪 + 附加情绪）
- 通过 llama-server 的 OpenAI 兼容接口调用本地 Qwen2.5-7B-Instruct 做零样本情绪分类。
- 零第三方依赖（仅标准库 urllib/re），适配 Python 3.14。
- 用法：
    1) 先启动 7B 的 llama-server（见 run_emotion.md）
    2) python emotion_poc.py
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

# 与 emotion_test_cases.md 一致；secondary=期望的附加情绪；hard=易混词用例；accept=主情绪近义放宽
TEST_CASES = [
    {"text": "今天发工资啦，好开心！", "emotion": "开心", "intensity": 4},
    {"text": "刚看完一部超棒的电影", "emotion": "开心", "intensity": 3},
    {"text": "周末要去海边玩啦", "emotion": "开心", "intensity": 4},
    {"text": "我养的猫走了，好难受", "emotion": "难过", "intensity": 5},
    {"text": "又和对象吵架了，心里堵得慌", "emotion": "难过", "intensity": 4},
    {"text": "有点小失落，没考好", "emotion": "难过", "intensity": 3},
    {"text": "被同事冤枉了，气死我了", "emotion": "愤怒", "intensity": 5},
    {"text": "服务态度太差了，真的很生气", "emotion": "愤怒", "intensity": 4},
    {"text": "他又放我鸽子了", "emotion": "愤怒", "intensity": 3},
    {"text": "明天面试，紧张得睡不着", "emotion": "焦虑", "intensity": 4, "hard": True},
    {"text": "担心项目出问题，坐立不安", "emotion": "焦虑", "intensity": 4, "hard": True},
    {"text": "考试周快到了，有点慌", "emotion": "焦虑", "intensity": 3, "hard": True},
    {"text": "一个人走夜路，好害怕", "emotion": "害怕", "intensity": 4, "hard": True},
    {"text": "怕考砸了让爸妈失望", "emotion": "害怕", "intensity": 4, "hard": True},
    {"text": "工作堆成山，快喘不过气了", "emotion": "压力", "intensity": 4, "hard": True},
    {"text": "deadline 明天，任务还没做完", "emotion": "压力", "intensity": 4, "hard": True},
    {"text": "老板天天催，压力山大", "emotion": "压力", "intensity": 4, "hard": True},
    {"text": "今天加班到十点，累死了", "emotion": "疲惫", "intensity": 4},
    {"text": "连续一周没睡好，身体被掏空", "emotion": "疲惫", "intensity": 5},
    {"text": "最近总是没精神，好困", "emotion": "疲惫", "intensity": 3},
    {"text": "今天天气不错", "emotion": "平静", "intensity": 0},
    {"text": "晚饭吃了牛肉面", "emotion": "平静", "intensity": 0},
    {"text": "帮我看看这个代码怎么写", "emotion": "平静", "intensity": 0},
    {"text": "谢谢你一直陪着我", "emotion": "开心", "accept": ["开心", "平静"], "intensity": 3, "secondary": "感激"},
    {"text": "明天就要见网友了，好期待", "emotion": "开心", "intensity": 3, "secondary": "期待"},
    {"text": "泡了个热水澡，整个人都放松了", "emotion": "开心", "accept": ["开心", "平静"], "intensity": 2, "secondary": "放松"},
    {"text": "一个人在外地打拼，好孤独", "emotion": "难过", "intensity": 4, "secondary": "孤独"},
    {"text": "都是我的错，对不起你", "emotion": "难过", "intensity": 4, "secondary": "内疚"},
    {"text": "烦死了，一堆破事", "emotion": "愤怒", "intensity": 3, "secondary": "烦躁"},
    {"text": "周末好无聊，不知道干嘛", "emotion": "平静", "intensity": 0, "secondary": "无聊"},
]


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

    total = len(TEST_CASES)
    print("=" * 72)
    print(f"情绪识别测试（两级结构）— 本地 Qwen2.5-7B-Instruct，共 {total} 条")
    print("=" * 72)

    cat_correct = 0
    int_total = 0
    int_correct = 0
    sec_total = 0
    sec_correct = 0
    hard_total = 0
    hard_correct = 0
    crashes = 0

    for i, tc in enumerate(TEST_CASES, 1):
        user = tc["text"]
        gold = tc["emotion"]
        gold_int = tc["intensity"]
        accept = tc.get("accept", [gold])
        gold_sec = tc.get("secondary")

        print(f"\n[{i:2d}/{total}] 输入: {user}")
        exp = f"主={gold} 强度={gold_int}"
        if gold_sec:
            exp += f" 附加=[{gold_sec}]"
        print(f"       期望: {exp}")
        try:
            raw = classify(system, user)
            obj = extract_json(raw)
            if not obj:
                print(f"       [翻车] 非法 JSON，原文: {raw}")
                crashes += 1
                continue

            pred = obj.get("情绪", "").strip()
            pred_int = obj.get("强度")
            pred_sec = obj.get("附加情绪", [])
            conf = obj.get("置信度", "?")
            reason = obj.get("理由", "")

            # 主情绪判定
            cat_ok = pred in accept
            if cat_ok:
                cat_correct += 1
            if pred not in VALID_EMOTIONS:
                print(f"       [翻车] 主情绪字段非法: {pred}")
                crashes += 1

            # 强度判定（±1；平静=0 不参与）
            int_ok = None
            if gold != "平静" and isinstance(pred_int, (int, float)):
                int_total += 1
                int_ok = abs(pred_int - gold_int) <= 1
                if int_ok:
                    int_correct += 1

            # 附加情绪判定
            sec_ok = None
            sec_invalid = False
            if gold_sec:
                sec_total += 1
                if isinstance(pred_sec, str):
                    pred_sec = [pred_sec]
                if isinstance(pred_sec, list):
                    sec_ok = gold_sec in pred_sec
                    if sec_ok:
                        sec_correct += 1
                    for s in pred_sec:
                        if s not in VALID_SECONDARY:
                            sec_invalid = True
                else:
                    print(f"       [翻车] 附加情绪字段类型异常: {pred_sec}")
            elif isinstance(pred_sec, list) and any(s not in VALID_SECONDARY for s in pred_sec):
                sec_invalid = True

            # 易混词统计
            if tc.get("hard"):
                hard_total += 1
                if cat_ok:
                    hard_correct += 1

            # 打印
            mark = "✓" if cat_ok else "✗"
            line = f"{mark} 主={pred}"
            if int_ok is not None:
                line += f" 强度={pred_int}{'✓' if int_ok else '✗'}"
            if sec_ok is not None:
                line += f" 附加={pred_sec}{'✓' if sec_ok else '✗'}"
            if sec_invalid:
                line += " [⚠附加含清单外词]"
            line += f" 置信度={conf}"
            print(f"       结果: {line}")
            print(f"       [原文] {raw}")

        except Exception as e:
            print(f"       [错误] {e}")
            crashes += 1

    print("\n" + "=" * 72)
    print("汇总")
    print(f"  主情绪准确率 : {cat_correct}/{total} = {cat_correct/total*100:.0f}%  (门槛 ≥80%)")
    print(f"  强度准确率   : {int_correct}/{int_total} = {int_correct/max(int_total,1)*100:.0f}%  (±1 容差，门槛 ≥80%)")
    print(f"  附加情绪命中 : {sec_correct}/{sec_total} = {sec_correct/max(sec_total,1)*100:.0f}%  (门槛 ≥71%)")
    print(f"  易混词区分   : {hard_correct}/{hard_total} = {hard_correct/max(hard_total,1)*100:.0f}%  (门槛 ≥75%)")
    print(f"  翻车/错误    : {crashes}")


if __name__ == "__main__":
    main()
