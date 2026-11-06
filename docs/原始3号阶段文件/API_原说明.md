# OpenAHU.Api（3号：本地 API 骨架）

本机回环上的 ASP.NET Core 最小 API，作为 WPF 桌面端的伴随进程：WPF 只通过 HTTP + JSON + DTO 访问，
**后端独占** SQLite（`%LocalAppData%\OpenAHU\campus.db`），客户端永远不碰 `.db` 文件。

> 本目录当前是**骨架**：宿主、会话握手、错误结构、路由表已就位；除 `data-status` 外端点均为占位实现。
> 编译与运行**未验证**（本机无 .NET SDK，见第 7 节）。

## 1. 边界（本骨架刻意不做的事）

| 不做 | 原因 |
| --- | --- |
| 不引用 EF Core / Microsoft.Data.Sqlite | Api 执行 SQL 会破坏 `Api → Application ← Infrastructure` 的方向；数据库异常到错误码的映射必须由 04 号用结果对象表达（见第 6 节） |
| 不定义 `HealthDto` | 冻结文档只写“服务/DB 就绪状态”，**没有给响应体**；擅自加 = 单方面改契约（见第 6 节） |
| 不接 4/5 号的 `AddApplication()` / `AddInfrastructure()` | 这两层的扩展方法尚未交付；先按已冻结路由占位，避免凭空发明接口 |
| 不引 `Microsoft.AspNetCore.OpenApi` | 无 NuGet 还原能力，无法确认版本与仓库 SDK 对齐；等契约 PR 合并后单独加 |
| 不放任何硬编码业务数据 | 占位端点统一返回 `503 db_unavailable`，不返回假 JSON（文档明确不接受 JSON stub 作最终交付） |

## 2. 目录

```
src/OpenAHU.Api/
  OpenAHU.Api.csproj                  Microsoft.NET.Sdk.Web, net10.0, 仅引用 Contracts
  Program.cs                          宿主：回环监听、JSON 约定、异常处理、路由、握手文件
  Hosting/SessionToken.cs             32 字节随机令牌 + 固定时间比较
  Hosting/SessionHandshake.cs         交接文件读写（原子替换、退出清理）
  Hosting/SessionTokenMiddleware.cs   回环来源校验 + 会话头校验 → 401
  Errors/ApiResults.cs                统一错误体构造（code/message/traceId）
  Errors/ApiExceptionHandler.cs       未捕获异常 → 500 internal，细节只进日志
  Endpoints/CampusEndpoints.cs        /api/v1 路由表（1 个真实 + 9 个占位）
  tools/check_route_coverage.py       路由表 ↔ 冻结 API 表比对（可进 CI）
  README.md
```

## 3. 启动方式与握手协议（1 号对接用）

后端自己决定端口，**不硬编码对外监听**：

1. 进程以 `http://127.0.0.1:0` 启动，操作系统分配随机端口。
2. 启动成功后写 `%LocalAppData%\OpenAHU\session.json`（先写 `.tmp` 再原子替换）：

```json
{"port":54321,"token":"k7d...43chars","processId":12345,"startedAt":"2026-11-02T10:00:00+08:00","baseUrl":"http://127.0.0.1:54321"}
```

3. 客户端读取该文件，把 `baseUrl` 作为基地址，每个请求加头：

```
X-OpenAHU-Session: <token>
```

4. 正常退出时文件被删除；崩溃残留由下次启动覆盖，客户端应以 `processId` 判断旧进程是否存活。

> ⚠️ 交接载体（文件 vs stdout 一行 JSON）**尚未与 1 号定稿**。若 1 号要求 stdout，改动只在 `Program.cs` 与本文档。
> ⚠️ 令牌只写入交接文件，**不写日志、不出现在错误响应里**。

## 4. 端点状态

| 方法 | 路径 | 状态 | 说明 |
| --- | --- | --- | --- |
| GET | `/api/v1/data-status` | ✅ 真实 | 常量 `LocalSQLite` / `IsDemo=true`；`UpdatedAt` 暂为 `null`，待 5 号提供“数据最后更新时间” |
| GET | `/api/v1/profile` | 🟡 占位 503 | 待 4 号 UseCase |
| GET | `/api/v1/terms` | 🟡 占位 503 | 同上 |
| GET | `/api/v1/courses` | 🟡 400 / 503 | 缺 `termId` 已返回 400 `invalid_request`；参数合法时仍 503。**未知 termId** 归 404 还是 400 待 4 号定 |
| GET | `/api/v1/grades` | 🟡 400 / 503 | 同上；空成绩保持 `null` |
| GET | `/api/v1/exams` | 🟡 400 / 503 | 同上；未公布时间排末尾 |
| GET | `/api/v1/links` | 🟡 占位 503 | — |
| POST/PUT/DELETE | `/api/v1/local/courses[/{id}]` | 🟡 占位 503 | 第二阶段写链路 |
| GET | `/api/v1/health` | ✅ 200 | `HealthDto`：`status=starting`、`databaseReady=false`（5 号探针接入后改真实值），不回显 DB 路径 |

占位端点返回体：

```json
{"code":"db_unavailable","message":"本地数据服务尚未就绪，请稍后重试。","traceId":"..."}
```

参数校验已实现（不依赖数据层）：

```json
// GET /api/v1/courses            （无 termId）
// 400 invalid_request
{"code":"invalid_request","message":"termId 不能为空，请先选择学期。","traceId":"..."}
```

**占位 503 不是 Gate A 证据**，Gate A 必须看到真实 SQLite 数据。

## 5. 安全

- 只监听 `127.0.0.1`；端口随机，不开放固定端口。
- 中间件同时校验 **来源是回环地址** 与 **会话令牌**（`CryptographicOperations.FixedTimeEquals`，防计时侧信道）。
- 不启用任意 Origin 的 CORS——浏览器请求没有令牌依然会被拒绝。
- 错误响应不含数据库路径、SQL、连接串、令牌；细节仅进服务端日志。
- 交接文件放在当前用户 `LocalAppData` 下（用户隔离），清理在 `finally` 中执行。

## 6. 待评审 / 待他人交付（阻塞项）

| 事项 | 阻塞谁 | 需要的结论 |
| --- | --- | --- |
| `HealthDto` 形状 | 组长（评审）、5 号（探针） | **已实现并登记为待评审**（`Contracts/Dtos/HealthDto.cs`）；`/health` 现返回 `starting`/`DatabaseReady=false`，5 号给出就绪探针后改为真实值 |
| 401 的错误码 | 3 号 | 冻结错误码表只有 400/404/409/500/503；当前在 `ApiResults.UnauthorizedCode` 集中写死 `"unauthorized"`，评审后迁入 `ApiErrorCodes` |
| DB 不可用 → 503 的判定位置 | 3、4、5 号 | Api 不引 EF，必须由 4 号用结果对象/领域异常表达“数据库不可用”，3 号只做映射 |
| `AddApplication()` / `AddInfrastructure()` 注册签名 | 3、4、5 号 | 交给我时只需给出 `IServiceCollection` 扩展方法名与所需配置键 |
| 交接载体（文件 / stdout） | 1 号 | 影响 `Program.cs` 与第 3 节 |
| 数据库路径配置键 | 5 号 | 建议 `OpenAHU:DatabasePath`，默认 `%LocalAppData%\OpenAHU\campus.db`；当前代码尚未读取 |

## 7. 验证状态（如实记录）

**已执行**

```text
python src/OpenAHU.Contracts/tools/check_contract_signatures.py
  → 8 个冻结类型一致 + 2 个待评审模型，退出码 0

python src/OpenAHU.Api/tools/check_route_coverage.py
  → 冻结 API 11 条，源码注册 11 条，路由表与冻结表完全一致，退出码 0
```

**未执行（不声称通过）**

- `dotnet restore / build / run`：本机 `dotnet --version` 报 no .NET SDKs were found。
- 真实 HTTP 调用、401 场景、交接文件读写、进程退出清理：均未跑过，需在装有 .NET 10 SDK 的 Windows 上验证。
- OpenAPI 文档：未生成。

**有 SDK 的机器上请按序验证**

```powershell
dotnet build src/OpenAHU.Api/OpenAHU.Api.csproj -c Release
dotnet run --project src/OpenAHU.Api            # 观察日志中的 BaseUrl
$s = Get-Content "$env:LOCALAPPDATA\OpenAHU\session.json" | ConvertFrom-Json
Invoke-RestMethod "$($s.baseUrl)/api/v1/health" -Headers @{ "X-OpenAHU-Session" = $s.token }      # 期望 200 starting/false
Invoke-RestMethod "$($s.baseUrl)/api/v1/data-status" -Headers @{ "X-OpenAHU-Session" = $s.token }
Invoke-RestMethod "$($s.baseUrl)/api/v1/data-status"                      # 期望 401 unauthorized
Invoke-RestMethod "$($s.baseUrl)/api/v1/courses" -Headers @{ "X-OpenAHU-Session" = $s.token }   # 期望 400 invalid_request
Invoke-RestMethod "$($s.baseUrl)/api/v1/courses?termId=term-2026-fall" -Headers @{ "X-OpenAHU-Session" = $s.token }  # 期望 503
```

## 8. 下一个 PR 顺序

1. 组长评审 `HealthDto` 形状；通过后写入冻结清单并处理 2 号/6 号的绑定与断言。
2. 4 号交付 Application 接口 → 3 号把 6 个占位端点替换为真实查询，503 改为 404/409。
3. 5 号给就绪探针 → `/health` 的 `DatabaseReady` 改为真实值；接 `AddInfrastructure()` 后 `data-status.UpdatedAt` 改为库内真实时间。
4. 6 号把两个 check 脚本接入 Windows CI。
