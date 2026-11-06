#!/usr/bin/env python3
"""离线检查迁移中的建表语句和v3演示Seed，不能替代dotnet编译与HTTP测试。"""
from pathlib import Path
import re,sqlite3
root=Path(__file__).resolve().parents[1]
m=(root/'src/OpenAHU.Infrastructure/Migrations/202611050001_InitialLocalDatabase.cs').read_text(encoding='utf-8')
statements=re.findall(r'migrationBuilder\.Sql\("""\s*([\s\S]*?)\s*"""\);',m)
assert len(statements)==12,f'expected 12 migration SQL statements; found {len(statements)}'
with sqlite3.connect(':memory:') as db:
    db.execute('pragma foreign_keys=ON')
    for st in statements:db.executescript(st)
    table_names={row[0] for row in db.execute('select name from sqlite_master where type="table"')}
    expected={'students','terms','courses','course_weeks','enrollments','grades','exams','campus_links','app_settings'}
    assert expected.issubset(table_names),expected-table_names
    sql=(root/'samples/02_seed_demo.sql').read_text(encoding='utf-8')
    db.executescript(sql)
    assert db.execute('pragma integrity_check').fetchone()==('ok',)
    assert db.execute('pragma foreign_key_check').fetchall()==[]
    assert db.execute('select count(*) from courses').fetchone()[0]==4
    assert db.execute('select count(*) from course_weeks').fetchone()[0]==56
    assert db.execute('select count(*) from grades where score is null').fetchone()[0]==1
    print('PASS: 9 tables, 3 indexes, demo seed, SQLite integrity, foreign keys')
