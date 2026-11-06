# OpenAHU.Contracts（3号冻结契约）

本目录是 **DTO 与错误结构的唯一来源**。Desktop / Api / tests 只能引用这里，禁止各自重复定义 `CourseDto` 一类的类型。归属：3 号（`feat/api` 分支）；任何字段变更属于跨组契约变更。

## 1. 边界（本类库不做什么）

- 不引用 EF Core / Microsoft.Data.Sqlite / ASP.NET Core / WPF：Contracts 只依赖 BCL。
- 不含业务校验、查询、映射：字段合法性由 04 Application 判定，HTTP 状态码与错误体映射由 03 Api 负责，SQL 只存在于 05 Infrastructure。
- 不含任何学校站点地址、CAS、账号密码字段；本版数据来源固定为本地 SQLite 演示库。

## 2. 文件清单

| 文件 | 类型 | 说明 |
| --- | --- | --- |
| `Dtos/StudentProfileDto.cs` | record | 本机演示资料 |
| `Dtos/TermDto.cs` | record | 学期与总周数 |
| `Dtos/CourseDto.cs` | record | 课程 + 星期/节次 + 开课周次数组 |
| `Dtos/GradeDto.cs` | record | 分数/绩点允许 `null`（未公布 ≠ 0） |
| `Dtos/ExamDto.cs` | record | 考试时间地点可全为 `null` |
| `Dtos/LinkDto.cs` | record | 本地常用入口 |
| `Dtos/DataStatusDto.cs` | record | 恒 `LocalSQLite` / `IsDemo=true` |
| `Dtos/HealthDto.cs` | record | 就绪状态（**待评审**） |
| `Local/CourseUpsertRequest.cs` | record | 本地课程写请求体（**待评审**） |
| `Errors/ApiError.cs` | record | 统一错误体 `{code,message,traceId}` |
| `Errors/ApiErrorCodes.cs` | static class | 冻结错误码常量 |
| `Json/ContractJson.cs` | static class | 两侧共用的 `JsonSerializerOptions` 工厂 |
| `tools/check_contract_signatures.py` | 脚本 | 文本级契约漂移检查（CI 可调） |

## 3. 冻结签名（与 `02_统一规范/00_所有AI必须遵守_统一上下文.md` 逐字一致）

| 类型 | 签名 |
| --- | --- |
| `StudentProfileDto` | `string Id, string DisplayName, string? College, string? Major, int? GradeYear, bool IsDemo` |
| `TermDto` | `string Id, string Name, DateOnly StartDate, int WeekCount, bool IsCurrent` |
| `CourseDto` | `string Id, string Name, string Teacher, string Location, DayOfWeek DayOfWeek, int StartSection, int EndSection, IReadOnlyList<int> WeekNumbers, string TermId` |
| `GradeDto` | `string CourseId, string CourseName, decimal Credits, decimal? Score, decimal? GradePoint, string TermId` |
| `ExamDto` | `string Id, string CourseName, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, string? Location, string TermId` |
| `LinkDto` | `string Id, string Title, string Url, string Category` |
| `DataStatusDto` | `string Provider, bool IsDemo, DateTimeOffset? UpdatedAt, string Message` |
| `ApiError` | `string Code, string Message, string TraceId` |

全部为 `sealed record`（位置参数即属性顺序）。`CourseDto.DayOfWeek` 在源码中写作 `System.DayOfWeek`，仅用于避免属性名与类型名同名时的解析歧义，语义不变。

### 3.1 待评审模型（已实现，未进冻结文档）

冻结文档没有定义这两个模型，但缺少它们 3 号无法交付“全部公共请求/响应模型”。已按最小形状实现并登记在
`tools/check_contract_signatures.py` 的 `PENDING_REVIEW` 集合中；评审通过后写入冻结清单、从该集合删除。

| 类型 | 签名 | 用途 |
| --- | --- | --- |
| `HealthDto` | `string Status, bool DatabaseReady, string Version` | `GET /api/v1/health` 响应体 |
| `CourseUpsertRequest` | `string Name, string Teacher, string Location, DayOfWeek DayOfWeek, int StartSection, int EndSection, IReadOnlyList<int> WeekNumbers, string TermId` | `POST/PUT /api/v1/local/courses[/{id}]` 请求体（不含 Id） |

## 4. JSON 形状（下发约定，1/2 号按此绑定 ViewModel）

- 命名：camelCase（`JsonSerializerDefaults.Web`），读取时大小写不敏感。
- 日期：`DateOnly` → `"2026-09-07"`；`DateTimeOffset` → `"2026-12-21T09:00:00+08:00"`。
- 枚举：`DayOfWeek` **默认输出数字 0–6**。
- 数值：`decimal` 输出数字；可空字段输出 `null`，**不省略键**。
- 列表接口返回裸数组，不额外包 `{data:...}`。

```jsonc
// GET /api/v1/profile
{"id":"demo-001","displayName":"演示学生甲","college":"示例学院","major":"示例专业","gradeYear":2024,"isDemo":true}

// GET /api/v1/terms
[{"id":"term-2026-fall","name":"2026年秋季学期（演示）","startDate":"2026-09-07","weekCount":18,"isCurrent":true}]

// GET /api/v1/courses?termId=term-2026-fall
[{"id":"course-001","name":"软件工程实践","teacher":"示例教师甲","location":"演示楼A101",
  "dayOfWeek":1,"startSection":1,"endSection":2,"weekNumbers":[1,2,3],"termId":"term-2026-fall"}]

// GET /api/v1/grades?termId=term-2026-fall（未公布分数保持 null）
[{"courseId":"course-003","courseName":"计算机网络","credits":3.0,"score":null,"gradePoint":null,"termId":"term-2026-fall"}]

// GET /api/v1/exams?termId=term-2026-fall（未公布时间地点）
[{"id":"exam-003","courseName":"计算机网络","startsAt":null,"endsAt":null,"location":null,"termId":"term-2026-fall"}]

// GET /api/v1/links
[{"id":"link-001","title":"项目仓库","url":"https://github.com/cdsvsvd/-Windows-.git","category":"项目"}]

// GET /api/v1/data-status
{"provider":"LocalSQLite","isDemo":true,"updatedAt":"2026-11-02T00:00:00+08:00","message":"本地演示数据，非学校官方信息"}

// GET /api/v1/health（数据层未接入时如实返回 starting / false）
{"status":"starting","databaseReady":false,"version":"1.0.0.0"}

// POST /api/v1/local/courses、PUT /api/v1/local/courses/{id} 的请求体（无 id）
{"name":"编译原理","teacher":"示例教师戊","location":"演示楼E501",
 "dayOfWeek":3,"startSection":3,"endSection":4,"weekNumbers":[1,3,5],"termId":"term-2026-fall"}

// 任意错误响应
{"code":"invalid_request","message":"termId 不能为空","traceId":"00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"}
```

## 4.1 写接口的请求 / 响应模型

| 方法 | 请求体 | 成功响应 | 失败 |
| --- | --- | --- | --- |
| `POST /api/v1/local/courses` | `CourseUpsertRequest` | `201` + `CourseDto`（含新生成的 `id`），`Location` 指向新资源 | 400 `invalid_request`（字段非法/周次越界）、404 `not_found`（未知 termId）、409 `conflict`、503 `db_unavailable` |
| `PUT /api/v1/local/courses/{id}` | `CourseUpsertRequest` | `200` + 更新后的 `CourseDto` | 同上 + 404（未知课程） |
| `DELETE /api/v1/local/courses/{id}` | 无 | `204` 无响应体 | 404、409（仍有关联成绩/考试）、503 |

约定：新增时 `id` 由后端生成（客户端不得提交）；编辑以路径 `id` 为准，请求体里没有 `id` 字段，避免路径与体冲突；
`WeekNumbers` 必须去重且落在该学期 `week_count` 范围内；删除策略（拒绝 vs 级联）由 04 号定，Api 只映射错误码。

## 5. 星期换算（最容易出错的一处）

| 语义 | SQLite `courses.day_of_week` | C# `DayOfWeek` / JSON `dayOfWeek` |
| --- | --- | --- |
| 周一 | 1 | 1 |
| 周六 | 6 | 6 |
| **周日** | **7** | **0** |

- 库 → DTO：`(DayOfWeek)(dbDay % 7)`。
- DTO → 库：`dbDay = dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek`。
- 前端不要假设 `dayOfWeek` 是 1–7；渲染前先换算。
- 备选方案（待评审）：改用 `JsonStringEnumConverter` 输出 `"Monday"`。这会让已有客户端绑定失效，需 1/2 号确认后再定，**当前保持数字**。

## 6. 变更流程

1. 字段增删改 = 契约变更：先提 Issue，说明受影响方（Desktop 绑定、Api 端点、06 的端到端用例）。
2. 变更 PR 必须同时更新本 README 表格与 `02_统一规范` 冻结清单，并附 `tools/check_contract_signatures.py` 输出。
3. 破坏性变更（改名/改类型/改可空性）需 1、2、4、5 号至少各一名确认；禁止在合并前直推 `main`。
4. 合并顺序：Contracts 小 PR 先行 → 4 号仓储抽象 → 5 号 EF/迁移 → 3 号 DI 与端点接入 → 1/2 号联调。

## 7. 待评审项

| 事项 | 现状 | 影响 |
| --- | --- | --- |
| `GET /api/v1/health` 响应体 | 冻结文档未定义；**已按建议实现 `HealthDto` 并登记为待评审** | 评审通过后 `/health` 可交付；形状若被否，改动仅 `Dtos/HealthDto.cs` + Api 一处 |
| 401 会话鉴权失败 | 冻结错误码只有 400/404/409/500/503 | 握手失败需要 `unauthorized`；当前集中在 `ApiResults.UnauthorizedCode` 一处，未写入 `ApiErrorCodes` |
| 写接口请求体 | **已实现 `CourseUpsertRequest`**（`OpenAHU.Contracts.Local`），字段与校验语义待 4/5 号确认 | 2 号可先绑定表单；字段若变，改动在契约 + 2 号绑定各一处 |
| 契约版本常量 | 暂无 `ContractVersion` | 若 6 号 CI 需要断言版本，再单独加 |

## 8. 验证状态（如实记录）

- ✅ 已执行：`python src/OpenAHU.Contracts/tools/check_contract_signatures.py` → 8 个冻结类型签名逐字一致、无多余类型、均 `sealed`、文件名匹配，另有 **2 个待评审模型**（`HealthDto`、`CourseUpsertRequest`），退出码 0。
- ❌ 未执行：`dotnet restore / build / test`。本机 `dotnet --version` 报 **no .NET SDK was found**，`net10.0` 无法编译；因此不声称任何编译或运行通过。
- ❌ 未执行：git 提交 / PR。当前工作目录不是 git 仓库（`git status` → not a git repository），无远程可推。
- ⚠️ 待办：仓库若已有 `net9.0` 或其他目标框架约定，需按实际 `global.json` 调整 `TargetFramework`（可能涉及跨组确认）。
- ⚠️ 待办：`DTO` 与 JSON 样例中的示例链接仍指向旧仓库名 `-Windows-`（已重定向到 `OpenAHU_Windows`），属文档与 Seed 数据变更，不在 3 号目录。
