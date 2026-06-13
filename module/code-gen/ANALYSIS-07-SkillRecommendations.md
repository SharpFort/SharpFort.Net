# 🧠 Skill 推荐分析报告

> **分析日期**: 2026-06-12  
> **目标**: 分析代码生成模块的改写/扩展工作中，哪些 Hermes Skill 可用

---

## 一、技能需求矩阵

根据改写计划，任务可分为以下类别：

| 任务类别 | 涉及范围 |
|----------|----------|
| 🔍 代码分析 | 理解现有代码、搜索模式、追踪依赖链 |
| ✏️ C# 代码编写 | 实体、服务、管理器、处理器的新增/改写 |
| 🧪 测试 | 单元测试、集成测试 |
| 📋 文档 | 技术文档、API 文档 |
| 🏗️ 架构图 | 流程图、架构图、ER 图 |
| 🎨 前端模板 | .vue / .ts 模板编写 |

---

## 二、可用的本地 Built-in Skills（无需网络）

以下技能已预装在本地 Hermes Agent 中，可以直接使用：

### 2.1 软件开发类

| Skill | 适用于 | 价值 |
|-------|--------|------|
| **`codebase-inspection`** | 分析代码量、语言分布 | ⭐⭐⭐⭐⭐ 已用于本分析 |
| **`writing-plans`** | 编写实现计划 | ⭐⭐⭐⭐⭐ 改写前必备 |
| **`plan`** | 创建结构化计划文件 | ⭐⭐⭐⭐ 复杂功能规划 |
| **`test-driven-development`** | TDD 开发流程 | ⭐⭐⭐⭐ 新增功能必须 |
| **`spike`** | 快速原型验证 | ⭐⭐⭐ 技术预研 |
| **`systematic-debugging`** | 4阶段根因调试 | ⭐⭐⭐ 问题排查 |
| **`subagent-driven-development`** | 多子代理分派任务 | ⭐⭐⭐⭐ 并行开发 |
| **`requesting-code-review`** | 提交前安全检查 | ⭐⭐⭐ 代码质量 |

### 2.2 GitHub 工作流类

| Skill | 适用于 | 价值 |
|-------|--------|------|
| **`github-pr-workflow`** | PR 生命周期管理 | ⭐⭐⭐ 团队协作 |
| **`github-code-review`** | PR diff 审查 | ⭐⭐⭐ Code Review |
| **`github-issues`** | Issue 管理 | ⭐⭐ 任务跟踪 |
| **`github-repo-management`** | 仓库/分支管理 | ⭐⭐ 版本管理 |

### 2.3 架构与文档

| Skill | 适用于 | 价值 |
|-------|--------|------|
| **`architecture-diagram`** | SVG 架构图 | ⭐⭐⭐⭐ 架构文档 |
| **`excalidraw`** | 手绘风格流程图 | ⭐⭐⭐ 设计讨论 |
| **`obsidian`** | Obsidian 笔记管理 | ⭐⭐ 知识沉淀 |

---

## 三、需要网络的 Skills

以下技能涉及网络访问，分析当前项目**不需要**：

| Skill | 原因 |
|-------|------|
| `huggingface-hub` | 与机器学习模型相关，不适用 |
| `llama-cpp` | 本地模型推理，不适用 |
| `polymarket` | 预测市场查询，完全无关 |
| `arxiv` | 学术论文搜索，暂不需要 |
| `spotify` | 音乐控制，无关 |
| 其他 MLOps 技能 | 训练/部署 ML 模型，不适用 |

---

## 四、强烈推荐的 Skill 组合

### 改写工作流建议

```
阶段 1: 理解代码 ──────► codebase-inspection (已用)
阶段 2: 制定计划 ──────► writing-plans + plan
                           ↓
阶段 3: 绘制架构图 ────► architecture-diagram
                           ↓
阶段 4: 分派实现 ──────► subagent-driven-development
                           ↓
阶段 5: TDD 开发 ──────► test-driven-development
                           ↓
阶段 6: 代码审查 ──────► requesting-code-review + github-code-review
                           ↓
阶段 7: PR 提交 ───────► github-pr-workflow
```

### 具体执行策略

对于较大的功能（如"增加 Vue 前端代码生成"），建议采用：

1. **`plan`** → 生成详细的 markdown 实施计划
2. **`subagent-driven-development`** → 将计划拆分为子任务，并行分派
3. **`test-driven-development`** → 每个子任务遵循 RED-GREEN-REFACTOR

---

## 五、当前环境限制

| 限制 | 影响 |
|------|------|
| `python3=missing` | 无法使用 pygount 做代码统计（但已通过直接阅读完成） |
| `pip=missing` | 无法安装 Python 工具 |
| Windows 环境 | 某些 Linux 特有工具不可用 |
| Shell 为 git-bash/MSYS | 使用 POSIX 语法，非 PowerShell |

---

## 六、总结

| 类别 | 数量 | 说明 |
|------|------|------|
| 当前分析直接可用 | ~8 个 | codebase-inspection, writing-plans, plan 等 |
| 改写开发阶段可用 | ~5 个 | subagent-driven-development, TDD, architecture-diagram 等 |
| 不需要（无关） | ~50+ | ML、音乐、邮件、智能家居等 |
| 需要但不可用 | 0 | 全部分析任务已通过直接文件阅读完成 |

**结论**: 现有的 Hermes 本地 Skills 足以支撑完整的改写开发流程。核心工作流 Skill（writing-plans → subagent-driven-development → test-driven-development）都是本地可用的，无需网络资源。
