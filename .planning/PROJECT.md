# KJ Unity Framework

## 技术栈

- 引擎: Unity 2022.3.62f2 LTS
- 热更新: HybridCLR
- 配置表: Luban v4.10.1
- 网络通信: Google.Protobuf 3.35.1
- 异步: UniTask v2.5.11
- UI: UGUI (内置)

## 架构设计

四层分层架构（严格向下依赖）：
Boot (入口层) → Core (核心框架) → General (通用系统) → Project (业务逻辑)

每层对应一个 .asmdef 文件，实现编译隔离。
