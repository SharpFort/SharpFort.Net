# 1. 运行环境阶段 (Base)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
# 设置时区为亚洲/上海
RUN ln -sf /usr/share/zoneinfo/Asia/Shanghai /etc/localtime && \
    echo "Asia/Shanghai" > /etc/timezone
WORKDIR /app
EXPOSE 19001

# 2. 编译构建阶段 (Build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 拷贝所有项目文件以进行 Restore (利用层缓存优化)
COPY ["Yi.Abp.sln", "./"]
COPY ["common.props", "./"]
COPY ["usings.props", "./"]
COPY ["version.props", "./"]

# 自动查找并拷贝所有 .csproj 文件
# 注意：这一步为了性能手动列出核心模块，或保持 COPY . .
# 针对本项目的多层级结构，直接拷贝全部源代码
COPY . .

# 执行还原
RUN dotnet restore "src/Yi.Abp.Web/Yi.Abp.Web.csproj"

# 执行发布
WORKDIR "/src/src/Yi.Abp.Web"
RUN dotnet publish "Yi.Abp.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 3. 最终镜像阶段 (Final)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# 默认启动 Yi.Abp.Web 模块
ENTRYPOINT ["dotnet", "Yi.Abp.Web.dll"]
