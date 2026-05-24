# Code-Gen 模块：保留、增强还是移除？—— 独立分析

## 1. 观点的正确之处

**一致性论点完全成立。** 对于 SharpFort 这种严格的 DDD 分层架构（Domain.Shared → Domain → Application.Contracts → Application → SqlSugarCore），一致性不仅仅关乎代码风格——它关乎**架构约束**：

- 每个 CRUD 服务必须继承 `SfCrudAppService<TEntity, TDto, TKey, TGetListInput>`
- 每个实体必须使用 `[SugarTable]` / `[SugarColumn]` 特性
- DTO 的命名必须遵循 `GetListInput` / `GetListOutputDto` / `CreateInput` / `UpdateInput` 约定
- 每层的依赖注入声明必须一致

AI + Skill 本质上是**概率性**的——今天生成的代码可能与昨天不同，模型更新后行为可能漂移。而 code-gen 是**确定性编译**——相同的 schema 输入永远产生相同的代码输出。对于 20 个 CRUD 模块，这种确定性不是奢侈品，是必需品。

## 2. 需要补充的论点

**一致性并非 code-gen 的唯一价值，甚至不是最重要的价值。** 真正不可替代的是：

### a) 元数据双向同步（Code ↔ DB 元数据）

`WebTemplateManager.BuildCodeToWebAsync()` 通过反射扫描所有 `[SugarTable]` 实体，将类名、属性类型、特性等元数据同步到 `gen_table` / `gen_field` 数据库表。这不是代码生成，而是**架构可视化**——它让数据库中的表结构信息成为可以被查询、管理和驱动后续流程的结构化数据。AI 无法做到这一点，因为它没有反射运行时程序集的能力。

### b) 模板作为架构知识的持久化载体

`gen_template` 表中的每一条模板记录，本质上是对 "SharpFort 项目中如何正确创建一个 CRUD 模块" 这个问题的编码。模板是**架构决策的可执行文档**，不依赖开发者的记忆或 AI 的上下文窗口。当项目架构演进（如基类从 `ApplicationService` 变为 `SfCrudAppService`），修改一处模板即可修复所有未来的生成。

### c) 批量操作的经济性

生成 50 个 CRUD 模块，code-gen 需要 2 秒。AI 需要 50 次交互，每次都可能需要修正。这不是 AI 能力问题——是**单位成本**问题。

## 3. AI + Skill 的真正优势（以及为什么它不能替代 code-gen）

| 维度 | Code-Gen | AI + Skill |
|------|----------|------------|
| 确定性 | 100% | ~85-95% |
| 处理边缘情况 | 差（模板无法表达条件逻辑） | 强（可以理解和处理异常） |
| 架构约束 | 编译时强制 | 依赖 prompt 描述 |
| 批量生成 | 极快 | 逐次交互 |
| 复杂业务逻辑 | 不能生成 | 可以生成 |
| 上下文理解 | 无 | 有（可以理解需求意图） |

关键洞察：**它们解决不同的问题，且问题边界清晰**——

- **Code-Gen** 解决的是 **"synthesis"（合成）** 问题：从结构化数据（表 schema）到骨架代码的确定性转换。输入是结构化元数据，输出是遵循固定模式的 scaffolding。
- **AI + Skill** 解决的是 **"authorship"（创作）** 问题：从非结构化需求到业务逻辑的创造性转换。输入是自然语言意图，输出是需要判断和权衡的实现。

这两者不是竞争关系，而是**上下游关系**。

## 4. 当前 code-gen 的不足

code-gen 模块目前的实现是概念验证级别：

- **模板引擎过于简陋**——只有 3 个字符串替换 handler，没有条件、循环、表达式
- **种子数据被注释掉**——`TemplateDataSeed.cs` 的全部内容被注释，意味着开箱不可用
- **两个核心方法未实现**——`PostWebBuildDbAsync` 和 `PostCodeBuildDbAsync` 是空白桩
- **不支持增量生成**——每次生成都会覆盖文件，无法保留手动添加的业务代码
- **没有与 Skill 集成**——它是孤立的，没有暴露给 AI 辅助工作流

这些不足不是放弃的理由，而是增强的方向。

## 5. 独立结论

**应该保留并显著增强 code-gen 模块。**

理由不是 "AI 还不够好"，而是 **code-gen 和 AI + Skill 解决根本不同的问题，且它们的组合产生 1+1 > 2 的效果**：

```
┌─────────────────────────────────────────────────┐
│              理想的开发工作流                      │
│                                                   │
│  1. 定义实体类 (手写或用 code-gen DB-First)        │
│       ↓                                          │
│  2. Code-Gen 同步到 gen_table/gen_field           │
│       ↓                                          │
│  3. Code-Gen 生成 CRUD 骨架代码                    │
│       ↓                                          │
│  4. AI + Skill 在此基础上添加:                     │
│     - 复杂查询逻辑                                │
│     - 业务规则验证                                │
│     - 跨模块集成                                  │
│     - Casbin 权限策略                             │
│     - 单元测试                                    │
│       ↓                                          │
│  5. Code-Gen 重新生成 → 仅覆盖骨架部分              │
│     (通过 partial class 或 protected region)      │
└─────────────────────────────────────────────────┘
```

### 增强方向建议（按优先级）

1. **P0：升级模板引擎** —— 从字符串替换升级到 Scriban（README 已规划），支持条件/循环/类型推断。Scriban 是纯 C# 实现，无额外依赖，与 SharpFort 的 "轻量化" 哲学一致。

2. **P0：实现增量生成** —— 使用 C# `partial class` 或 Scriban 的 protected region，确保重新生成不会覆盖手动编写的业务代码。

3. **P1：完善 DB-First 工作流** —— 实现 `PostWebBuildDbAsync` 和 `PostCodeBuildDbAsync`，让模块可以从现有数据库表逆向生成实体。

4. **P1：创建 Code-Gen Skill** —— 编写一个 Skill，让 AI 能够：
   - 理解项目中 code-gen 的能力边界
   - 调用 code-gen API 生成骨架
   - 然后在生成的基础上添加业务逻辑
   - 知道哪些文件是 "生成区域"（不应手动修改），哪些是 "扩展区域"

5. **P2：模板文件化** —— 考虑将模板从数据库迁移到文件系统（如 `.scriban` 文件），使其可被 git 版本控制和 CI 检查。

## 6. 风险提示

- **不要过度工程化** —— code-gen 的价值在于解决 SharpFort 项目自身的 scaffolding 需求，不要试图做一个通用的代码生成框架
- **保持轻量精神** —— 即使引入 Scriban，核心代码不应超过 1500 行
- **Skill 是放大器，不是替代品** —— 增强 code-gen 的方向应该是让它更好地与 Skill 协作，而不是让它变得更像 AI

---

**一句话总结**：保留并增强 code-gen。code-gen 提供确定性骨架，AI+Skill 提供创造性血肉，两者结合才是完整答案。当前模块需要增强（尤其是模板引擎和增量生成），但其架构设计（双向同步、管道 handler、DB 驱动模板）方向是正确的。
