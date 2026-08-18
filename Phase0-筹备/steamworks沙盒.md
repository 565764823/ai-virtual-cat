# 🔐 Steamworks 沙盒环境搭建（T0-9）

> 状态：⏭️ 整体后置到 Phase 6 发行前（T0-9 已简化为「本地开发环境」，Steamworks 沙盒移至 Phase 6「Steamworks 集成」）。
> 依据：[`流程路线.md`](../流程路线.md) Phase 6「Steamworks 集成」，前置 T0-9（本地环境已完成）。
> 本地开发环境见 [`环境搭建说明.md`](环境搭建说明.md)；本文档作为 Phase 6 沙盒搭建的参考，届时再按此执行。

---

## 一、总览

Steamworks 是 Valve 提供的发行与平台能力 SDK。本项目**无自建后端**，在线能力（登录 / 成就 / 云存档）全部复用 Steam（见 [`技术栈.md`](../技术栈.md) §六），因此 Phase 0 先把「Steamworks 沙盒」环境搭好，供 Phase 1 的「Steam 登录 + 成就 + 云存档」开发调试。

**沙盒 = 开发期测试环境**，两条路径：

| 路径 | 说明 | 适用 |
|------|------|------|
| `steam_appid.txt` 本地沙盒 | exe 同目录放 App ID，免 Steam 客户端登录即可 `SteamAPI.Init()` | 本地快速调试 |
| Steam 客户端沙盒 | 通过 Steam 客户端启动测试包，走真实登录 / 成就 / 云存档 | 完整功能联调 |

---

## 二、注册 Steamworks 账号（Steam Direct）

1. 打开 <https://partner.steamgames.com/>
2. 用现有 Steam 账号登录（无则先注册一个 Steam 账号）
3. 按引导完成 Steamworks 开发者注册
4. 支付 **$100 一次性提交费**（Steam Direct；该 App 在 Steam 销售额达 $1000 后返还）

> 费用说明：$100 是**每个 App** 的提交费，非年费；个人开发者用个人 Steam 账号即可，无需公司资质。

---

## 三、创建 App，获取 App ID

1. Steamworks 后台 → 「创建新产品」→ 类型选「游戏」
2. 填写 App 名称 / 类型 / 发行方式（这些后续都可改）
3. 创建后进入「App Admin」页，记录 **App ID**（一串数字，如 `3251130`），后续沙盒与 SDK 都要用它

---

## 四、下载 Steamworks SDK

1. 打开 <https://partner.steamgames.com/doc/sdk>
2. 下载最新 `steamworks_sdk_<版本>.zip`（含 `public/` C++ 头文件与 `redistributable_bin/` 原生库）
3. 解压到固定位置，如 `D:\Steamworks\`（Unity 集成时要从 `redistributable_bin/win64/steam_api64.dll` 取原生库）

---

## 五、沙盒配置

### 5.1 steam_appid.txt（本地免登录沙盒）

- 在游戏 **exe 同目录**新建 `steam_appid.txt`，内容只写 App ID 数字（无空格、无换行后缀）。
- `SteamAPI.Init()` 会读它，无需 Steam 客户端登录即可初始化，适合本地快速调试。

### 5.2 Steam 客户端沙盒（完整联调）

1. Steamworks 后台 → 「用户与权限」→ 添加测试人员（自己或协作者的 Steam 账号）
2. 上传测试构建，或用 Unity 本地构建的 exe 直接在 Steam 客户端里以「非 Steam 游戏」方式运行
3. 验证登录 / 成就 / 云存档的真实链路

---

## 六、Unity 集成准备（Steamworks.NET）

选型：**Steamworks.NET**（rlabrecque，Unity 事实标准；备选 Facepunch.Steamworks 已停维护）。

1. 从 GitHub `rlabrecque/Steamworks.NET` 或 Unity Asset Store 获取包
2. 导入 Unity 工程 `Assets/Plugins/Steamworks.NET/`
3. 把 SDK `redistributable_bin/win64/steam_api64.dll` 放进插件目录（Windows x64）
4. 场景挂 `SteamManager` 单例，最小验证：

```csharp
if (SteamManager.Initialized)
{
    Debug.Log($"Steam 已初始化，AppID={SteamUtils.GetAppID()}，用户={SteamFriends.GetPersonaName()}");
}
```

> 完整 SDK 接入与「Steam 登录 / 成就 / 云存档」功能开发属 **Phase 1**，本阶段只搭沙盒环境，不做功能实现。

---

## 七、沙盒连通验证（DoD）

- [ ] `steam_appid.txt` 已放 exe 目录，`SteamAPI.Init()` 返回 true
- [ ] 日志读到正确的 App ID
- [ ] （可选）Steam 客户端沙盒登录后读到 Steam ID / 昵称

---

## 八、用户操作待办清单

> 以下步骤需用户亲自操作（Claude 无法代做账号注册与付费）：

- [ ] 注册 Steamworks 开发者账号
- [ ] 支付 $100 提交费，创建 App
- [ ] 记录 App ID
- [ ] 下载 Steamworks SDK
- [ ] 完成 §七 沙盒连通验证

---

## 九、相关文档

- [环境搭建说明.md](环境搭建说明.md) — 本地开发环境（Unity / Python / 模型）
- [技术栈.md](../技术栈.md) — Steam 在线能力与 C# 绑定选型
- [流程路线.md](../流程路线.md) — Phase 1 用户系统（Steam 登录）
