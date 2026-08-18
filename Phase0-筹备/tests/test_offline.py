#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T0-8 离线单元测试：覆盖 POC（T0-3~T0-5）与数据库（T0-7）中「零依赖、不联网」的纯函数。
- 仅标准库 unittest，与项目「零第三方依赖」风格一致，可在 CI 无模型、无网络环境下跑通。
- 联网函数（chat / classify / extract_facts / embed 的语义向量分支）不在此测，需本地 llama-server。
- 用法：
    python Phase0-筹备/tests/test_offline.py
"""
import importlib.util
import math
import os
import sys
import unittest
from unittest import mock

# 强制 UTF-8 输出（与 POC 脚本一致，避免 Windows 控制台中文乱码）
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

TESTS_DIR = os.path.dirname(os.path.abspath(__file__))
PHASE0_DIR = os.path.dirname(TESTS_DIR)
POC_DIR = os.path.join(PHASE0_DIR, "POC")
DB_DIR = os.path.join(PHASE0_DIR, "database")

PERSONA_PROMPT = os.path.join(POC_DIR, "persona_prompt.md")
EMOTION_PROMPT = os.path.join(POC_DIR, "emotion", "emotion_prompt.md")
MEMORY_PROMPT = os.path.join(POC_DIR, "memory", "memory_prompt.md")


def load_module(name, path):
    """按文件路径加载 POC 脚本为模块（脚本名非标准包，用 importlib 最干净）。"""
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


chat_poc = load_module("chat_poc", os.path.join(POC_DIR, "chat_poc.py"))
emotion_poc = load_module("emotion_poc", os.path.join(POC_DIR, "emotion", "emotion_poc.py"))
memory_poc = load_module("memory_poc", os.path.join(POC_DIR, "memory", "memory_poc.py"))
init_db = load_module("init_db", os.path.join(DB_DIR, "init_db.py"))


class TestPromptLoading(unittest.TestCase):
    """三个 POC 的 load_system_prompt 都能从对应 markdown 提取非空 System Prompt。"""

    def test_persona_prompt_extracts(self):
        self.assertTrue(chat_poc.load_system_prompt(PERSONA_PROMPT).strip())

    def test_emotion_prompt_extracts(self):
        self.assertTrue(emotion_poc.load_system_prompt(EMOTION_PROMPT).strip())

    def test_memory_prompt_extracts(self):
        self.assertTrue(memory_poc.load_system_prompt(MEMORY_PROMPT).strip())


class TestEmotionParsing(unittest.TestCase):
    """T0-4 情绪 JSON 解析。"""

    def test_extract_json_object(self):
        obj = emotion_poc.extract_json('```json\n{"情绪": "开心", "强度": 4}\n```')
        self.assertEqual(obj["情绪"], "开心")
        self.assertEqual(obj["强度"], 4)

    def test_extract_json_with_noise(self):
        obj = emotion_poc.extract_json('好的，识别结果是 {"情绪": "平静", "强度": 0}，理由如下')
        self.assertEqual(obj["情绪"], "平静")

    def test_extract_json_none(self):
        self.assertIsNone(emotion_poc.extract_json("这里没有 JSON 内容"))


class TestMemoryParsing(unittest.TestCase):
    """T0-5 记忆抽取的事实解析。"""

    def test_extract_json_array(self):
        arr = memory_poc.extract_json_array('```json\n[{"事实": "用户考研"}]\n```')
        self.assertEqual(arr, [{"事实": "用户考研"}])

    def test_extract_json_array_none(self):
        self.assertIsNone(memory_poc.extract_json_array("这里没有数组"))

    def test_normalize_fact_valid(self):
        f = memory_poc.normalize_fact({
            "归属": "猫", "置信度": "0.8", "主体": "奶盖",
            "关系": "是", "客体": "橘猫", "事实": "奶盖是橘猫",
        })
        self.assertEqual(f["归属"], "猫")
        self.assertAlmostEqual(f["置信度"], 0.8)
        self.assertEqual(f["主体"], "奶盖")

    def test_normalize_fact_invalid_owner_falls_back(self):
        self.assertEqual(memory_poc.normalize_fact({"归属": "外星人"})["归属"], "用户")

    def test_normalize_fact_confidence_clamped(self):
        self.assertEqual(memory_poc.normalize_fact({"置信度": 5.0})["置信度"], 1.0)
        self.assertEqual(memory_poc.normalize_fact({"置信度": -1.0})["置信度"], 0.0)

    def test_normalize_fact_bad_confidence_defaults(self):
        self.assertAlmostEqual(memory_poc.normalize_fact({"置信度": "不是数字"})["置信度"], 0.5)


class TestEmbedding(unittest.TestCase):
    """T0-5 字符 n-gram 向量（离线可复现基线）。"""

    def test_char_ngram_vector_deterministic(self):
        v1 = memory_poc.char_ngram_vector("用户喜欢小鱼干")
        v2 = memory_poc.char_ngram_vector("用户喜欢小鱼干")
        self.assertEqual(v1, v2)
        self.assertEqual(len(v1), memory_poc.EMBED_DIM)

    def test_char_ngram_vector_normalized(self):
        v = memory_poc.char_ngram_vector("用户喜欢小鱼干")
        norm = math.sqrt(sum(x * x for x in v))
        self.assertAlmostEqual(norm, 1.0, places=6)

    def test_l2_normalize(self):
        v = memory_poc.l2_normalize([3.0, 4.0])
        self.assertAlmostEqual(v[0], 0.6, places=6)
        self.assertAlmostEqual(v[1], 0.8, places=6)

    def test_cosine_similarity(self):
        a = memory_poc.char_ngram_vector("用户喜欢小鱼干")
        same = memory_poc.char_ngram_vector("用户喜欢小鱼干")
        diff = memory_poc.char_ngram_vector("今天天气很好")
        self.assertAlmostEqual(memory_poc.cosine(a, same), 1.0, places=6)
        self.assertGreater(memory_poc.cosine(a, same), memory_poc.cosine(a, diff))


class TestMemoryStore(unittest.TestCase):
    """T0-5 记忆库：插入 / 去重 / 冲突覆盖 / 置信度门控 / 检索。"""

    def setUp(self):
        self.store = memory_poc.MemoryStore()

    def _insert(self, subject, relation, object_, fact, owner="用户", conf=0.9):
        return self.store.insert({
            "主体": subject, "关系": relation, "客体": object_,
            "事实": fact, "归属": owner, "置信度": conf,
        }, "test")

    def _patch_embed(self):
        # MemoryStore.insert 内部调 embed()，离线 patch 为字符 n-gram，避免走网络超时
        return mock.patch.object(memory_poc, "embed", side_effect=memory_poc.char_ngram_vector)

    def test_insert_dedup_conflict(self):
        with self._patch_embed():
            action, m = self._insert("用户", "喜欢", "小鱼干", "用户喜欢小鱼干")
            self.assertEqual(action, "insert")
            self.assertEqual(m["状态"], "有效")

            # 去重：同主体 + 关系 + 客体
            action2, _ = self._insert("用户", "喜欢", "小鱼干", "用户喜欢小鱼干")
            self.assertEqual(action2, "dedup")

            # 冲突：同主体 + 关系、不同客体 → 旧记忆标「已更新」，新记忆 insert
            action3, _ = self._insert("用户", "喜欢", "罐头", "用户现在喜欢罐头")
            self.assertEqual(action3, "insert")
            self.assertEqual(m["状态"], "已更新")

    def test_confidence_gate(self):
        with self._patch_embed():
            _, low = self._insert("用户", "讨厌", "香菜", "用户讨厌香菜", conf=0.3)
            self.assertEqual(low["状态"], "待确认")
            self.assertEqual(len(self.store.valid()), 0)  # 待确认不参与检索

    def test_retrieve_hits_related_memory(self):
        with self._patch_embed():
            self._insert("用户", "在准备", "考研", "用户正在准备考研")
            self._insert("用户", "讨厌", "香菜", "用户讨厌吃香菜")
            self._insert("猫", "是", "橘猫", "奶盖是一只橘猫")
            hits = self.store.retrieve("我最近在准备考研复习")
            self.assertTrue(hits)
            # 查询含「准备/考研/复习」，相关记忆应排在香菜/橘猫之前
            self.assertIn("考研", hits[0][1]["事实"])


class TestInject(unittest.TestCase):
    """T0-5 记忆注入人设。"""

    def test_inject_appends_memories(self):
        base = "你是奶盖"
        mems = [{"事实": "用户正在准备考研"}, {"事实": "用户讨厌香菜"}]
        out = memory_poc.inject(base, mems)
        self.assertIn("你是奶盖", out)
        self.assertIn("用户正在准备考研", out)
        self.assertIn("用户讨厌香菜", out)

    def test_inject_no_memories_returns_system(self):
        self.assertEqual(memory_poc.inject("系统", []), "系统")


class TestDatabase(unittest.TestCase):
    """T0-7 数据库：伪向量确定性 + float32 BLOB round-trip。"""

    def test_fake_embed_deterministic(self):
        v1 = init_db.fake_embed("用户讨厌香菜")
        v2 = init_db.fake_embed("用户讨厌香菜")
        self.assertEqual(v1, v2)
        self.assertEqual(len(v1), init_db.EMBED_DIM)

    def test_fake_embed_differs_for_different_text(self):
        self.assertNotEqual(init_db.fake_embed("用户喜欢小鱼干"), init_db.fake_embed("用户讨厌香菜"))

    def test_pack_unpack_roundtrip(self):
        v = init_db.fake_embed("奶盖是一只橘猫")
        blob = init_db.pack_vec(v)
        self.assertEqual(len(blob), len(v) * 4)  # float32 = 4 字节
        self.assertEqual(init_db.unpack_vec(blob), v)


if __name__ == "__main__":
    unittest.main(verbosity=2)
