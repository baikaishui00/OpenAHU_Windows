namespace OpenAHU.Infrastructure;

public sealed class Student
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? StudentNumber { get; set; }
    public string? College { get; set; }
    public string? Major { get; set; }
    public int? GradeYear { get; set; }
    public string CreatedAtUtc { get; set; } = "";
}
public sealed class Term
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string StartDate { get; set; } = "";
    public int WeekCount { get; set; }
    public bool IsCurrent { get; set; }
}
public sealed class Course
{
    public string Id { get; set; } = "";
    public string TermId { get; set; } = "";
    public string? CourseCode { get; set; }
    public string Name { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Location { get; set; } = "";
    public int DayOfWeek { get; set; }
    public int StartSection { get; set; }
    public int EndSection { get; set; }
    public List<CourseWeek> Weeks { get; set; } = new();
    public List<Enrollment> Enrollments { get; set; } = new();
}
public sealed class CourseWeek
{
    public string CourseId { get; set; } = "";
    public int WeekNumber { get; set; }
}
public sealed class Enrollment
{
    public string StudentId { get; set; } = "";
    public string CourseId { get; set; } = "";
}
public sealed class Grade
{
    public string StudentId { get; set; } = "";
    public string CourseId { get; set; } = "";
    public double Credits { get; set; }
    public double? Score { get; set; }
    public double? GradePoint { get; set; }
}
public sealed class Exam
{
    public string Id { get; set; } = "";
    public string CourseId { get; set; } = "";
    public string? StartsAt { get; set; }
    public string? EndsAt { get; set; }
    public string? Location { get; set; }
}
public sealed class CampusLink
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Category { get; set; } = "常用";
}
public sealed class AppSetting
{
    public string SettingKey { get; set; } = "";
    public string SettingValue { get; set; } = "";
}
