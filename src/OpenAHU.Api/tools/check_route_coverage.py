#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""路由覆盖检查：把 API 源码里注册的端点与冻结 API 表逐条比对。

用法（仓库根目录）：
    python src/OpenAHU.Api/tools/check_route_coverage.py

检查项：
  1. 冻结文档 `| GET | `/api/v1/xxx` |` 表格中的每条「方法 + 路径」都能在源码里找到注册；
  2. 源码没有注册冻结表以外的路径；
  3. `--allow-missing` 里列出的缺口只提示不失败（用于已评审的已知阻塞项）。

本脚本不启动服务、不编译 C#，只是文本级护栏。退出码 0 = 通过（允许的缺口除外），1 = 不一致。
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

DOC_ROW = re.compile(r"\|\s*(GET|POST|PUT|DELETE|PATCH)\s*\|\s*`(/api/v1[^`]*)`")
GROUP_PREFIX = "/api/v1"
MAP_ENDPOINT = re.compile(r'Map(Get|Post|Put|Delete)\(\s*"([^"]+)"')
# 任何形如 MapXxx(group, "METHOD", "/path") 的注册辅助方法（MapPending、MapPendingTermScoped ...）
# 都必须被识别，否则新增辅助方法会让检查误报“缺失”。
MAP_HELPER = re.compile(r'Map\w+\(\s*\w+\s*,\s*"([A-Z]+)"\s*,\s*"([^"]+)"')


def doc_routes(doc_path: Path) -> set[tuple[str, str]]:
    routes: set[tuple[str, str]] = set()
    for line in doc_path.read_text(encoding="utf-8-sig").splitlines():
        match = DOC_ROW.search(line)
        if match:
            # 文档把查询串写进了路径（如 `/api/v1/courses?termId=...`），
            # 而查询串不参与路由模板，比对前统一去掉。
            routes.add((match.group(1), match.group(2).split("?")[0]))
    return routes


def source_routes(api_dir: Path) -> set[tuple[str, str]]:
    routes: set[tuple[str, str]] = set()
    for path in sorted(api_dir.rglob("*.cs")):
        text = path.read_text(encoding="utf-8-sig")
        for match in MAP_ENDPOINT.finditer(text):
            routes.add((match.group(1).upper(), GROUP_PREFIX + match.group(2)))
        for match in MAP_HELPER.finditer(text):
            routes.add((match.group(1), GROUP_PREFIX + match.group(2)))
    return routes


def main() -> int:
    root = Path(__file__).resolve().parents[3]
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--doc",
        default=str(root / "02_统一规范" / "00_所有AI必须遵守_统一上下文.md"),
    )
    parser.add_argument("--api", default=str(root / "src" / "OpenAHU.Api"))
    parser.add_argument(
        "--allow-missing",
        action="append",
        default=[],
        help="已评审的已知缺口，可重复，如 --allow-missing '/api/v1/health'",
    )
    args = parser.parse_args()

    doc_path = Path(args.doc)
    api_dir = Path(args.api)
    if not doc_path.is_file():
        print(f"FAIL 找不到冻结文档：{doc_path}")
        return 1

    frozen = doc_routes(doc_path)
    implemented = source_routes(api_dir)

    missing = sorted(frozen - implemented, key=lambda item: (item[0], item[1]))
    extra = sorted(implemented - frozen, key=lambda item: (item[0], item[1]))

    allowed = {route.strip() for route in args.allow_missing}
    blocking = [item for item in missing if item[1] not in allowed]

    print(f"docs   : {doc_path}")
    print(f"source : {api_dir}")
    print(f"冻结 API 条数 {len(frozen)}，源码注册条数 {len(implemented)}")

    for method, path in sorted(frozen, key=lambda item: (item[0], item[1])):
        if (method, path) in implemented:
            state = "已注册"
        elif path in allowed:
            state = "允许缺口（待评审）"
        else:
            state = "缺失"
        print(f"  [{state}] {method} {path}")

    for method, path in extra:
        print(f"  [多余] {method} {path}")

    problems = [f"缺失：{m} {p}" for m, p in blocking] + [
        f"冻结表之外的路径：{m} {p}" for m, p in extra
    ]
    if problems:
        print("\n失败项：")
        for item in problems:
            print("  - " + item)
        return 1

    if missing:
        print("\n结果：仅存在已声明缺口，其余路由与冻结表一致")
    else:
        print("\n结果：路由表与冻结 API 表完全一致")
    print("注意：本检查不启动服务、不编译 C#。")
    return 0


if __name__ == "__main__":
    # GitHub Actions Windows consoles may default to cp1252; force Unicode-safe output.
    for stream in (sys.stdout, sys.stderr):
        if stream is not None and hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    sys.exit(main())
