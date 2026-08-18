-- ============================================================================
-- AI 虚拟猫咪陪伴养成游戏 · SQLite 核心库 Schema（T0-7 交付物）
-- 版本：schema v1（PRAGMA user_version = 1）
-- 说明：单文件本地库，存全部核心数据。纯本地、无服务器。
-- 归属标记：memories.owner_type（'user'=用户级共享 / 'cat'=猫级专属）+ cat_id。
-- 向量存储：memories.embedding 存 float32 BLOB（小端序），检索走本地余弦线性扫描。
-- 迁移与回滚：见 migrations.md；本文件为「当前完整 schema」，等价于 migrations/0001_init.sql。
-- ============================================================================

PRAGMA foreign_keys = ON;

-- ----------------------------------------------------------------------------
-- schema_migrations：迁移审计表（记录已应用迁移，user_version 为权威版本号）
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS schema_migrations (
    version     INTEGER PRIMARY KEY,      -- 迁移版本号（与 PRAGMA user_version 对应）
    name        TEXT NOT NULL,            -- 迁移文件名（如 0001_init.sql）
    description TEXT,                     -- 迁移说明
    checksum    TEXT,                     -- SQL 文件 SHA-256（变更检测）
    applied_at  TEXT NOT NULL             -- 应用时间（ISO8601）
);

-- ----------------------------------------------------------------------------
-- users：用户（本地存档，MVP 默认游客存档，Steam 登录可选）
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    steam_id      TEXT UNIQUE,            -- Steam 账号 ID（游客为 NULL）
    display_name  TEXT,                   -- 玩家称呼（可空）
    is_guest      INTEGER NOT NULL DEFAULT 1,  -- 1=游客存档；0=已绑定 Steam
    settings_json TEXT,                   -- 用户偏好（JSON：通知开关/模型选择等，预留）
    created_at    TEXT NOT NULL,
    updated_at    TEXT NOT NULL
);

-- ----------------------------------------------------------------------------
-- cats：猫咪（品种/性格/外观/三态/等级/亲密度）
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS cats (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id         INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name            TEXT NOT NULL,        -- 名字（如「奶盖」）
    breed           TEXT,                 -- 品种：英短/布偶/橘猫/三花/暹罗
    personality     TEXT,                 -- 性格：黏人/高冷/话痨/懒散/活泼
    appearance_json TEXT,                 -- 外观：毛色/花纹/眼睛颜色/配饰（JSON）
    growth_stage    TEXT NOT NULL DEFAULT '幼猫',  -- 幼猫/成猫/资深伙伴
    level           INTEGER NOT NULL DEFAULT 1,    -- 成长等级
    intimacy        INTEGER NOT NULL DEFAULT 0,    -- 亲密度
    mood            INTEGER NOT NULL DEFAULT 60,   -- 三态：心情 0~100
    energy          INTEGER NOT NULL DEFAULT 60,   -- 三态：精力 0~100
    health          INTEGER NOT NULL DEFAULT 80,   -- 三态：健康 0~100
    is_adopted      INTEGER NOT NULL DEFAULT 0,    -- 是否已领养（当前主猫标记）
    created_at      TEXT NOT NULL,
    updated_at      TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_cats_user ON cats(user_id);

-- ----------------------------------------------------------------------------
-- conversations：会话（一次连续聊天）
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS conversations (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    cat_id     INTEGER NOT NULL REFERENCES cats(id) ON DELETE CASCADE,
    title      TEXT,                     -- 会话标题（可选）
    started_at TEXT NOT NULL,
    ended_at   TEXT                      -- 结束时间（NULL=进行中）
);

CREATE INDEX IF NOT EXISTS idx_conversations_cat ON conversations(cat_id, started_at);

-- ----------------------------------------------------------------------------
-- messages：消息（会话内逐条）
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS messages (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    conversation_id INTEGER NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    role            TEXT NOT NULL CHECK(role IN ('user','cat','system')),
    content         TEXT NOT NULL,
    emotion_id      INTEGER REFERENCES emotion_logs(id),  -- 关联情绪分析结果（用户消息）
    created_at      TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_messages_conversation ON messages(conversation_id, id);

-- ----------------------------------------------------------------------------
-- memories：长期记忆（事实三元组 + 向量片段，含归属标记）
--   归属标记：owner_type='user' 用户级（全猫共享，cat_id=NULL）
--             owner_type='cat'  猫级（仅属某猫，cat_id 指向该猫）
--   质量仲裁：status 待确认/有效/已更新/已删除；confidence 置信度门控
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS memories (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id     INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    owner_type  TEXT NOT NULL CHECK(owner_type IN ('user','cat')),  -- 归属：用户级/猫级
    cat_id      INTEGER REFERENCES cats(id) ON DELETE CASCADE,      -- 猫级记忆指向具体猫；用户级 NULL
    subject     TEXT,                    -- 主体（谁：用户/猫）
    relation    TEXT,                    -- 关系（动词/短语）
    object      TEXT,                    -- 客体（具体内容）
    fact        TEXT NOT NULL,           -- 一句可独立理解的自然语言复述（注入用）
    confidence  REAL NOT NULL DEFAULT 1.0,   -- 置信度 0~1（<0.5 标「待确认」）
    status      TEXT NOT NULL DEFAULT 'valid'
                CHECK(status IN ('pending','valid','updated','deleted')),
    source      TEXT,                    -- 来源：会话 ID / 'user_edit' / 'system'
    embedding   BLOB,                    -- float32 向量（小端序），本地余弦检索用
    embed_dim   INTEGER,                 -- 向量维度（如 512）
    embed_model TEXT,                    -- Embedding 模型（如 bge-small-zh-v1.5）
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL,
    deleted_at  TEXT                     -- 软删除时间（隐私可控，删除即全链路清理）
);

CREATE INDEX IF NOT EXISTS idx_memories_owner ON memories(user_id, owner_type, cat_id);
CREATE INDEX IF NOT EXISTS idx_memories_status ON memories(status);

-- ----------------------------------------------------------------------------
-- emotion_logs：情绪记录（识别对象=用户，两级结构：主情绪 + 附加情绪）
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS emotion_logs (
    id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id            INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    message_id         INTEGER REFERENCES messages(id) ON DELETE SET NULL,  -- 关联来源消息
    source_text        TEXT NOT NULL,       -- 被识别的用户输入文本
    primary_emotion    TEXT NOT NULL,       -- 主情绪：开心/难过/愤怒/焦虑/害怕/压力/疲惫/平静
    intensity          INTEGER NOT NULL,    -- 强度 1~5（平静=0）
    secondary_emotions TEXT,                -- 附加情绪 JSON 数组：感激/期待/放松/孤独/内疚/烦躁/无聊
    confidence         REAL,                -- 置信度
    reason             TEXT,                -- 识别理由
    created_at         TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_emotion_user_time ON emotion_logs(user_id, created_at);

-- ----------------------------------------------------------------------------
-- reminders：提醒任务
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS reminders (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id           INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    cat_id            INTEGER REFERENCES cats(id) ON DELETE SET NULL,  -- 拟人化提醒来源猫（可空）
    type              TEXT NOT NULL CHECK(type IN ('作息','用药','学习','自定义')),
    title             TEXT NOT NULL,
    content           TEXT,                -- 提醒文案（猫咪口吻）
    schedule_type     TEXT NOT NULL CHECK(schedule_type IN ('once','repeat')),
    trigger_at        TEXT,                -- 一次性触发时间（schedule_type='once'）
    repeat_rule       TEXT,                -- 循环规则 JSON/cron（schedule_type='repeat'）
    enabled           INTEGER NOT NULL DEFAULT 1,
    last_triggered_at TEXT,
    created_at        TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_reminders_user ON reminders(user_id, enabled, trigger_at);

-- ----------------------------------------------------------------------------
-- mood_records：心情记录（每日心情打卡，MVP）
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS mood_records (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id     INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    emotion     TEXT NOT NULL,             -- 情绪标签
    intensity   INTEGER NOT NULL,          -- 强度 1~5
    note        TEXT,                      -- 一句话原因
    record_date TEXT NOT NULL,             -- 打卡日期 YYYY-MM-DD
    created_at  TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_mood_user_date ON mood_records(user_id, record_date);

-- ----------------------------------------------------------------------------
-- study_records：学习记录（P2+）
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS study_records (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id          INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    subject          TEXT,                 -- 科目
    duration_minutes INTEGER,              -- 时长（分钟）
    focus_sessions   INTEGER DEFAULT 0,    -- 番茄钟次数
    goal             TEXT,                 -- 目标
    completed        INTEGER NOT NULL DEFAULT 0,
    record_date      TEXT NOT NULL,        -- YYYY-MM-DD
    created_at       TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_study_user_date ON study_records(user_id, record_date);

-- ----------------------------------------------------------------------------
-- health_records：健康记录（P2+）
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS health_records (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id     INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    metric      TEXT NOT NULL,             -- 指标：饮水/睡眠/运动/体重/生理期/用药
    value       REAL NOT NULL,
    unit        TEXT,                      -- 单位：ml/小时/次/kg/片
    note        TEXT,
    record_date TEXT NOT NULL,             -- YYYY-MM-DD
    created_at  TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_health_user_date ON health_records(user_id, record_date);

-- ----------------------------------------------------------------------------
-- anniversaries：纪念日（P4）
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS anniversaries (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id        INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title          TEXT NOT NULL,
    date           TEXT NOT NULL,          -- YYYY-MM-DD
    mode           TEXT NOT NULL DEFAULT 'countdown'
                   CHECK(mode IN ('countdown','countup')),
    note           TEXT,
    remind_enabled INTEGER NOT NULL DEFAULT 1,
    created_at     TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_anniv_user_date ON anniversaries(user_id, date);

-- ----------------------------------------------------------------------------
-- monthly_books：月度回忆（P4）
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS monthly_books (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id      INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    cat_id       INTEGER REFERENCES cats(id) ON DELETE SET NULL,
    month        TEXT NOT NULL,            -- YYYY-MM
    content_json TEXT,                     -- 金句/心情变化/成就/时间线（JSON）
    generated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_monthly_user ON monthly_books(user_id, month);
