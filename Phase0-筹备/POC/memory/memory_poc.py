#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T0-5 POC：本地记忆抽取与注入（抽取 → 向量化 → 检索 → 注入 最小闭环）
- 抽取：通过 llama-server（7B，端口 8081）从对话抽取「用户/猫」的结构化事实（三元组 + 归属 + 置信度）。
- 向量化：零依赖纯 Python 字符 n-gram 哈希向量（本地余弦检索的可复现基线）；
          若服务以 --embeddings 启动，则优先走 /v1/embeddings 语义向量（正式落地换 bge-small-zh）。
- 检索：本地余弦相似度 top-k。
- 注入：把检索到的记忆拼入猫咪人设 System Prompt，实现「记得」。
- 质量仲裁：置信度门控（低置信标「待确认」）+ 同主体同关系去重 + 冲突覆盖（新事实覆盖旧事实）。
- 零第三方依赖（仅标准库），适配 Python 3.14。
用法：
    python memory_poc.py           # 演示完整闭环（抽取→向量化→检索→注入）
    python memory_poc.py --eval    # 运行评测集并输出抽取召回 + 检索命中指标
"""
import json
import math
import re
import sys
import urllib.request
import zlib
from datetime import datetime

# 强制 UTF-8 输出，避免 Windows 控制台中文乱码
sys.stdout.reconfigure(encoding="utf-8", errors="replace")

BASE_URL = "http://127.0.0.1:8081/v1/chat/completions"
EMBED_URL = "http://127.0.0.1:8081/v1/embeddings"
EXTRACT_PROMPT_FILE = "memory_prompt.md"
PERSONA_PROMPT_FILE = "../persona_prompt.md"

EMBED_DIM = 512
CONFIDENCE_GATE = 0.5   # 低于此置信度的记忆标「待确认」，不参与检索注入
RETRIEVE_K = 3

# ---------------------------------------------------------------------------
# 提示词加载（与 T0-3/T0-4 同款：取最后一个 ``` 代码块）
# ---------------------------------------------------------------------------
def load_system_prompt(path):
    with open(path, "r", encoding="utf-8") as f:
        text = f.read()
    blocks = text.split("```")
    code_blocks = [blocks[i].strip() for i in range(1, len(blocks), 2)]
    return code_blocks[-1] if code_blocks else ""


# ---------------------------------------------------------------------------
# LLM 调用（OpenAI 兼容 chat/completions）
# ---------------------------------------------------------------------------
def chat(system: str, user: str, temperature=0.7, max_tokens=256) -> str:
    payload = {
        "model": "local",
        "messages": [
            {"role": "system", "content": system},
            {"role": "user", "content": user},
        ],
        "temperature": temperature,
        "top_p": 0.9,
        "repeat_penalty": 1.15,
        "max_tokens": max_tokens,
        "stream": False,
    }
    req = urllib.request.Request(
        BASE_URL,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=180) as resp:
        data = json.loads(resp.read().decode("utf-8"))
    return data["choices"][0]["message"]["content"].strip()


# ---------------------------------------------------------------------------
# 抽取：对话 → 结构化事实列表
# ---------------------------------------------------------------------------
def extract_json_array(text):
    """从模型输出里抠出第一个 JSON 数组（容忍 markdown 代码块、前后杂字）。"""
    text = re.sub(r"```[a-zA-Z]*", "", text)
    m = re.search(r"\[.*\]", text, re.DOTALL)
    if not m:
        return None
    try:
        return json.loads(m.group(0))
    except json.JSONDecodeError:
        return None


def normalize_fact(item: dict) -> dict:
    owner = item.get("归属", "用户")
    if owner not in ("用户", "猫"):
        owner = "用户"
    try:
        conf = float(item.get("置信度", 0.5))
    except (TypeError, ValueError):
        conf = 0.5
    conf = max(0.0, min(1.0, conf))
    return {
        "主体": str(item.get("主体", "用户")),
        "关系": str(item.get("关系", "")),
        "客体": str(item.get("客体", "")),
        "事实": str(item.get("事实", "")),
        "归属": owner,
        "置信度": conf,
    }


def extract_facts(system: str, conversation: str):
    """调用本地 LLM 抽取事实，返回 [fact, ...]。"""
    user = f"【猫咪与用户的对话】\n{conversation}"
    raw = chat(system, user, temperature=0.1, max_tokens=600)
    arr = extract_json_array(raw)
    facts = []
    if isinstance(arr, list):
        for item in arr:
            if isinstance(item, dict):
                facts.append(normalize_fact(item))
    return facts, raw


# ---------------------------------------------------------------------------
# 向量化：本地余弦检索（零依赖字符 n-gram 基线 + 可选语义向量）
# ---------------------------------------------------------------------------
def l2_normalize(vec):
    norm = math.sqrt(sum(v * v for v in vec)) or 1.0
    return [v / norm for v in vec]


def char_ngram_vector(text: str, dim=EMBED_DIM):
    """纯 Python 字符 n-gram 哈希向量（unigram + bigram，L2 归一）。
    零依赖、确定性、离线可用；作为「本地余弦相似度检索」的可复现基线。"""
    text = text.strip().lower()
    features = list(text)
    features += [text[i:i + 2] for i in range(len(text) - 1)]
    vec = [0.0] * dim
    for feat in features:
        h = zlib.crc32(feat.encode("utf-8"))
        idx = h % dim
        sign = 1.0 if (h >> 1) % 2 == 0 else -1.0
        vec[idx] += sign
    return l2_normalize(vec)


def embed(text: str):
    """优先用服务端语义向量；服务不支持（未加 --embeddings）则回落字符 n-gram 基线。"""
    try:
        payload = {"input": text, "model": "local"}
        req = urllib.request.Request(
            EMBED_URL,
            data=json.dumps(payload).encode("utf-8"),
            headers={"Content-Type": "application/json"},
            method="POST",
        )
        with urllib.request.urlopen(req, timeout=30) as resp:
            data = json.loads(resp.read().decode("utf-8"))
        return l2_normalize(data["data"][0]["embedding"])
    except Exception:
        return char_ngram_vector(text)


def cosine(a, b):
    """两个向量均已 L2 归一，点积即余弦相似度。"""
    return sum(x * y for x, y in zip(a, b))


# ---------------------------------------------------------------------------
# 记忆库：去重 + 冲突仲裁 + 置信度门控
# ---------------------------------------------------------------------------
class MemoryStore:
    def __init__(self):
        self.memories = []
        self._next_id = 1
        self.events = []  # 质量仲裁日志

    def _key(self, m):
        return (m["主体"], m["关系"])

    def insert(self, fact: dict, source: str):
        """插入一条记忆。返回 (action, memory)，action ∈ insert/dedup/conflict。"""
        m = dict(fact)
        m["id"] = self._next_id
        m["来源"] = source
        m["时间"] = datetime.now().strftime("%Y-%m-%d %H:%M")
        m["状态"] = "待确认" if m.get("置信度", 1.0) < CONFIDENCE_GATE else "有效"
        m["向量"] = embed(m["事实"])

        k = self._key(m)
        for old in self.memories:
            if self._key(old) == k:
                if old["客体"] == m["客体"]:
                    self.events.append(f"去重：跳过「{m['事实']}」")
                    return ("dedup", old)
                old["状态"] = "已更新"
                self.events.append(f"冲突仲裁：新「{m['事实']}」覆盖旧「{old['事实']}」")

        self.memories.append(m)
        self._next_id += 1
        return ("insert", m)

    def valid(self):
        return [m for m in self.memories if m["状态"] == "有效"]

    def retrieve(self, query: str, k=RETRIEVE_K):
        qv = embed(query)
        scored = [(cosine(qv, m["向量"]), m) for m in self.valid()]
        scored.sort(key=lambda x: -x[0])
        return scored[:k]


# ---------------------------------------------------------------------------
# 注入：把检索到的记忆拼入猫咪人设 System Prompt
# ---------------------------------------------------------------------------
def inject(system: str, memories) -> str:
    if not memories:
        return system
    lines = "\n".join(f"- {m['事实']}" for m in memories)
    block = (
        "\n\n【你的长期记忆（关于铲屎官，请在合适时自然使用，不要生硬罗列，不要主动背出记忆）】\n"
        + lines
    )
    return system + block


# ---------------------------------------------------------------------------
# 评测数据（ground truth 见 memory_test_cases.md）
# ---------------------------------------------------------------------------
CONVERSATIONS = [
    {
        "id": "C1",
        "text": (
            "铲屎官：奶盖，我今天开始准备考研了，以后晚上可能都要看书。\n"
            "奶盖：*竖耳朵* 考研是啥？能陪本喵玩吗喵～\n"
            "铲屎官：就是我以后要去读研究生啦，最近得好好复习。\n"
            "奶盖：*蹭蹭* 那铲屎官加油喵～本喵在旁边陪你看书。\n"
            "铲屎官：对了，我喝咖啡提神，一喝就精神。\n"
            "奶盖：*探头* 咖啡是什么味道？本喵也想闻闻喵～"
        ),
        # 期望：出现这些关键词即算抽取成功
        "expect": ["考研", "咖啡"],
    },
    {
        "id": "C2",
        "text": (
            "铲屎官：奶盖，你知不知道，我最讨厌吃香菜了，闻到就想吐。\n"
            "奶盖：*炸毛* 香菜？本喵也觉得那东西怪怪的喵！\n"
            "铲屎官：还是你最懂我。对了，我给你起名叫奶盖，因为你是橘色的，像奶茶上面的奶盖。\n"
            "奶盖：*打滚* 本喵叫奶盖，才不是吃的喵～\n"
            "铲屎官：哈哈，你就是我最爱的橘猫奶盖。"
        ),
        "expect": ["香菜", "奶盖", "橘猫"],
    },
    {
        "id": "C3",
        "text": (
            "铲屎官：奶盖，我下周有个重要的面试，好紧张。\n"
            "奶盖：*耷拉耳朵* 铲屎官别怕，本喵给你踩奶打气喵～\n"
            "铲屎官：谢谢奶盖，我面试的是程序员岗位。\n"
            "奶盖：*甩尾巴* 程序员是不是天天敲键盘？那铲屎官陪本喵玩的时间更少了喵。"
        ),
        "expect": ["面试", "程序员"],
    },
]

QUERIES = [
    {"query": "我最近晚上都在复习，好累", "expect": "考研"},
    {"query": "今天外卖想点香菜味的，怎么样", "expect": "香菜"},
    {"query": "奶盖你是什么颜色的猫呀", "expect": "橘猫"},
    {"query": "面试好紧张怎么办", "expect": "面试"},
    {"query": "给我冲杯咖啡吧", "expect": "咖啡"},
]


# ---------------------------------------------------------------------------
# 演示：完整闭环走一遍
# ---------------------------------------------------------------------------
def demo(extract_system, persona_system):
    store = MemoryStore()
    print("=" * 72)
    print("T0-5 记忆闭环演示：抽取 → 向量化 → 检索 → 注入")
    print("=" * 72)

    # 阶段 1：抽取 + 落库
    print("\n【阶段 1】抽取：从对话抽取结构化事实并落库")
    for conv in CONVERSATIONS:
        print(f"\n  ▸ 对话 {conv['id']}")
        facts, _raw = extract_facts(extract_system, conv["text"])
        if not facts:
            print("    [无事实抽出]")
            continue
        for f in facts:
            action, m = store.insert(f, conv["id"])
            tag = {"insert": "＋新增", "dedup": "＝去重", "conflict": "⇄覆盖"}[action]
            print(
                f"    {tag} [{m['归属']}·{m['状态']}] {m['事实']}"
                f"  (置信度 {m['置信度']:.2f}, {m['主体']}-{m['关系']}-{m['客体']})"
            )

    print("\n  质量仲裁日志：")
    for e in store.events:
        print(f"    - {e}")

    # 阶段 2：检索
    print("\n【阶段 2】检索：对查询做本地余弦 top-k")
    for q in QUERIES:
        print(f"\n  ▸ 查询：{q['query']}")
        hits = store.retrieve(q["query"])
        for i, (score, m) in enumerate(hits, 1):
            print(f"    #{i} (相似度 {score:.3f}) {m['事实']}")

    # 阶段 3：注入 + 记得
    print("\n【阶段 3】注入：检索到的记忆拼入人设，猫咪「记得」")
    probe = QUERIES[2]["query"]  # "奶盖你是什么颜色的猫呀"
    hits = store.retrieve(probe)
    memories = [m for _, m in hits]
    system = inject(persona_system, memories)
    print(f"\n  ▸ 铲屎官：{probe}")
    reply = chat(system, probe, temperature=0.7, max_tokens=256)
    print(f"    奶盖  ：{reply}")


# ---------------------------------------------------------------------------
# 评测：抽取召回 + 检索命中
# ---------------------------------------------------------------------------
def evaluate(extract_system):
    print("=" * 72)
    print("T0-5 记忆 POC 评测")
    print("=" * 72)

    # 抽取召回：每个期望关键词是否出现在抽出的任何「事实/客体」里
    store = MemoryStore()
    rec_total = rec_hit = 0
    print("\n【抽取召回】")
    for conv in CONVERSATIONS:
        facts, _raw = extract_facts(extract_system, conv["text"])
        for f in facts:
            store.insert(f, conv["id"])
        pool = " ".join(f["事实"] + f["客体"] for f in facts)
        print(f"\n  对话 {conv['id']} 抽出 {len(facts)} 条：")
        for f in facts:
            print(f"    - [{f['归属']}] {f['事实']}")
        for kw in conv["expect"]:
            rec_total += 1
            ok = kw in pool
            rec_hit += ok
            print(f"    期望「{kw}」{'✓' if ok else '✗'}")

    # 检索命中：top-k 里是否含期望关键词
    hit_total = hit_ok = 0
    print("\n【检索命中 top-3】")
    for q in QUERIES:
        hits = store.retrieve(q["query"])
        pool = " ".join(m["事实"] + m["客体"] for _, m in hits)
        hit_total += 1
        ok = q["expect"] in pool
        hit_ok += ok
        top = " / ".join(m["事实"] for _, m in hits)
        print(f"\n  查询「{q['query']}」→ 期望「{q['expect']}」{'✓' if ok else '✗'}")
        print(f"    top-3: {top}")

    print("\n" + "=" * 72)
    print("汇总")
    print(f"  抽取召回 : {rec_hit}/{rec_total} = {rec_hit/max(rec_total,1)*100:.0f}%  (门槛 ≥80%)")
    print(f"  检索命中 : {hit_ok}/{hit_total} = {hit_ok/max(hit_total,1)*100:.0f}%  (门槛 ≥60%)")


def main():
    extract_system = load_system_prompt(EXTRACT_PROMPT_FILE)
    persona_system = load_system_prompt(PERSONA_PROMPT_FILE)
    if not extract_system:
        print(f"[!] 未能从 {EXTRACT_PROMPT_FILE} 提取 System Prompt")
        return
    if not persona_system:
        print(f"[!] 未能从 {PERSONA_PROMPT_FILE} 提取 System Prompt")

    if "--eval" in sys.argv:
        evaluate(extract_system)
    else:
        demo(extract_system, persona_system)


if __name__ == "__main__":
    main()
