# 🔄 CI/CD 流水线（T0-8）

> 状态：✅ 已完成
> 依据：[`计划.md`](计划.md) T0-8「自动构建 + 测试」，前置 T0-6 脚手架。
> 落地：GitHub Actions 工作流 [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) + 离线单元测试 [`tests/test_offline.py`](tests/test_offline.py) + Unity 构建脚本 [`scaffold/Assets/Editor/BuildScript.cs`](scaffold/Assets/Editor/BuildScript.cs)。

---

## 一、流水线概览

```
push → main / 发起 PR / 手动触发
  ├─ python-tests ────────────── ① 语法编译检查
  │                             ② 离线单元测试（POC 纯函数）
  │                             ③ SQLite 建模自检（迁移+播种+四项自检）
  │
  ├─ check-unity-license ────── 探测是否配置 UNITY_LICENSE（未配置则跳过构建，不红）
  │
  └─ unity-build ────────────── GameCI 自动构建 Win64 + 上传产物（需许可证）
```

**设计原则**：测试「零依赖、离线可跑」，不依赖本地 llama-server / 模型 / 联网，保证 CI 稳定；构建「许可证门控」，未配置 Unity 许可证时整 job 跳过（显示为 skipped），不影响其他 job 通过。

---

## 二、三个 Job 说明

| Job | 触发时机 | 内容 | 依赖 |
|-----|---------|------|------|
| `python-tests` | 每次 push/PR | 编译检查 → 单元测试 → SQLite 自检 | 仅标准库 Python |
| `check-unity-license` | 每次 push/PR | 探测 `UNITY_LICENSE` secret 是否已配置 | 无 |
| `unity-build` | 许可证已配置时 | GameCI 构建 `StandaloneWindows64` + 上传产物 | Unity 6 + 许可证 |

### 2.1 python-tests（核心，立即生效）

对应本阶段「测试」验收，覆盖三大块：

1. **语法编译检查**：`python -m compileall` 扫全部 POC / database / tests 脚本，防止脚本改坏无法运行。
2. **离线单元测试**（`tests/test_offline.py`，24 项）：覆盖 POC 与数据库的**纯函数**，不联网、不加载模型——
   - 人设/情绪/记忆 Prompt 提取（`load_system_prompt`）；
   - 情绪 JSON 解析、记忆事实解析（`extract_json` / `normalize_fact`）；
   - 字符 n-gram 向量确定性 + L2 归一 + 余弦相似度；
   - 记忆库去重/冲突覆盖/置信度门控/检索 top-k；
   - 数据库伪向量确定性 + float32 BLOB round-trip。
3. **SQLite 建模自检**：`python database/init_db.py --fresh`，跑 T0-7 的四项自检（外键 / 归属标记 / 向量 round-trip / 迁移幂等），任一失败退出码非 0 → CI 红。

### 2.2 unity-build（许可证门控）

对应「构建」验收：GameCI `unity-builder` 读取 `scaffold/ProjectSettings/ProjectVersion.txt` 的 Unity 6（6000.0.81f1），调用 `AiCat.Editor.BuildScript.BuildWindows` 编译并打包 Win64。**构建本身即验证「脚手架编译通过」**（脚本有任何编译错误都会导致构建失败）。

---

## 三、启用 Unity 自动构建（配置许可证）

个人许可证即可，GameCI 支持「首次用账号密码 + 序列号激活，之后用生成的 `.ulf` 续期」。步骤如下：

1. **准备个人许可证文件**：
   - 按 GameCI 官方流程，用 `game-ci/unity-activate` 工作流，输入 `UNITY_EMAIL` / `UNITY_PASSWORD`，产出 `Unity_v20XX.x.ulf` 激活文件（一次性）。
2. **配置仓库 Secrets**（GitHub → Settings → Secrets and variables → Actions）：
   - `UNITY_LICENSE`：粘贴 `.ulf` 文件全文；
   - `UNITY_EMAIL` / `UNITY_PASSWORD`：Unity 账号（激活续期用）。
3. **触发构建**：配置后下次 push / PR，或手动 `workflow_dispatch` 即自动构建；产物在 Actions 页的 Artifacts 里下载。

> 未配置许可证时，`unity-build` 自动跳过，不影响 `python-tests` 通过。Unity 6 镜像是否已由 GameCI 发布，以 `game-ci/unity-builder` 实际拉取 `unityci/editor:ubuntu-6000.0.81f1-*` 为准；若 6000.0.81f1 镜像缺失，需把 `ProjectVersion.txt` 对齐到 GameCI 已发布镜像的 Unity 6 小版本。

---

## 四、本地运行（等价于 CI 的 python-tests）

零依赖，任意 Python 3.8+：

```bash
cd AI\ cat          # 项目根

# ① 语法编译检查
python -m compileall -q Phase0-筹备/POC Phase0-筹备/database Phase0-筹备/tests

# ② 离线单元测试
python Phase0-筹备/tests/test_offline.py

# ③ SQLite 建模自检
python Phase0-筹备/database/init_db.py --fresh
```

三条全绿即等同 CI 的 `python-tests` 通过。

---

## 五、发布（Release 回档点）

- 本流水线**不自动发 release**；release 仍由「任务完成收尾流程」手动生成，作为可回滚的版本节点（`v0.0.x-m<里程碑>`）。
- 后续如需自动打包发布，可在 `unity-build` 产物基础上追加 `softprops/action-gh-release` 或 `ncipollo/release-action`（Phase 1 起再评估，避免与手动回档点冲突）。

---

## 六、已知边界（Phase 1 再补）

- **Unity 测试（Test Runner）**：脚手架暂无 EditMode/PlayMode 测试程序集，`unity-build` 只做「编译 + 打包」验证；Phase 1 引入正式功能测试后再接 `game-ci/unity-test-runner`。
- **多平台构建**：当前仅 Win64（Steam 桌面主平台）；macOS/Linux 构建可仿 `BuildWindows` 加 `BuildMac` / `BuildLinux` 方法。
- **缓存**：`Library` 缓存按 Assets/Packages/ProjectSettings 哈希失效，Unity 首次构建仍偏慢（拉镜像 + 导入），属正常。
- **模型不进 CI**：LLM 相关的端到端测试（人设/情绪/记忆的 LLM 调用）依赖本地模型，CI 不跑，由 `test_offline.py` 的纯函数 + 本地 `run_*.md` 手工评测覆盖。

---

## 七、相关文档

- [计划.md](计划.md) — T0-8 任务定义
- [../.github/workflows/ci.yml](../.github/workflows/ci.yml) — 流水线定义
- [tests/test_offline.py](tests/test_offline.py) — 离线单元测试
- [scaffold/README.md](scaffold/README.md) — 脚手架（T0-6）
- [database/run_database.md](database/run_database.md) — SQLite 建模（T0-7）
