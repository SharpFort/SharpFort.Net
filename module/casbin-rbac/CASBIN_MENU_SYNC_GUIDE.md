# Casbin 权限与菜单同步机制排查指南

## 1. 问题现象与背景
在系统开发中，经常会遇到以下困惑：
**现象**：在前端“菜单管理”中新增了某个模块（如 `file-management`）的菜单数据，发现数据只存入了 `casbin_sys_menu` 表，并没有同步写入 `casbin_rule` 表。但对比其他模块（如 `fluid-sequence`），却发现前端操作后，两个表都有对应的数据，权限能正常生效。

## 2. 根本原因分析 (Root Cause)

基于 SharpFort 系统的 Casbin-RBAC 设计原理，`casbin_rule`（Casbin策略表）和 `casbin_sys_menu`（菜单表）的定位和职责是完全不同的。

1. **数据的含义不同**：
   - **`casbin_sys_menu` 表**：存储的是系统的**菜单资源实体**，如它的路由 (`Router`)、前端组件 (`Component`) 以及后端的 API 路径 (`ApiUrl`)。
   - **`casbin_rule` 表**：存储的是**策略 (Policy)**，明确表示“**角色 (Role)** 在哪个**域 (Domain)** 下对哪个**资源 (API)** 拥有什么**操作权限 (Action)**”。在 RBAC 体系下，它的 `p` 规则数据结构通常为 `p, v0(RoleCode), v1(Domain), v2(ApiUrl), v3(ApiMethod)`。

2. **无法自动同步的原因**：
   在前端“菜单管理”中**仅仅创建菜单此时无法也不应写入 `casbin_rule`**。因为此时该菜单仅仅是一个“资源”，系统并不知道应该把这个资源的权限赋予给哪个“角色”。没有角色信息（缺少 `v0`），自然无法生成 Casbin 策略。

3. **出现差异的具体排查点（Why `fluid-sequence` works but `file` doesn't?）**：
   `fluid-sequence` 模块表现正常，而 `file-management` 模块表现异常，一定是以下几个环境因素或操作步骤之一存在差异：

   - **差异一：漏掉了“分配角色”的操作**
     `fluid-sequence` 模块在新建菜单后，开发人员可能顺手去“角色管理”中给 Admin 或相关角色分配了这些新菜单。
     *源码逻辑*：`RoleManager.GiveRoleSetMenuAsync` 会在分配菜单后调用 `_casbinPolicyManager.SetRolePermissionsAsync`，此时才会将带有 `ApiUrl` 的菜单同步到 `casbin_rule` 中。
     *导致问题*：若 `file-management` 仅建了菜单，没在角色管理里打勾保存，就不会写 Casbin 规则。

   - **差异二：菜单的 `ApiUrl` 为空**
     *源码逻辑*：`CasbinPolicyManager.cs` 在生成权限时有如下拦截校验：
     `if (string.IsNullOrWhiteSpace(menu.ApiUrl)) continue;`
     *导致问题*：如果 `file-management` 在前端配置菜单时，**只填写了“路由地址/组件路径”，而没有填写“API路径”**（或填错了格式），Casbin 引擎会自动忽略它（因为前端菜单无需 Casbin 保护，只有后端 API 接口需要通过 Casbin 鉴权）。而 `fluid-sequence` 肯定正确配置了对应的 `ApiUrl`（如 `/api/app/sequence-rule`）。

   - **差异三：修改了 `ApiUrl` 但没有关联的角色**
     *源码逻辑*：虽然 `MenuService.UpdateAsync` 中有 API 变更自动刷新 Casbin 的逻辑，但前提是该菜单已经被某些角色绑定 (`roleIds.Any()` 为 true)。如果没有绑定角色，修改 API 也不会触发 Casbin 规则插入。

---

## 3. 标准操作解决方案

针对新开发模块（以及以后遇到类似鉴权 403 / 菜单未同步的问题），请遵循以下标准 SOP 流程：

### 第一步：正确配置菜单数据
在“菜单管理”中新增“菜单”或“按钮”权限时，**必须确保后端接口需要控制的菜单项，其 `API 路径 (ApiUrl)` 和 `请求方法 (ApiMethod)` 已正确填写**。
- `ApiUrl` 示例: `/api/app/file` 或 `/api/app/file/*`
- `ApiMethod` 示例: `GET`、`POST` 或 `*`

*(注意：如果是纯粹为了前端页面展示的目录或菜单，不需要控制后端 API 时，`ApiUrl` 留空也是允许并且正常的，Casbin 理论上本就不应保护没有具体 API 映射的伪静态路由。)*

### 第二步：前往角色分配权限 (核心环节)
创建完菜单资源后，菜单只是“存在”了。你**必须**前往【角色管理】页面：
1. 选中需要该模块权限的角色（通常是系统管理员或特定角色）。
2. 点击“分配菜单/权限”或类似操作，在权限树中勾选刚才新增的菜单并**保存**。
3. *保存操作将触发后端的 `RoleManager` 同步策略至 `casbin_rule` 表中并重新加载内存 Enforcer。*

### 第三步：验证与排错
1. **查表验证**：执行上述操作后，查看 `casbin_rule` 表，检索 `v2` 列是否出现了你刚配置的 `ApiUrl`。
2. **鉴权验证**：前端重新登录使其获取最新路由，并点击对应模块观察后端控制台是否触发 403 拦截，或者 Casbin 验权日志。
3. **特例排查**：如果确认步骤一、二都做了仍然没有同步，请使用调试模式在 `CasbinPolicyManager.SetRolePermissionsAsync` 下断点，查看 `ApiUrl` 取值是否存在空格、大小写等被过滤的情况。

## 4. 总结与复用
将来任何新开发模块出现“菜单存在但功能提示无权或未写入防线”的问题，直接核对：
✅ 1. 对应菜单的 `ApiUrl` 是否准确配置（不能只有 `Router` 和 `Component`）。
✅ 2. 该菜单是否已经在具体的 `Role` 下被勾选并成功下发（查 `casbin_sys_role_menu` 是否有关系）。
✅ 3. 在做了 `1` 和 `2` 的前提下，Casbin 必然会忠实地将数据映射到 `casbin_rule` 中。
