-- 全部是虚构数据，不代表安徽大学实际记录、真实教学安排或官方成绩计算。
PRAGMA foreign_keys = ON;
BEGIN TRANSACTION;
INSERT INTO students VALUES
('demo-001','演示学生甲','DEMO001','示例学院','示例专业',2024,'2026-11-02T00:00:00Z'),
('demo-002','演示学生乙','DEMO002','示例学院','示例专业',2025,'2026-11-02T00:00:00Z');
INSERT INTO terms VALUES
('term-2026-fall','2026年秋季学期（演示）','2026-09-07',18,1),
('term-2027-spring','2027年春季学期（演示）','2027-03-01',18,0);
INSERT INTO courses VALUES
('course-001','term-2026-fall','D-COMP-001','软件工程实践','示例教师甲','演示楼A101',1,1,2),
('course-002','term-2026-fall','D-COMP-002','数据库原理','示例教师乙','演示楼B202',2,3,4),
('course-003','term-2026-fall','D-COMP-003','计算机网络','示例教师丙','演示楼C303',4,5,6),
('course-004','term-2027-spring','D-COMP-004','程序设计训练','示例教师丁','演示楼A202',3,1,2);
INSERT INTO course_weeks(course_id,week_number)
WITH RECURSIVE w(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM w WHERE x<16)
SELECT 'course-001',x FROM w;
INSERT INTO course_weeks(course_id,week_number)
WITH RECURSIVE w(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM w WHERE x<18)
SELECT 'course-002',x FROM w;
INSERT INTO course_weeks(course_id,week_number)
WITH RECURSIVE w(x) AS (SELECT 2 UNION ALL SELECT x+2 FROM w WHERE x<16)
SELECT 'course-003',x FROM w;
INSERT INTO course_weeks(course_id,week_number)
WITH RECURSIVE w(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM w WHERE x<14)
SELECT 'course-004',x FROM w;
INSERT INTO enrollments VALUES
('demo-001','course-001'),('demo-001','course-002'),('demo-001','course-003'),('demo-001','course-004'),
('demo-002','course-001'),('demo-002','course-002');
INSERT INTO grades VALUES
('demo-001','course-001',3.0,88.0,NULL),
('demo-001','course-002',3.5,91.0,NULL),
('demo-001','course-003',3.0,NULL,NULL),
('demo-002','course-001',3.0,79.0,NULL);
INSERT INTO exams VALUES
('exam-001','course-001','2026-12-21T09:00:00+08:00','2026-12-21T11:00:00+08:00','演示楼D401'),
('exam-002','course-002','2026-12-23T14:00:00+08:00','2026-12-23T16:00:00+08:00','演示楼D402'),
('exam-003','course-003',NULL,NULL,NULL);
INSERT INTO campus_links VALUES
('link-001','项目仓库','https://github.com/cdsvsvd/-Windows-.git','项目'),
('link-002','GitHub','https://github.com/','开发');
INSERT INTO app_settings VALUES
('active_demo_student_id','demo-001'),
('source','LocalSQLiteDemo'),
('source_label','本地演示数据，非学校官方信息');
COMMIT;
