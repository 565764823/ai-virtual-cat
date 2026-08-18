namespace AiCat.Data.Migration
{
    /// <summary>
    /// 迁移清单（只增不改，按版本号递增）。
    /// DDL 逐条复制自 Phase0-筹备/database/migrations/0001_init.sql（权威可读来源）。
    /// </summary>
    public static class Migrations
    {
        public static readonly IReadOnlyList<IMigration> All = new IMigration[]
        {
            new MigrationStep(
                version: 1,
                name: "0001_init.sql",
                description: "初始化核心库（12 张业务表 + 1 张迁移审计表 + 索引）",
                sqlStatements: new[]
                {
                    @"CREATE TABLE schema_migrations (
    version     INTEGER PRIMARY KEY,
    name        TEXT NOT NULL,
    description TEXT,
    checksum    TEXT,
    applied_at  TEXT NOT NULL
)",

                    @"CREATE TABLE users (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    steam_id      TEXT UNIQUE,
    display_name  TEXT,
    is_guest      INTEGER NOT NULL DEFAULT 1,
    settings_json TEXT,
    created_at    TEXT NOT NULL,
    updated_at    TEXT NOT NULL
)",

                    @"CREATE TABLE cats (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id         INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name            TEXT NOT NULL,
    breed           TEXT,
    personality     TEXT,
    appearance_json TEXT,
    growth_stage    TEXT NOT NULL DEFAULT '幼猫',
    level           INTEGER NOT NULL DEFAULT 1,
    intimacy        INTEGER NOT NULL DEFAULT 0,
    mood            INTEGER NOT NULL DEFAULT 60,
    energy          INTEGER NOT NULL DEFAULT 60,
    health          INTEGER NOT NULL DEFAULT 80,
    is_adopted      INTEGER NOT NULL DEFAULT 0,
    created_at      TEXT NOT NULL,
    updated_at      TEXT NOT NULL
)",

                    "CREATE INDEX idx_cats_user ON cats(user_id);",

                    @"CREATE TABLE conversations (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    cat_id     INTEGER NOT NULL REFERENCES cats(id) ON DELETE CASCADE,
    title      TEXT,
    started_at TEXT NOT NULL,
    ended_at   TEXT
)",

                    "CREATE INDEX idx_conversations_cat ON conversations(cat_id, started_at);",

                    @"CREATE TABLE messages (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    conversation_id INTEGER NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    role            TEXT NOT NULL CHECK(role IN ('user','cat','system')),
    content         TEXT NOT NULL,
    emotion_id      INTEGER REFERENCES emotion_logs(id),
    created_at      TEXT NOT NULL
)",

                    "CREATE INDEX idx_messages_conversation ON messages(conversation_id, id);",

                    @"CREATE TABLE memories (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id     INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    owner_type  TEXT NOT NULL CHECK(owner_type IN ('user','cat')),
    cat_id      INTEGER REFERENCES cats(id) ON DELETE CASCADE,
    subject     TEXT,
    relation    TEXT,
    object      TEXT,
    fact        TEXT NOT NULL,
    confidence  REAL NOT NULL DEFAULT 1.0,
    status      TEXT NOT NULL DEFAULT 'valid'
                CHECK(status IN ('pending','valid','updated','deleted')),
    source      TEXT,
    embedding   BLOB,
    embed_dim   INTEGER,
    embed_model TEXT,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL,
    deleted_at  TEXT
)",

                    "CREATE INDEX idx_memories_owner ON memories(user_id, owner_type, cat_id);",
                    "CREATE INDEX idx_memories_status ON memories(status);",

                    @"CREATE TABLE emotion_logs (
    id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id            INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    message_id         INTEGER REFERENCES messages(id) ON DELETE SET NULL,
    source_text        TEXT NOT NULL,
    primary_emotion    TEXT NOT NULL,
    intensity          INTEGER NOT NULL,
    secondary_emotions TEXT,
    confidence         REAL,
    reason             TEXT,
    created_at         TEXT NOT NULL
)",

                    "CREATE INDEX idx_emotion_user_time ON emotion_logs(user_id, created_at);",

                    @"CREATE TABLE reminders (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id           INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    cat_id            INTEGER REFERENCES cats(id) ON DELETE SET NULL,
    type              TEXT NOT NULL CHECK(type IN ('作息','用药','学习','自定义')),
    title             TEXT NOT NULL,
    content           TEXT,
    schedule_type     TEXT NOT NULL CHECK(schedule_type IN ('once','repeat')),
    trigger_at        TEXT,
    repeat_rule       TEXT,
    enabled           INTEGER NOT NULL DEFAULT 1,
    last_triggered_at TEXT,
    created_at        TEXT NOT NULL
)",

                    "CREATE INDEX idx_reminders_user ON reminders(user_id, enabled, trigger_at);",

                    @"CREATE TABLE mood_records (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id     INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    emotion     TEXT NOT NULL,
    intensity   INTEGER NOT NULL,
    note        TEXT,
    record_date TEXT NOT NULL,
    created_at  TEXT NOT NULL
)",

                    "CREATE INDEX idx_mood_user_date ON mood_records(user_id, record_date);",

                    @"CREATE TABLE study_records (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id          INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    subject          TEXT,
    duration_minutes INTEGER,
    focus_sessions   INTEGER DEFAULT 0,
    goal             TEXT,
    completed        INTEGER NOT NULL DEFAULT 0,
    record_date      TEXT NOT NULL,
    created_at       TEXT NOT NULL
)",

                    "CREATE INDEX idx_study_user_date ON study_records(user_id, record_date);",

                    @"CREATE TABLE health_records (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id     INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    metric      TEXT NOT NULL,
    value       REAL NOT NULL,
    unit        TEXT,
    note        TEXT,
    record_date TEXT NOT NULL,
    created_at  TEXT NOT NULL
)",

                    "CREATE INDEX idx_health_user_date ON health_records(user_id, record_date);",

                    @"CREATE TABLE anniversaries (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id        INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title          TEXT NOT NULL,
    date           TEXT NOT NULL,
    mode           TEXT NOT NULL DEFAULT 'countdown'
                   CHECK(mode IN ('countdown','countup')),
    note           TEXT,
    remind_enabled INTEGER NOT NULL DEFAULT 1,
    created_at     TEXT NOT NULL
)",

                    "CREATE INDEX idx_anniv_user_date ON anniversaries(user_id, date);",

                    @"CREATE TABLE monthly_books (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id      INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    cat_id       INTEGER REFERENCES cats(id) ON DELETE SET NULL,
    month        TEXT NOT NULL,
    content_json TEXT,
    generated_at TEXT NOT NULL
)",

                    "CREATE INDEX idx_monthly_user ON monthly_books(user_id, month);",
                })
        };
    }
}
