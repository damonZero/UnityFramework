@echo off
rem ============================================================
rem  KJ CDN 服务器 — 一键启动 (Windows)
rem  用法: 双击 或 命令行运行 start_server.bat
rem
rem  CDN 根目录解析优先级:
rem   1. 命令行 --root 参数（如有）
rem   2. 环境变量 KJ_CDN_ROOT
rem   3. 本仓库目录
rem
rem  本机开发环境推荐先设置:
rem    set KJ_CDN_ROOT=G:\Mine\NewProjectK\KJ\Server
rem ============================================================

cd /d %~dp0

rem 让日志窗口标题一目了然
title KJ CDN Server

if "%KJ_CDN_ROOT%"=="" (
    echo.
    echo  [KJ] 未设置 KJ_CDN_ROOT 环境变量，将使用仓库目录作为根目录。
    echo  [KJ] 若指向错误，请设置: set KJ_CDN_ROOT=G:\Mine\NewProjectK\KJ\Server
    echo.
)

echo.
echo  [KJ] 正在启动 CDN 服务器...
echo  [KJ] 请等待出现 "KJ CDN 服务器已启动" 后使用
echo.

python server.py %*

echo.
echo  [KJ] 服务器已退出 (按任意键关闭窗口)
pause
