# 🐳 SharpFort.Net Docker 构建说明

本项目（SharpFort.Net）目前仅包含后端服务。以下是使用 Docker 进行构建与部署的详细说明。

---

## 🏗️ 后端构建 (SharpFort.Net)

### 1. 完整镜像编译
在项目根目录下执行以下命令。该过程采用多阶段构建，会自动完成代码还原、编译与发布，确保环境一致性。

```bash
# 构建镜像
docker build -t sharpfort/api:latest -f Dockerfile .
```

### 2. 运行容器

#### 🚀 基础启动
直接运行镜像，使用默认配置。

```bash
docker run -d \
  --name sharpfort-api \
  -p 19001:19001 \
  sharpfort/api:latest
```

#### ⚙️ 挂载自定义配置启动
如果您需要修改数据库连接字符串或其他配置，可以将宿主机的 `appsettings.json` 挂载到容器内。

```bash
docker run -d \
  --name sharpfort-api \
  -p 19001:19001 \
  -v /path/to/your/appsettings.json:/app/appsettings.json \
  sharpfort/api:latest
```

---

## 📝 注意事项

1. **时区设置**：镜像内部已默认将时区设置为 `Asia/Shanghai`。
2. **暴露端口**：后端服务默认监听 **19001** 端口。
3. **配置文件优先级**：容器启动后会优先读取 `/app/appsettings.json`。如果您挂载了该文件，请确保其内部配置（如 Redis 地址、数据库连接）在容器网络环境下是可访问的。
4. **.dockerignore**：项目已配置 `.dockerignore` 文件，构建时会自动忽略 `bin/`, `obj/`, `.git` 等无关目录，请勿删除该文件。

---

> **提示**：前端项目（sharpfort-net-vue）请参考对应的独立仓库文档。
