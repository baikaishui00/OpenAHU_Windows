using OpenAHU.Contracts.Dtos;
using OpenAHU.Contracts.Local;

namespace OpenAHU.Application;

/// <summary>仓储抽象不依赖 EF Core 或 SQLite，供5号的数据实现替换。</summary>
public interface ICampusRepository
{
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
    Task<StudentProfileDto?> GetProfileAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TermDto>> GetTermsAsync(CancellationToken cancellationToken);
    Task<TermDto?> GetTermAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseDto>> GetCoursesAsync(string termId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GradeDto>> GetGradesAsync(string termId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExamDto>> GetExamsAsync(string termId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LinkDto>> GetLinksAsync(CancellationToken cancellationToken);
    Task<CourseDto> CreateCourseAsync(CourseUpsertRequest request, CancellationToken cancellationToken);
    Task<CourseDto> UpdateCourseAsync(string id, CourseUpsertRequest request, CancellationToken cancellationToken);
    Task DeleteCourseAsync(string id, CancellationToken cancellationToken);
}
