# KJ Unity Framework

## 技术栈

- 引擎: Unity 2022.3.62f2 LTS
- 依赖注入: VContainer
- 热更新: HybridCLR
- 配置表: Luban v4.10.1
- 网络通信: Google.Protobuf 3.35.1
- 异步: UniTask v2.5.11
- UI: UGUI (内置)

## 架构设计

四层分层架构（严格向下依赖）：
Boot (入口层) → Core (核心框架) → General (通用系统) → Project (业务逻辑)

Boot 层必须保持最小依赖，只承担启动入口、场景装配、热更新桥接和必要的生命周期过渡，不直接持有完整业务容器图。依赖注入主体通过 VContainer 在 Core / General / Project 层逐步组织与注册。

每层对应一个 .asmdef 文件，实现编译隔离。

## 命名规范

- **Core 层**：`System`（如 AudioSystem、EventSystem、ResourceSystem、UISystem）
  - 接口：`ISystem` / `ITickableSystem` / `IAsyncSystem`
  - 管理器：`SystemManager`
- **业务层（General/Project）**：`Model`（MVVM 规范，如 TaskModel、ShopModel）
  - 业务功能按需实现接口来区分职责（如 ILoginHandler 处理登录数据）
  - 不使用 Module 一词
