# KJ 热更新测试服务器

独立维护的 CDN 静态文件服务器，为 YooAsset Host 模式热更提供资源下发。

> 本仓库与 **KJ 客户端工程**（`damonZero/UnityFramework`）**分开管理**。
> 真正的 CDN 内容由 KJ 构建管线的 `HostUpdatePublisher` 写入 KJ 工程的
> `Server/CDN`，本服务器把那个目录作为 Web 根目录，因此发布补丁后
> **无需复制文件**，刷新即可访问最新版本。

## 快速开始

### 方式 1：双击启动（Windows）

先设置环境变量，再双击 `start_server.bat`：

```powershell
# 一次性设置（当前窗口有效）
set KJ_CDN_ROOT=G:\Mine\NewProjectK\KJ\Server
start_server.bat
```

### 方式 2：命令行（推荐）

```powershell
# 通过环境变量指定 CDN 根目录
set KJ_CDN_ROOT=G:\Mine\NewProjectK\KJ\Server
python server.py

# 或直接用 --root 参数
python server.py --root G:\Mine\NewProjectK\KJ\Server
# 自定义端口:
python server.py --root G:\Mine\NewProjectK\KJ\Server --port 9000
```

> 不设置 `KJ_CDN_ROOT` / `--root` 时，服务器会把**本仓库目录**当作根目录
> （适合仓库自带 CDN 的场景）。KJ 工程与本仓库分离部署时**务必显式指定**。

## 验证是否正常

浏览器打开：

```
http://localhost:8080/CDN/Android/DefaultPackage/DefaultPackage.version
```

应返回内容：`1.0.0`

## 设备访问地址

| 设备 | 地址 |
|------|------|
| Android 模拟器 (MuMu/Genymotion) | `http://10.0.2.2:8080/CDN/Android/DefaultPackage` |
| 局域网真机 | `http://<本机局域网IP>:8080/CDN/Android/DefaultPackage` |
| 宿主机浏览器 | `http://localhost:8080/CDN/Android/DefaultPackage` |

> 真机测试需要把 KJ 构建管线的 `BuildProfile.CdnBaseUrl` 改为局域网 IP
> （如 `http://192.168.1.100:8080/CDN/Android/DefaultPackage`），并确保防火墙放行 8080 端口。

## 完整热更测试流程

1. 启动本服务器（设置 `KJ_CDN_ROOT` 指向 KJ 工程的 `Server` 目录）
2. 安装基线 APK（1.0.0）
3. 在 Unity Dashboard 修改热更代码 → 「发布热更补丁 1.0.1」
4. 重启 APK（不重装）→ 观察日志中版本检查 → 下载 → 启动
5. 确认新代码/资源生效

## 技术说明

- 纯 Python 标准库（`http.server`），**无第三方依赖**
- 自动禁用 HTTP 缓存（`Cache-Control: no-store`），保证 YooAsset 每次拿到最新文件
- 支持多线程（`ThreadingHTTPServer`），可并发下载
- 默认监听 `0.0.0.0`，局域网设备可直接访问
- CDN 根目录解析优先级：`--root` 参数 > 环境变量 `KJ_CDN_ROOT` > 本仓库目录
