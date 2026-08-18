# 🔁 SQLite 迁移与备份恢复方案（T0-7 · 迁移方案）

> 状态：✅ 已完成
> 依据：[`技术栈.md`](../../技术栈.md) §4.1「存档可靠性：表结构变更必须配套迁移脚本与回滚方案」。
> 落地：`migrations/NNNN_*.sql` 增量脚本 + `init_db.py` 迁移器。

---

## 一、版本管理机制

**双记录，`PRAGMA user_version` 为权威：**

| 机制 | 作用 |
|------|------|
| `PRAGMA user_version` | 权威 schema 版本号（随库文件走，备份/恢复后不变，事务内原子更新） |
| `schema_migrations` 表 | 审计日志：`version / name / description / checksum / applied_at`，含每个脚本 SHA-256 |

两者在同一事务内更新，保证「版本号 + 审计」永远一致。

---

## 二、迁移流程（启动时执行）

```
游戏启动 → 打开 cat.db → 读 user_version
  → 扫描 migrations/NNNN_*.sql（升序）
  → 对每个 version > user_version 的脚本：
       BEGIN ─ 执行 SQL ─ 写 schema_migrations ─ 设 user_version ─ COMMIT
       失败则 ROLLBACK（单脚本原子，绝不半套）
  → 迁移完成 → 打开业务功能
```

- **增量应用**：老存档自动补到最新版本，旧数据无损。
- **单脚本原子**：一个迁移一个事务，失败整体回滚，不留半迁移状态。
- **幂等**：重复启动时 `user_version` 已最新，0 个新迁移，直接跳过（`init_db.py` 自检 [4] 已验证）。

---

## 三、迁移脚本规范

- 命名：`NNNN_描述.sql`（如 `0001_init.sql`），`NNNN` 为递增版本号，**只增不改**。
- 内容：单向 `CREATE / ALTER / INSERT`，不回写历史脚本；已发布版本的脚本冻结（改历史脚本会破坏旧存档的确定性迁移）。
- 新字段落地三原则（涉及已有表时）：
  1. 新增列给 `DEFAULT`，避免 `NOT NULL` 无默认值导致老行迁移失败；
  2. 删除/改名列走「新增 + 复制 + 删旧」多步迁移，不直接 `DROP COLUMN`（老版本 SQLite 兼容）；
  3. 结构性大改优先「重建新表 + 回填数据」。
- `schema.sql` 始终等于「最新完整 schema」快照（供人阅读 + 新库直建），与 `migrations/` 累计增量保持一致；v1 时两者相同。

---

## 四、回滚方案

> 单机游戏存档，**回滚 = 恢复上一份备份库文件**，不做逆向 `down` 脚本（逆向脚本易出数据损耗）。

| 场景 | 处理 |
|------|------|
| 迁移失败 | 事务已回滚，库停留在迁移前版本，直接重试或恢复备份 |
| 迁移成功但发现 bug | 恢复「迁移前自动备份」的库文件，回退 `user_version` |
| 存档损坏 | 见 §五「损坏自恢复」 |

**迁移前自动备份**：每次迁移前，把当前 `cat.db` 复制为 `cat.db.bak.<user_version>`（保留最近 N 份），保证任何回滚都有落点。

---

## 五、备份与损坏自恢复（存档可靠性）

对应 PRD NFR「可靠性：定期备份 + 损坏自恢复 + 迁移脚本」。

1. **定期自动备份**：每次正常退出 + 每日首次启动，复制 `cat.db` 到 `backup/` 目录（保留最近 N 份轮转）。
2. **启动校验**：打开库前先跑 `PRAGMA integrity_check`；失败则触发自恢复。
3. **损坏自恢复**：
   - `integrity_check` 失败 → 依次尝试恢复最近一份健康备份；
   - 无备份 → 以「只读模式」抢救已提交数据（`PRAGMA journal_mode` / `REPLACE` 重建）。
4. **WAL 模式**（可选）：`PRAGMA journal_mode=WAL` 提升并发写与崩溃恢复能力；代价是 `.db-wal/.db-shm` 附属文件，备份需连同 WAL checkpoint 一起处理。
5. **Steam 云存档**：云同步时上传「校验和 + 版本号」，恢复时先比对 `user_version`，跨版本云存档先走本地迁移再打开。

---

## 六、落地检查清单（DoD 对应）

- [x] schema 定义完成，含归属标记（`memories.owner_type` + `cat_id`）
- [x] 迁移脚本机制：`PRAGMA user_version` + `schema_migrations` + 增量 `NNNN_*.sql`
- [x] 回滚方案：迁移前备份 + 恢复回退
- [x] 备份与损坏自恢复方案（对应技术栈 §4.1）
- [x] Python 原型验证通过（外键/归属标记/向量 round-trip/迁移幂等）

---

## 七、相关文档

- [schema.md](schema.md) — Schema 定义与表结构详解
- [run_database.md](run_database.md) — 运行与自检说明
