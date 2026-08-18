# ▶️ 运行 SQLite 建模原型（T0-7）

> 本目录是 T0-7「SQLite 本地数据库建模」交付物：核心 schema + 迁移方案 + Python 原型。

## 一、文件说明

```
database/
├── schema.sql               # 当前完整 schema（供人阅读 + 新库直建）
├── migrations/
│   └── 0001_init.sql        # 迁移基线（v1，与 schema.sql 等价）
├── init_db.py               # Python 原型：迁移器 + 播种样例 + 四项自检
├── schema.md                # 建模说明（表结构/归属标记/向量/对齐）
├── migrations.md            # 迁移与回滚、备份恢复方案
└── run_database.md          # 本文档
```

## 二、运行步骤

零依赖（仅 Python 标准库），任意 Python 3 均可：

```bash
cd Phase0-筹备/database

python init_db.py --fresh    # 删除重建：建库 + 迁移 + 播种 + 自检（推荐首次）
python init_db.py            # 增量运行：已有库则跳过迁移与播种，仍跑自检
python init_db.py --verify   # 仅自检（不迁移、不播种）
```

## 三、预期输出

1. **迁移**：`已应用迁移: v[1]`，`user_version` 设为 1，写入 `schema_migrations` 审计记录。
2. **样例数据**（奶盖场景）：
   - 记忆 3 条，含归属标记：`用户级(共享)` ×2（考研/香菜）、`猫级(cat_id=1)` ×1（橘猫奶盖）；
   - 情绪记录 1 条（焦虑/强度 3/附加「压力」）；
   - 提醒 1 条 + 心情打卡 1 条。
3. **四项自检**（全部应 `✓ 通过`）：

| # | 自检项 | 验证内容 |
|---|--------|---------|
| 1 | 外键完整性 | `PRAGMA foreign_key_check` 违规 0 条 |
| 2 | 归属标记双层 | 用户级记忆 `cat_id` 恒 NULL；猫级记忆 `cat_id` 必非空 |
| 3 | 向量 BLOB round-trip | float32 打包→落库→解包维度与数值一致 |
| 4 | 迁移幂等 | 重跑迁移新增 0 个，`user_version` 不变 |

## 四、正式落地衔接（Phase 1）

- 用 **sqlite-net-pcl** 在 C# 侧按 `schema.sql` 建同构 ORM 实体；`embedding` 用 `byte[]` 对应 BLOB。
- 复用 `init_db.py` 的四项自检思路，移植为 Unity 内的「启动自检 + 迁移」入口。
- 迁移与备份恢复按 [migrations.md](migrations.md) 执行。

## 五、相关文档

- [schema.md](schema.md) — Schema 定义与表结构详解
- [migrations.md](migrations.md) — 迁移与回滚、备份恢复方案
