@echo off
setlocal enabledelayedexpansion

:: =================================================================
:: SharpFort.Net 部署示例脚本 (模板)
:: 使用说明：
:: 1. 将此文件更名为 deploy.bat (该名称已被 .gitignore 忽略，不会提交私密信息)
:: 2. 修改下方的配置变量以匹配您的服务器环境
:: 3. 确保本地已安装 7-Zip 或修改压缩命令
:: =================================================================

:: --- 服务器配置 ---
set SERVER_USER=your_user
set SERVER_IP=your_server_ip
set REMOTE_PATH=/home/your_path/build/app.zip
set REMOTE_WORK_DIR=/home/your_path/app-dir
set REMOTE_COMMAND="cd !REMOTE_WORK_DIR! && unzip -o !REMOTE_PATH! -d ./ && ./start.sh"

:: --- 本地配置 ---
set ZIP_FILE=publish_output.zip
set SOURCE_PATH=./src/Yi.Abp.Web/bin/Release/net8.0/linux-x64/publish/*
:: 尝试自动获取 7-Zip 路径，如果找不到请手动指定
set SEVEN_ZIP="C:\Program Files\7-Zip\7z.exe"
if not exist !SEVEN_ZIP! set SEVEN_ZIP="D:\Program Files\7-Zip\7z.exe"

echo [1/3] 开始构建项目...
:: dotnet publish -c Release -r linux-x64 --self-contained false

echo [2/3] 正在压缩发布包...
if exist !SEVEN_ZIP! (
    !SEVEN_ZIP! a !ZIP_FILE! !SOURCE_PATH!
) else (
    echo [错误] 找不到 7-Zip，请在脚本中配置 SEVEN_ZIP 路径。
    pause
    exit /b
)

echo [3/3] 正在上传并部署到服务器...
:: scp !ZIP_FILE! !SERVER_USER!@!SERVER_IP!:!REMOTE_PATH!
:: ssh !SERVER_USER!@!SERVER_IP! !REMOTE_COMMAND!

echo =================================================================
echo 部署流程模拟完成！
echo 注意：为了安全，上传和远程执行命令已默认注释。
echo 请在 deploy.bat 中取消注释并配置正确参数。
echo =================================================================
pause
