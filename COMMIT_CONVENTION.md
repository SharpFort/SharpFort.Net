# Git 提交规范 (Commit Message Convention)

为了规范代码提交日志，增强可读性及版本回溯，本项目采用类 Angular 规范。

## 1. 提交格式
每次提交必须包含一个类型标签，格式如下：
`<type>: <description>`

## 2. 标签类型 (Type)
| 标签 | 说明 | 示例 |
| :--- | :--- | :--- |
| **feat** | 新功能 (feature) | `feat: 增加登录滑块验证码` |
| **fix** | 修复 bug | `fix: 修复 RSA 解密分块溢出问题` |
| **docs** | 文档、注释变更 | `docs: 完善 RSAHelper 类注释` |
| **style** | 代码格式 (不影响逻辑，如空格、缩进) | `style: 优化 StorageFile 变量命名及缩进` |
| **refactor** | 代码重构 (既非新功能也非修复 bug) | `refactor: 重构文件上传逻辑` |
| **perf** | 性能优化 | `perf: 提高字典查询缓存命中率` |
| **test** | 增加或修改测试用例 | `test: 增加 AccountService 单元测试` |
| **chore** | 构建过程或辅助工具的变动 (如依赖库更新) | `chore: 更新 usings.props 引用` |
| **revert** | 代码回退 | `revert: 回退到上次稳定的 WebModule 配置` |
| **build** | 打包、发布相关变更 | `build: 发布 v1.2.0 版本` |

## 3. 强制校验 (可选)
可以通过 Git Hooks 强制执行此规范。详情请参考下方的“如何配置强制校验”。
