# 03 号 → 04 / 05 号：接口对接需求单

> 状态：**待评审**。本文只描述 03 号（Api + Contracts）需要 04（Application）与 05（Infrastructure）提供什么、
> 以及两侧的语义边界，避免各自实现后才发现形状不一致。
> 不涉及任何学校站点；数据源固定为本地 SQLite 演示库。

## 0. 为什么需要这张单子

路由、DTO、错误码已冻结（`src/OpenAHU.Contracts`），但 Api 现在只能返回 `503`——因为缺少 Application 的查询接口。
如果不先对齐“Application 返回什么、错误怎么表达、转换在哪一层做”，三处会同时返工：

1. 04 号按自己的形状返回 → 03 号在 Api 里写第二套映射；
2. 05 号仓储抛 `DbUpdateException` → 03 号为了映射 503 被迫 `using Microsoft.EntityFrameworkCore`（违反分层）；
3. `dayOfWeek` 两边各转一次 → 周日错一格。

---

## 1. 先决问题 A：Application 是否引用 `OpenAHU.Contracts`

**冲突点**：`02_统一规范/00_...md` 写的是「Contracts 被 Desktop / Api / 必要测试引用」，
但没写 Application；而 `CourseDto` 之类只能由 Contracts 定义。于是有两种合法读法：

| | 方案 A：Application 返回自有只读模型 | 方案 B：Application 直接返回 DTO |
| --- | --- | --- |
| 类型 | 领域模型 + Contracts DTO（两套） | 只有 Contracts DTO |
| 映射位置 | Api 层（3 号） | 无映射 |
| 分层纯度 | 业务层不感知传输形状 ✅ | 业务层引用传输类型 ⚠️ |
| 代码量 | 多一层 mapper + 映射测试 | 少 |
| 文档合规 | 严格符合“Contracts 只被 Desktop/Api/测试引用” | 需要评审确认引用方向 |

**我的建议：方案 A**，理由：① 与“Application 不依赖 HTTP”一致；② `WeekNumbers`、空成绩、排序等业务规则留在 04 号手里，
Api 只做形状映射；③ 06 号的端到端测试能独立验证 JSON 形状。
**代价**：多一组 `CourseView` 之类的模型与 mapper（约 200 行），由我（3 号）写。

**需拍板**：组长 + 4 号。若选 B，请明确写入规范，我按 B 实现（改动更小）。

## 2. 先决问题 B：`503 db_unavailable` 由谁判定

数据库文件被占用、只读、迁移未完成时，异常从 05 号冒出。Api **不能**引用 EF Core 来捕获它。

**建议**：04 号定义领域异常 `CampusStoreUnavailableException`（放在 Application），05 号在仓储层把
`SqliteException` 包装成它，03 号在 `ApiExceptionHandler` 里映射 `503 + db_unavailable`。
替代方案是返回结果对象 `QueryResult<T>`，但异常方案改动更小，且不污染返回类型。

**需拍板**：4、5 号。定之前，我的兜底是“所有异常 → 500 internal”（当前实现）。

## 3. 我需要的查询接口（草案，签名可谈）

```csharp
// 全部带 CancellationToken；全部只返回“当前本地演示资料”范围内的数据，
// 不接受任何 studentId 参数（身份由后端 app_settings.active_demo_student_id 决定）。
public interface IProfileQuery            { Task<CurrentProfile> GetActiveAsync(CancellationToken ct); }
public interface ITermQuery               { Task<IReadOnlyList<TermView>> GetAllAsync(CancellationToken ct); }
public interface ICourseQuery             { Task<IReadOnlyList<CourseView>> GetByTermAsync(string termId, CancellationToken ct); }
public interface IGradeQuery              { Task<IReadOnlyList<GradeView>> GetByTermAsync(string termId, CancellationToken ct); }
public interface IExamQuery               { Task<IReadOnlyList<ExamView>> GetByTermAsync(string termId, CancellationToken ct); }
public interface ILinkQuery               { Task<IReadOnlyList<LinkView>> GetAllAsync(CancellationToken ct); }
public interface IDataStatusQuery         { Task<DateTimeOffset?> GetLastUpdatedAsync(CancellationToken ct); }
```

写链路（第二阶段，先冻结语义）：

```csharp
public sealed record CourseUpsertRequest(string Name, string Teacher, string Location,
    DayOfWeek DayOfWeek, int StartSection, int EndSection, IReadOnlyList<int> WeekNumbers, string TermId);

public interface ICourseCommand
{
    Task<CourseWriteResult> CreateAsync(CourseUpsertRequest request, CancellationToken ct);
    Task<CourseWriteResult> UpdateAsync(string courseId, CourseUpsertRequest request, CancellationToken ct);
    Task<CourseWriteResult> DeleteAsync(string courseId, CancellationToken ct);
}
// CourseWriteResult: Ok(courseId) | ValidationFailed(field, message) | UnknownTerm | NotFound | Conflict(reason)
```

## 4. 语义细节（不写清楚必然返工）

| 主题 | 约定 | 归属 |
| --- | --- | --- |
| 未知 `termId` | **建议 404 `not_found`**（资源不存在）；缺失/空白 `termId` 已由 Api 返回 400 | 4 号确认 |
| 课程排序 | `termId, dayOfWeek, startSection` 升序 | 4 号 |
| `weekNumbers` | 升序、去重；空数组表示未排课（不返回 `null`） | 4 号 |
| 成绩空值 | `score`/`gradePoint` 保持 `null`，**不代入 0**，不算 GPA | 4 号 |
| 考试排序 | `startsAt` 升序，`null` 排末尾 | 4 号 |
| 星期转换 | **只在 4/5 号做一次**：库 `1..7` → `DayOfWeek`（周日→0）；Api 不再转换 | 4/5 号 |
| 学分类型 | 库 `REAL` → `decimal`；禁止用 `double` | 5 号 |
| 时间 | 库内带偏移 ISO 字符串 → `DateTimeOffset`；`null` 保持 `null` | 5 号 |
| 身份 | 只读 `app_settings.active_demo_student_id`，端点拒绝 `studentId` 查询参数 | 3/4 号 |

## 5. 错误映射表（唯一出口，我实现）

| 场景 | 判定方 | HTTP | `code` |
| --- | --- | --- | --- |
| 缺参、格式非法、越界 | Api（已实现）或 Application 校验结果 | 400 | `invalid_request` |
| 未知课程 id、未知 termId | Application | 404 | `not_found` |
| 删除仍有成绩/考试的课程、并发写入冲突 | Application | 409 | `conflict` |
| 未捕获异常 | Api | 500 | `internal` |
| 数据库不可用 | Application（异常或结果） | 503 | `db_unavailable` |
| 会话令牌无效 | Api 中间件 | 401 | `unauthorized`（**待评审**，尚未进 `ApiErrorCodes`） |

## 6. 我发现的一个数据缺口：`UpdatedAt` 没有来源

`DataStatusDto.UpdatedAt` 目前恒为 `null`，因为 **schema 里没有任何 `updated_at` 列**
（`courses`、`grades` 都只有业务字段）。三种选择，需要 5 号定：

1. 迁移给 `courses`/`grades` 加 `updated_at`（可追溯，但改表 + 迁移）；
2. 用数据库文件 `LastWriteTime`（不改表，但含索引/日志噪声）；
3. 明确该字段首版恒为 `null`，前端不展示“更新时间”。

我倾向 **3**（首版最省事、不撒谎），若组长要求展示时间则走 1。

## 7. 03 号已经给出、你们可以直接用的东西

- `src/OpenAHU.Contracts/`：8 个 DTO + `ApiError` + `ApiErrorCodes` + `ContractJson`（JSON 约定、camelCase、日期格式）。
- `src/OpenAHU.Api/`：路由表、会话握手、错误映射、`termId` 校验（400）。
- 契约漂移检查：`python src/OpenAHU.Contracts/tools/check_contract_signatures.py`
- 路由覆盖检查：`python src/OpenAHU.Api/tools/check_route_coverage.py --allow-missing /api/v1/health`

## 8. 我接手时需要你们给的（最小信息）

1. 04 号：上面 7 个接口的最终签名 + 先决问题 A/B 的结论；
2. 05 号：`AddInfrastructure(...)` 扩展方法名与配置键（建议 `OpenAHU:DatabasePath`，默认 `%LocalAppData%\OpenAHU\campus.db`）；
3. 5 号：`UpdatedAt` 的选择（第 6 节）；
4. 3、5 号协作点：DI 注册写在 Api 的 Composition Root，我不改 Infrastructure 文件，你不改 `Program.cs`。

## 9. 阻塞链（当前）

```
评测 HealthDto + 401 码（组长）
        │
        ├── /health 实现（3 号）── Gate A 用例（6 号）
        │
04 号 接口 A/B 结论 ── 3 号替换 9 个占位端点 ── Gate B（读链路）
        │
05 号 EF/迁移 + 仓储 ── 5 号 Seed ── 重启持久化验收（Gate C）
```
