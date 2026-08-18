#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
T0-7 POC：SQLite 本地数据库建模（核心表结构 + 迁移方案 的 Python 原型）
- 落地 schema.sql / migrations/*.sql，零第三方依赖（仅标准库 sqlite3/struct/hashlib/zlib）。
- 依据 T0-2：本机无 .NET SDK，建模阶段用 Python sqlite3 原型对齐 schema，正式落地换 sqlite-net-pcl。
- 迁移：按 migrations/NNNN_*.sql 顺序递增应用；PRAGMA user_version 为权威版本号；
        每次迁移在事务内应用并写入 schema_migrations 审计表（含 SHA-256 校验）。
- 自检：外键完整性 / 归属标记（用户级|猫级双层）/ 向量 BLOB round-trip / 迁移幂等。
- 用法：
    python init_db.py             # 建库 + 迁移 + 播种样例 + 自检（默认 cat.db）
    python init_db.py --fresh     # 删除重建
    python init_db.py --verify    # 仅对现有库跑自检（不迁移、不播种）
"""
import hashlib
import os
import sqlite3
import struct
import sys
import zlib
from datetime import datetime

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
MIGRATIONS_DIR = os.path.join(BASE_DIR, "migrations")
DB_PATH = os.path.join(BASE_DIR, "cat.db")
EMBED_DIM = 512
CONFIDENCE_GATE = 0.5  # 与 T0-5 memory_poc 一致：低于此置信度标「待确认」


# ---------------------------------------------------------------------------
# 数据库连接
# ---------------------------------------------------------------------------
def connect(path):
    conn = sqlite3.connect(path)
    conn.execute("PRAGMA foreign_keys = ON")
    conn.row_factory = sqlite3.Row
    return conn


def now():
    return datetime.now().strftime("%Y-%m-%d %H:%M:%S")


# ---------------------------------------------------------------------------
# 迁移器
# ---------------------------------------------------------------------------
def read_migrations():
    """扫描 migrations/NNNN_*.sql，返回 [(version, name, sql), ...] 升序。"""
    out = []
    for fn in sorted(os.listdir(MIGRATIONS_DIR)):
        if not fn.endswith(".sql"):
            continue
        try:
            version = int(fn.split("_", 1)[0])
        except ValueError:
            continue
        with open(os.path.join(MIGRATIONS_DIR, fn), "r", encoding="utf-8") as f:
            sql = f.read()
        out.append((version, fn, sql))
    return out


def get_user_version(conn):
    return conn.execute("PRAGMA user_version").fetchone()[0]


def apply_migrations(conn):
    """增量应用所有未应用的迁移，返回本次应用的版本号列表。"""
    current = get_user_version(conn)
    applied = []
    for version, name, sql in read_migrations():
        if version <= current:
            continue
        checksum = hashlib.sha256(sql.encode("utf-8")).hexdigest()[:16]
        try:
            conn.execute("BEGIN")
            conn.executescript(sql)
            conn.execute(
                "INSERT INTO schema_migrations (version, name, description, checksum, applied_at) "
                "VALUES (?, ?, ?, ?, ?)",
                (version, name, name, checksum, now()),
            )
            conn.execute(f"PRAGMA user_version = {version}")
            conn.execute("COMMIT")
        except Exception:
            conn.execute("ROLLBACK")
            raise
        applied.append(version)
    return applied


# ---------------------------------------------------------------------------
# 向量：float32 BLOB（小端序），演示用确定性伪向量，正式落地换 bge-small-zh
# ---------------------------------------------------------------------------
def fake_embed(text, dim=EMBED_DIM):
    """确定性伪向量：复用 T0-5 memory_poc 的字符 n-gram 哈希思路，保证可复现。"""
    text = text.strip().lower()
    feats = list(text) + [text[i:i + 2] for i in range(len(text) - 1)]
    vec = [0.0] * dim
    for f in feats:
        h = zlib.crc32(f.encode("utf-8"))
        vec[h % dim] += 1.0 if (h >> 1) % 2 == 0 else -1.0
    return vec


def pack_vec(vec):
    return struct.pack("<%df" % len(vec), *vec)


def unpack_vec(blob):
    return list(struct.unpack("<%df" % (len(blob) // 4), blob))


# ---------------------------------------------------------------------------
# 播种样例：奶盖场景（用户级/猫级记忆 + 情绪 + 提醒 + 心情打卡）
# ---------------------------------------------------------------------------
def seed_sample(conn):
    c = conn.cursor()

    # 用户 + 猫咪
    c.execute(
        "INSERT INTO users (steam_id, display_name, is_guest, created_at, updated_at) "
        "VALUES (NULL, '铲屎官', 1, ?, ?)", (now(), now()),
    )
    user_id = c.lastrowid
    c.execute(
        "INSERT INTO cats (user_id, name, breed, personality, growth_stage, is_adopted, created_at, updated_at) "
        "VALUES (?, '奶盖', '橘猫', '黏人', '成猫', 1, ?, ?)", (user_id, now(), now()),
    )
    cat_id = c.lastrowid

    # 会话 + 消息
    c.execute(
        "INSERT INTO conversations (user_id, cat_id, title, started_at) VALUES (?, ?, '和奶盖的日常', ?)",
        (user_id, cat_id, now()),
    )
    conv_id = c.lastrowid
    c.execute(
        "INSERT INTO messages (conversation_id, role, content, created_at) VALUES (?, 'user', ?, ?)",
        (conv_id, "奶盖，我今天开始准备考研了，以后晚上可能都要看书。", now()),
    )
    msg_id = c.lastrowid
    c.execute(
        "INSERT INTO messages (conversation_id, role, content, created_at) VALUES (?, 'cat', ?, ?)",
        (conv_id, "*蹭蹭* 那铲屎官加油喵～本喵在旁边陪你看书。", now()),
    )

    # 长期记忆（含归属标记双层 + 向量 BLOB）
    mems = [
        # (owner_type, cat_id, subject, relation, object, fact, confidence)
        ("user", None, "用户", "在准备", "考研", "用户正在准备考研", 0.9),
        ("user", None, "用户", "讨厌", "香菜", "用户讨厌吃香菜", 0.9),
        ("cat", cat_id, "猫", "是", "橘猫", "奶盖是一只橘猫", 0.9),
    ]
    for owner_type, mcat_id, subject, relation, object_, fact, conf in mems:
        vec = fake_embed(fact)
        c.execute(
            "INSERT INTO memories (user_id, owner_type, cat_id, subject, relation, object, fact, "
            "confidence, status, source, embedding, embed_dim, embed_model, created_at, updated_at) "
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            (user_id, owner_type, mcat_id, subject, relation, object_, fact, conf,
             "pending" if conf < CONFIDENCE_GATE else "valid", "user_edit",
             pack_vec(vec), len(vec), "bge-small-zh-v1.5", now(), now()),
        )

    # 情绪记录（两级结构，关联上面的用户消息）
    c.execute(
        "INSERT INTO emotion_logs (user_id, message_id, source_text, primary_emotion, intensity, "
        "secondary_emotions, confidence, reason, created_at) "
        "VALUES (?, ?, ?, '焦虑', 3, ?, 0.9, ?, ?)",
        (user_id, msg_id, "奶盖，我今天开始准备考研了，以后晚上可能都要看书。",
         '["压力"]', "提到备考与长期复习安排", now()),
    )

    # 提醒 + 心情打卡
    c.execute(
        "INSERT INTO reminders (user_id, cat_id, type, title, content, schedule_type, trigger_at, created_at) "
        "VALUES (?, ?, '学习', '今晚复习提醒', '*伸懒腰* 铲屎官，该看书啦喵～', 'once', ?, ?)",
        (user_id, cat_id, now(), now()),
    )
    c.execute(
        "INSERT INTO mood_records (user_id, emotion, intensity, note, record_date, created_at) "
        "VALUES (?, '压力', 3, '备考有点累', ?, ?)",
        (user_id, datetime.now().strftime("%Y-%m-%d"), now()),
    )

    conn.commit()
    return user_id, cat_id


# ---------------------------------------------------------------------------
# 自检
# ---------------------------------------------------------------------------
def verify(conn):
    print("=" * 72)
    print("自检：外键 / 归属标记 / 向量 round-trip / 迁移幂等")
    print("=" * 72)
    ok_all = True

    # 1. 外键完整性
    fk_violations = conn.execute("PRAGMA foreign_key_check").fetchall()
    ok = len(fk_violations) == 0
    ok_all &= ok
    print(f"\n[1] 外键完整性        {'✓ 通过' if ok else '✗ 失败'}"
          f"  (foreign_key_check 违规 {len(fk_violations)} 条)")
    for v in fk_violations:
        print(f"    ✗ {dict(v)}")

    # 2. 归属标记：用户级记忆 cat_id 必须为 NULL，猫级记忆 cat_id 必须指向有效猫
    bad_user = conn.execute(
        "SELECT COUNT(*) FROM memories WHERE owner_type='user' AND cat_id IS NOT NULL"
    ).fetchone()[0]
    bad_cat = conn.execute(
        "SELECT COUNT(*) FROM memories WHERE owner_type='cat' AND cat_id IS NULL"
    ).fetchone()[0]
    ok = (bad_user == 0 and bad_cat == 0)
    ok_all &= ok
    print(f"\n[2] 归属标记双层     {'✓ 通过' if ok else '✗ 失败'}"
          f"  (用户级含 cat_id: {bad_user}，猫级缺 cat_id: {bad_cat})")

    # 3. 向量 BLOB round-trip
    rows = conn.execute("SELECT fact, embedding, embed_dim FROM memories WHERE embedding IS NOT NULL").fetchall()
    ok = len(rows) > 0
    for r in rows:
        vec = unpack_vec(r["embedding"])
        if len(vec) != r["embed_dim"] or len(vec) == 0:
            ok = False
        if abs(sum(v * v for v in vec) - sum(v * v for v in fake_embed(r["fact"]))) > 1e-9:
            ok = False
    ok_all &= ok
    print(f"\n[3] 向量 BLOB round-trip {'✓ 通过' if ok else '✗ 失败'}"
          f"  ({len(rows)} 条，dim={rows[0]['embed_dim'] if rows else '-'})")

    # 4. 迁移幂等：再次应用应 0 个新迁移，user_version 不变
    before = get_user_version(conn)
    applied = apply_migrations(conn)
    after = get_user_version(conn)
    ok = (len(applied) == 0 and before == after)
    ok_all &= ok
    print(f"\n[4] 迁移幂等          {'✓ 通过' if ok else '✗ 失败'}"
          f"  (重跑新增 {len(applied)} 个，user_version {before}→{after})")

    print("\n" + "=" * 72)
    print("自检汇总：" + ("全部通过 ✓" if ok_all else "存在失败项 ✗"))
    return ok_all


# ---------------------------------------------------------------------------
# 展示
# ---------------------------------------------------------------------------
def show(conn, user_id, cat_id):
    print("\n" + "=" * 72)
    print("样例数据总览")
    print("=" * 72)

    print("\n【记忆（含归属标记）】")
    for r in conn.execute(
        "SELECT owner_type, cat_id, subject, relation, object, fact, status, confidence "
        "FROM memories ORDER BY owner_type DESC, id"
    ):
        layer = "用户级(共享)" if r["owner_type"] == "user" else f"猫级(cat_id={r['cat_id']})"
        print(f"  [{layer}] [{r['status']}] {r['fact']}"
              f"  ({r['subject']}-{r['relation']}-{r['object']}, 置信度 {r['confidence']})")

    print("\n【情绪记录】")
    for r in conn.execute(
        "SELECT primary_emotion, intensity, secondary_emotions, confidence FROM emotion_logs"
    ):
        print(f"  主情绪={r['primary_emotion']} 强度={r['intensity']}"
              f" 附加={r['secondary_emotions']} 置信度={r['confidence']}")

    print("\n【提醒 / 心情打卡】")
    rem = conn.execute("SELECT type, title, content, schedule_type FROM reminders").fetchone()
    mood = conn.execute("SELECT emotion, intensity, note, record_date FROM mood_records").fetchone()
    print(f"  提醒: [{rem['type']}] {rem['title']} — {rem['content']} ({rem['schedule_type']})")
    print(f"  心情: {mood['record_date']} {mood['emotion']}({mood['intensity']}) {mood['note']}")

    print("\n【库统计】")
    tables = conn.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name"
    ).fetchall()
    print(f"  业务表 {len(tables)} 张: {', '.join(t['name'] for t in tables)}")
    print(f"  user_version = {get_user_version(conn)}")
    print(f"  迁移记录 {conn.execute('SELECT COUNT(*) FROM schema_migrations').fetchone()[0]} 条")


def main():
    fresh = "--fresh" in sys.argv
    verify_only = "--verify" in sys.argv

    if verify_only and not os.path.exists(DB_PATH):
        print(f"[!] {DB_PATH} 不存在，无法 --verify；请先运行 `python init_db.py` 建库")
        return

    if fresh and os.path.exists(DB_PATH):
        os.remove(DB_PATH)

    conn = connect(DB_PATH)

    print("=" * 72)
    print("T0-7 SQLite 建模原型：迁移 → 播种 → 自检")
    print("=" * 72)

    applied = apply_migrations(conn)
    if applied:
        print(f"\n已应用迁移: v{applied}")
    else:
        print(f"\n库已是最新 (user_version={get_user_version(conn)})，无需迁移")

    if not verify_only:
        # 播种去重保护：库中已有数据则跳过，避免重复运行时样例翻倍（重建请用 --fresh）
        if conn.execute("SELECT COUNT(*) FROM users").fetchone()[0] == 0:
            user_id, cat_id = seed_sample(conn)
            show(conn, user_id, cat_id)
        else:
            print("\n[!] 库中已有数据，跳过播种（如需重建样例请用 --fresh）")

    ok = verify(conn)
    conn.close()
    print(f"\n数据库文件: {DB_PATH}")
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
