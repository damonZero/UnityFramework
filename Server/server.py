#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
KJ CDN 静态文件服务器
=====================
用 Python 标准库实现的轻量 CDN 服务器，为 YooAsset Host 模式提供热更资源。

CDN 内容由构建管线 (HostUpdatePublisher / P10_PublishCdnStage) 直接写入
本仓库 Server/Res/CDN 目录。本服务器把 Server/Res 作为 Web 根目录，
所以发布补丁后无需复制文件，刷新即可访问最新版本。

目录结构：
    KJ/            <- 仓库根
    ├── Client/    <- Unity 工程
    └── Server/    <- 服务器代码
        ├── Res/   <- CDN 内容（构建管线发布到这里，即 Web 根）
        └── server.py

用法：
    python server.py [--port 8080] [--host 0.0.0.0] [--root <路径>]
    或设置环境变量 KJ_CDN_ROOT=<路径> 后直接运行

默认 Web 根：Server/Res（本文件所在目录的 Res 子目录）。

访问地址（取决于设备）：
    模拟器 (MuMu/Genymotion)  : http://10.0.2.2:8080/CDN/Android/DefaultPackage
    局域网真机                : http://<本机IP>:8080/CDN/Android/DefaultPackage
    宿主机浏览器              : http://localhost:8080/CDN/Android/DefaultPackage
"""

import argparse
import logging
import os
import sys
from datetime import datetime
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

# 候选默认根目录：
# 1. 环境变量 KJ_CDN_ROOT（推荐，独立仓库场景）
# 2. 本文件所在目录的 Res 子目录（Server/Res，CDN 内容所在）
_ENV_ROOT = os.environ.get("KJ_CDN_ROOT", "")
_REPO_RES_ROOT = str(Path(__file__).resolve().parent / "Res")
DEFAULT_ROOT = _ENV_ROOT if _ENV_ROOT else _REPO_RES_ROOT


class CdnHandler(SimpleHTTPRequestHandler):
    """带热更日志的静态文件 handler。"""

    # 热更测试场景必须禁用缓存，确保 YooAsset 每次拿到最新 manifest/文件
    def end_headers(self):
        self.send_header("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0")
        self.send_header("Pragma", "no-cache")
        self.send_header("Expires", "0")
        super().end_headers()

    def log_message(self, format, *args):
        ts = datetime.now().strftime("%H:%M:%S")
        logging.info("%s  %s  %s", ts, self.client_address[0], format % args)


def parse_args():
    parser = argparse.ArgumentParser(description="KJ CDN 静态文件服务器")
    parser.add_argument("--host", default="0.0.0.0", help="监听地址，默认 0.0.0.0")
    parser.add_argument("--port", type=int, default=8080, help="监听端口，默认 8080")
    parser.add_argument("--root", default=DEFAULT_ROOT,
                        help=f"CDN 根目录（默认: 环境变量 KJ_CDN_ROOT 或本文件所在目录 {DEFAULT_ROOT}）")
    return parser.parse_args()


def main():
    args = parse_args()

    root = os.path.abspath(args.root)
    if not os.path.isdir(root):
        print(f"[错误] CDN 根目录不存在: {root}")
        print("  请通过 --root 或环境变量 KJ_CDN_ROOT 指定 Server/Res 目录。")
        print(f"  例如: KJ_CDN_ROOT={_REPO_RES_ROOT} python server.py")
        sys.exit(1)

    logging.basicConfig(
        level=logging.INFO,
        format="%(message)s",
        stream=sys.stdout,
    )

    os.chdir(root)
    handler = lambda *a, **kw: CdnHandler(*a, directory=root, **kw)
    httpd = ThreadingHTTPServer((args.host, args.port), handler)

    print("=" * 60)
    print(" KJ CDN 服务器已启动")
    print(f"  Web 根目录 : {root}")
    print(f"  监听地址   : {args.host}:{args.port}")
    print("-" * 60)
    print(" 模拟器访问  : http://10.0.2.2:8080/CDN/Android/DefaultPackage")
    print(" 浏览器验证  : http://localhost:8080/CDN/Android/DefaultPackage/DefaultPackage.version")
    print(" 按 Ctrl+C 停止")
    print("=" * 60)

    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\n服务器已停止")
    finally:
        httpd.server_close()


if __name__ == "__main__":
    main()
