using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenAHU.Infrastructure;

namespace OpenAHU.Infrastructure.Migrations;

[DbContext(typeof(CampusDbContext))]
[Migration("202611050001_InitialLocalDatabase")]
public sealed class InitialLocalDatabase : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE students (
  id TEXT PRIMARY KEY,
  display_name TEXT NOT NULL,
  student_number TEXT UNIQUE,
  college TEXT,
  major TEXT,
  grade_year INTEGER,
  created_at_utc TEXT NOT NULL
);
""");
        migrationBuilder.Sql("""
CREATE TABLE terms (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  start_date TEXT NOT NULL, -- YYYY-MM-DD；作为首个教学周周一，演示日期为虚构
  week_count INTEGER NOT NULL CHECK (week_count BETWEEN 1 AND 30),
  is_current INTEGER NOT NULL DEFAULT 0 CHECK (is_current IN (0,1))
);
""");
        migrationBuilder.Sql("""
CREATE TABLE courses (
  id TEXT PRIMARY KEY,
  term_id TEXT NOT NULL REFERENCES terms(id) ON DELETE RESTRICT,
  course_code TEXT,
  name TEXT NOT NULL,
  teacher TEXT NOT NULL DEFAULT '',
  location TEXT NOT NULL DEFAULT '',
  day_of_week INTEGER NOT NULL CHECK (day_of_week BETWEEN 1 AND 7), -- 周一=1、周日=7
  start_section INTEGER NOT NULL CHECK (start_section BETWEEN 1 AND 20),
  end_section INTEGER NOT NULL CHECK (end_section BETWEEN start_section AND 20)
);
""");
        migrationBuilder.Sql("""
CREATE TABLE course_weeks (
  course_id TEXT NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
  week_number INTEGER NOT NULL CHECK (week_number BETWEEN 1 AND 30),
  PRIMARY KEY(course_id,week_number)
);
""");
        migrationBuilder.Sql("""
CREATE TABLE enrollments (
  student_id TEXT NOT NULL REFERENCES students(id) ON DELETE CASCADE,
  course_id TEXT NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
  PRIMARY KEY(student_id, course_id)
);
""");
        migrationBuilder.Sql("""
CREATE TABLE grades (
  student_id TEXT NOT NULL,
  course_id TEXT NOT NULL,
  credits REAL NOT NULL CHECK(credits >= 0 AND credits <= 50),
  score REAL CHECK(score IS NULL OR (score BETWEEN 0 AND 100)),
  grade_point REAL CHECK(grade_point IS NULL OR (grade_point BETWEEN 0 AND 5)),
  PRIMARY KEY(student_id, course_id),
  FOREIGN KEY(student_id,course_id) REFERENCES enrollments(student_id,course_id) ON DELETE CASCADE
);
""");
        migrationBuilder.Sql("""
CREATE TABLE exams (
  id TEXT PRIMARY KEY,
  course_id TEXT NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
  starts_at TEXT, -- ISO 8601带偏移，如2026-12-21T09:00:00+08:00
  ends_at TEXT,
  location TEXT,
  CHECK (starts_at IS NULL OR ends_at IS NULL OR ends_at > starts_at)
);
""");
        migrationBuilder.Sql("""
CREATE TABLE campus_links (
  id TEXT PRIMARY KEY,
  title TEXT NOT NULL,
  url TEXT NOT NULL,
  category TEXT NOT NULL DEFAULT '常用'
);
""");
        migrationBuilder.Sql("""
CREATE TABLE app_settings (
  setting_key TEXT PRIMARY KEY,
  setting_value TEXT NOT NULL
);
""");
        migrationBuilder.Sql("""
CREATE INDEX idx_courses_term_schedule ON courses(term_id,day_of_week,start_section);
""");
        migrationBuilder.Sql("""
CREATE INDEX idx_exams_course ON exams(course_id);
""");
        migrationBuilder.Sql("""
CREATE INDEX idx_enrollments_student ON enrollments(student_id);
""");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("app_settings");
        migrationBuilder.DropTable("campus_links");
        migrationBuilder.DropTable("exams");
        migrationBuilder.DropTable("grades");
        migrationBuilder.DropTable("enrollments");
        migrationBuilder.DropTable("course_weeks");
        migrationBuilder.DropTable("courses");
        migrationBuilder.DropTable("terms");
        migrationBuilder.DropTable("students");
    }
}
