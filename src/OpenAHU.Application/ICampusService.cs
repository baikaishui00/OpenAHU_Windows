using OpenAHU.Contracts.Dtos;
using OpenAHU.Contracts.Local;

namespace OpenAHU.Application;

/// <summary>API唯一业务入口，3号端点只依赖此类型。</summary>
public interface ICampusService
{
    Task<StudentProfileDto> GetProfileAsync(CancellationToken ct);
    Task<IReadOnlyList<TermDto>> GetTermsAsync(CancellationToken ct);
    Task<IReadOnlyList<CourseDto>> GetCoursesAsync(string termId, CancellationToken ct);
    Task<IReadOnlyList<GradeDto>> GetGradesAsync(string termId, CancellationToken ct);
    Task<IReadOnlyList<ExamDto>> GetExamsAsync(string termId, CancellationToken ct);
    Task<IReadOnlyList<LinkDto>> GetLinksAsync(CancellationToken ct);
    Task<CourseDto> CreateCourseAsync(CourseUpsertRequest request, CancellationToken ct);
    Task<CourseDto> UpdateCourseAsync(string id, CourseUpsertRequest request, CancellationToken ct);
    Task DeleteCourseAsync(string id, CancellationToken ct);
}
