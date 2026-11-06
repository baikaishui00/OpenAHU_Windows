-- OpenAHU Windows v3: SQLite 数据库参考结构（只供本地样例数据开发）
-- 实际 C# 后端数据库结构由 EF Core Migrations 负责；不要在同一数据库重复执行此脚本与首次迁移。
PRAGMA foreign_keys = ON;
CREATE TABLE students (
  id TEXT PRIMARY KEY,
  display_name TEXT NOT NULL,
  student_number TEXT UNIQUE,
  college TEXT,
  major TEXT,
  grade_year INTEGER,
  created_at_utc TEXT NOT NULL
);
CREATE TABLE terms (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  start_date TEXT NOT NULL, -- YYYY-MM-DD；作为首个教学周周一，演示日期为虚构
  week_count INTEGER NOT NULL CHECK (week_count BETWEEN 1 AND 30),
  is_current INTEGER NOT NULL DEFAULT 0 CHECK (is_current IN (0,1))
);
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
CREATE TABLE course_weeks (
  course_id TEXT NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
  week_number INTEGER NOT NULL CHECK (week_number BETWEEN 1 AND 30),
  PRIMARY KEY(course_id,week_number)
);
CREATE TABLE enrollments (
  student_id TEXT NOT NULL REFERENCES students(id) ON DELETE CASCADE,
  course_id TEXT NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
  PRIMARY KEY(student_id, course_id)
);
CREATE TABLE grades (
  student_id TEXT NOT NULL,
  course_id TEXT NOT NULL,
  credits REAL NOT NULL CHECK(credits >= 0 AND credits <= 50),
  score REAL CHECK(score IS NULL OR (score BETWEEN 0 AND 100)),
  grade_point REAL CHECK(grade_point IS NULL OR (grade_point BETWEEN 0 AND 5)),
  PRIMARY KEY(student_id, course_id),
  FOREIGN KEY(student_id,course_id) REFERENCES enrollments(student_id,course_id) ON DELETE CASCADE
);
CREATE TABLE exams (
  id TEXT PRIMARY KEY,
  course_id TEXT NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
  starts_at TEXT, -- ISO 8601带偏移，如2026-12-21T09:00:00+08:00
  ends_at TEXT,
  location TEXT,
  CHECK (starts_at IS NULL OR ends_at IS NULL OR ends_at > starts_at)
);
CREATE TABLE campus_links (
  id TEXT PRIMARY KEY,
  title TEXT NOT NULL,
  url TEXT NOT NULL,
  category TEXT NOT NULL DEFAULT '常用'
);
CREATE TABLE app_settings (
  setting_key TEXT PRIMARY KEY,
  setting_value TEXT NOT NULL
);
CREATE INDEX idx_courses_term_schedule ON courses(term_id,day_of_week,start_section);
CREATE INDEX idx_exams_course ON exams(course_id);
CREATE INDEX idx_enrollments_student ON enrollments(student_id);
