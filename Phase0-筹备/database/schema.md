# 🗄️ SQLite 本地数据库建模（T0-7 · Schema 定义）

> 状态：✅ 已完成
> 依据：[`技术栈.md`](../../技术栈.md) §4、[`技术选型确认.md`](../技术选型确认.md) §3.5、[`PRD`](../产品需求文档PRD.md) §四、[`计划.md`](../计划.md) T0-7。
> 落地文件：[`schema.sql`](schema.sql)（当前完整 schema）、[`migrations/0001_init.sql`](migrations/0001_init.sql)（迁移基线）、[`init_db.py`](init_db.py)（Python 原型）。
> 迁移与回滚、备份恢复见 [`migrations.md`](migrations.md)。

---

## 一、概览

| 项 | 内容 |
|----|------|
| 数据库 | SQLite 单文件库（`cat.db`），纯本地、无服务器 |
| ORM | 正式落地用 **sqlite-net-pcl**（C#）；本阶段无 .NET SDK，用 **Python sqlite3 原型**对齐 schema |
| 版本机制 | `PRAGMA user_version`（权威）+ `schema_migrations` 审计表 |
| 核心约定 | 记忆含**归属标记**（用户级/猫级）；向量存 **float32 BLOB**；外键 `ON DELETE` 语义明确 |

**表清单（13 张）**：11 张业务表 + 1 张迁移审计表 `schema_migrations` + 内部 `sqlite_sequence`。

| 表 | 用途 | 优先级 |
|----|------|--------|
| `users` | 用户（本地存档，游客/Steam） | MVP |
| `cats` | 猫咪（品种/性格/外观/三态/等级/亲密度） | MVP |
| `conversations` | 会话 | MVP |
| `messages` | 消息 | MVP |
| `memories` | 长期记忆（事实 + 向量，含归属标记） | MVP 建表，P2 启用 |
| `emotion_logs` | 情绪记录（用户，两级结构） | MVP 建表，P2 启用 |
| `reminders` | 提醒任务 | MVP |
| `mood_records` | 心情记录 | MVP |
| `study_records` | 学习记录 | P2+ |
| `health_records` | 健康记录 | P2+ |
| `anniversaries` | 纪念日 | P4 |
| `monthly_books` | 月度回忆 | P4 |

> 说明：MVP 阶段即建好全部核心表（含 P2+/P4 表），避免后续返工；未启用的表仅建结构不写数据。

---

## 二、关键设计决策

### 2.1 记忆归属标记（双层）—— 本任务核心风险点

多猫共存是 P2 才做，但**归属字段现在就必须定好**（对应计划 §3.2、流程路线 §十二风险）。`memories` 用两个字段表达「用户级 / 猫级」双层归属：

| 字段 | 含义 |
|------|------|
| `owner_type` | `'user'` = 用户级（描述用户自身，全猫共享）；`'cat'` = 猫级（描述某只猫，专属） |
| `cat_id` | 猫级记忆指向具体猫（外键）；**用户级恒为 NULL** |

- **用户级（共享）**：`owner_type='user'`，`cat_id=NULL` —— 例「用户讨厌吃香菜」，任意猫都该记得。
- **猫级（专属）**：`owner_type='cat'`，`cat_id=奶盖` —— 例「奶盖是一只橘猫」，只属于奶盖。

检索注入时：用户级记忆全量可用，猫级记忆按当前会话的 `cat_id` 过滤。

### 2.2 向量存储（float32 BLOB）

- `embedding BLOB` 存 float32 数组（小端序），配合 `embed_dim` + `embed_model` 记录维度与模型。
- 检索走**本地余弦线性扫描**（数据量小，无需 ANN / 独立向量库，符合技术栈 §3.2）。
- C# 侧用 `Buffer.BlockCopy` 转 `float[]`；Python 侧用 `struct` 打包，两者字节序一致（小端）。
- `embed_model` 定版 `bge-small-zh-v1.5`（512 维）；接入困难时 MVP 退化为关键词/BM25 检索（T0-5 已验证兜底）。

### 2.3 质量仲裁与隐私（记忆生命周期）

| 字段 | 作用 |
|------|------|
| `confidence` | 0~1；< 0.5 标 `status='pending'`（待确认，不参与检索），与 T0-5 `CONFIDENCE_GATE=0.5` 一致 |
| `status` | `pending` / `valid` / `updated` / `deleted` |
| `source` | 来源会话 ID / `user_edit` / `system` |
| `deleted_at` | **软删除**；用户删除记忆即置 `deleted`，并全链路清理（向量/摘要/回忆/备份/云存档副本），符合 PRD 隐私要求 |

### 2.4 时间戳

- 统一存 `TEXT`，格式 `YYYY-MM-DD HH:MM:SS`（本地时间，与 T0-5 memory_poc 一致）。
- 日期类字段（`record_date`、`date`、`month`）单独存 `YYYY-MM-DD` / `YYYY-MM`，便于按日/月聚合。

### 2.5 外键语义

- 用户/猫删除时级联清理其专属数据：`conversations`、`messages`、`memories`、`mood_records`、`study_records`、`health_records`、`anniversaries` 均 `ON DELETE CASCADE`。
- 仅「引用关系」用 `ON DELETE SET NULL`：`messages.emotion_id`、`emotion_logs.message_id`、`reminders.cat_id`、`monthly_books.cat_id`。

---

## 三、核心表字段详解

### 3.1 `memories`（长期记忆）

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | INTEGER PK | |
| `user_id` | INTEGER FK | 归属用户 |
| `owner_type` | TEXT | `user`/`cat`（归属标记） |
| `cat_id` | INTEGER FK NULL | 猫级记忆指向具体猫 |
| `subject` / `relation` / `object` | TEXT | 事实三元组（主体/关系/客体） |
| `fact` | TEXT | 一句可独立理解的自然语言复述（注入用） |
| `confidence` | REAL | 置信度 0~1 |
| `status` | TEXT | `pending`/`valid`/`updated`/`deleted` |
| `source` | TEXT | 来源 |
| `embedding` / `embed_dim` / `embed_model` | BLOB/INT/TEXT | 向量 |
| `created_at` / `updated_at` / `deleted_at` | TEXT | 时间 |

> 与 T0-5 `memory_poc.py` 的字段一一对齐：`主体→subject`、`关系→relation`、`客体→object`、`事实→fact`、`归属→owner_type`、`置信度→confidence`、`状态→status`、`来源→source`、`向量→embedding`。

### 3.2 `emotion_logs`（情绪记录）

| 字段 | 类型 | 说明 |
|------|------|------|
| `primary_emotion` | TEXT | 主情绪：开心/难过/愤怒/焦虑/害怕/压力/疲惫/平静 |
| `intensity` | INTEGER | 强度 1~5（平静=0） |
| `secondary_emotions` | TEXT | 附加情绪 JSON 数组：感激/期待/放松/孤独/内疚/烦躁/无聊 |
| `confidence` / `reason` | REAL/TEXT | 置信度 / 识别理由 |
| `message_id` | FK NULL | 关联来源消息 |

> 与 T0-4 `emotion_poc.py` 对齐：`情绪→primary_emotion`、`强度→intensity`、`附加情绪→secondary_emotions`（JSON 数组）、`置信度→confidence`、`理由→reason`。识别对象为**用户**，与猫咪「三态」是两套独立数据（PRD §2.3 概念区分）。

### 3.3 其余表（字段见 schema.sql 注释）

- `cats` 三态：`mood`/`energy`/`health`（0~100），`intimacy` 亲密度，`is_adopted` 主猫标记。
- `reminders`：`schedule_type`（once/repeat）+ `trigger_at`（一次性）+ `repeat_rule`（循环，JSON/cron）。
- `messages.role`：`user`/`cat`/`system`。

---

## 四、与 AI 引擎的衔接（T0-6/T0-7 联调点）

```
Unity(C#)  →  sqlite-net-pcl  →  cat.db
   │ 对话                          ▲
   ▼                               │ 检索注入（本地余弦，按 owner_type+cat_id 过滤）
ChatService ──组装 Prompt
   │ 记忆抽取（T0-5 落地）── 写入 memories（embedding BLOB）
   │ 情绪识别（T0-4 落地）── 写入 emotion_logs
```

- 记忆检索 SQL 范式：`WHERE user_id=? AND status='valid' AND (owner_type='user' OR (owner_type='cat' AND cat_id=?))`。
- 向量比对在应用层做（取出候选 `embedding` 后余弦打分），SQLite 不做向量索引。

---

## 五、相关文档

- [migrations.md](migrations.md) — 迁移与回滚、备份恢复方案
- [run_database.md](run_database.md) — 运行与自检说明
- [init_db.py](init_db.py) — Python 原型源码
