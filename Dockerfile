# ==========================================
# 1. 运行环境阶段 (Base)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# 切换到 root 用户以配置文件系统和时区
USER root
# 设置时区为亚洲/上海
RUN ln -sf /usr/share/zoneinfo/Asia/Shanghai /etc/localtime && \
    echo "Asia/Shanghai" > /etc/timezone

# 【最佳实践】：配置完系统环境后，务必降级为非特权用户 app，防止容器逃逸风险
USER app
WORKDIR /app

# 暴露 19001 端口 (你提到 19001 端口未被占用)
EXPOSE 19001
ENV ASPNETCORE_HTTP_PORTS=19001

# ==========================================
# 2. 编译构建阶段 (Build)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 拷贝全局配置文件 (利用层缓存优化)
COPY ["Sf.Abp.sln", "./"]
COPY["common.props", "./"]
COPY ["usings.props", "./"]
COPY ["version.props", "./"]

# 针对本项目的多层级结构，直接拷贝全部源代码
# (虽然会牺牲一部分 Docker 本地层缓存，但我们会在 GitHub Actions 中用外部缓存弥补)
COPY . .

# 执行还原
RUN dotnet restore "src/Sf.Abp.Web/Sf.Abp.Web.csproj"

# 执行编译
WORKDIR "/src/src/Sf.Abp.Web"
RUN dotnet build "Sf.Abp.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ==========================================
# 3. 发布阶段 (Publish)
# 【修复点】：增加独立的 publish 阶段，修复原代码 COPY --from 找不到阶段的 Bug
# ==========================================
FROM build AS publish
RUN dotnet publish "Sf.Abp.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ==========================================
# 4. 最终镜像阶段 (Final)
# ==========================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 默认启动 Sf.Abp.Web 模块
ENTRYPOINT ["dotnet", "Sf.Abp.Web.dll"]