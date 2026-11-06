#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""契约漂移检查：把 Contracts 源码里的 DTO 签名与冻结文档逐字比对。

用法（在仓库根目录）：
    python src/OpenAHU.Contracts/tools/check_contract_signatures.py
    python src/OpenAHU.Contracts/tools/check_contract_signatures.py --doc 02_统一规范/00_所有AI必须遵守_统一上下文.md

检查项：
  1. 文档 `- `XxxDto(...)`` 里的参数（类型 + 名称 + 顺序）与源码 `public sealed record XxxDto(...)` 完全一致；
  2. 源码出现的 record 集合与文档一致（不多不少）；
  3. 每个 record 都是 `sealed`，且所在文件名等于类型名。

本脚本不编译 C#，只是文本级护栏；真正的编译验证仍需 .NET SDK（见 README「验证状态」）。
退出码 0 = 全部通过；1 = 存在漂移。
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

DOC_PATTERN = re.compile(r"-\s*`(\w+)\s*\(([^`]*)\)`")
RECORD_PATTERN = re.compile(r"public\s+(sealed\s+)?record\s+(\w+)\s*\(([^;{]*?)\)\s*;", re.S)

# 已提出、等待跨组评审的公共模型：允许存在，但必须显式登记在这里，
# 评审通过后应写入冻结文档并从本集合删除（否则下一次检查会报“多出未冻结类型”）。
PENDING_REVIEW: frozenset[str] = frozenset()


def normalize_param(raw: str) -> str:
    """把 ' System.DayOfWeek   DayOfWeek ' 规范成 'DayOfWeek DayOfWeek'。"""
    text = re.sub(r"\s+", " ", raw.strip())
    if not text:
        return ""
    # 最后一段是参数名，前面是类型；去掉 System. 前缀便于与文档比对。
    parts = text.rsplit(" ", 1)
    if len(parts) != 2:
        return text
    type_name, param_name = parts
    type_name = type_name.replace("System.", "")
    return f"{type_name} {param_name}"


def parse_params(text: str) -> list[str]:
    return [normalize_param(p) for p in text.split(",") if p.strip()]


def parse_doc(doc_path: Path) -> dict[str, list[str]]:
    body = doc_path.read_text(encoding="utf-8-sig")
    frozen: dict[str, list[str]] = {}
    for match in DOC_PATTERN.finditer(body):
        frozen[match.group(1)] = parse_params(match.group(2))
    return frozen


def parse_sources(contracts_dir: Path) -> tuple[dict[str, list[str]], dict[str, dict]]:
    found: dict[str, list[str]] = {}
    meta: dict[str, dict] = {}
    for path in sorted(contracts_dir.rglob("*.cs")):
        text = path.read_text(encoding="utf-8-sig")
        for match in RECORD_PATTERN.finditer(text):
            is_sealed = match.group(1) is not None
            name = match.group(2)
            found[name] = parse_params(match.group(3))
            meta[name] = {"file": path.name, "path": str(path), "sealed": is_sealed}
    return found, meta


def main() -> int:
    root = Path(__file__).resolve().parents[3]
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--doc",
        default=str(root / "02_统一规范" / "00_所有AI必须遵守_统一上下文.md"),
    )
    parser.add_argument("--contracts", default=str(root / "src" / "OpenAHU.Contracts"))
    args = parser.parse_args()

    doc_path = Path(args.doc)
    contracts_dir = Path(args.contracts)
    if not doc_path.is_file():
        print(f"FAIL 找不到冻结文档：{doc_path}")
        return 1

    frozen = parse_doc(doc_path)
    found, meta = parse_sources(contracts_dir)
    problems: list[str] = []

    if not frozen:
        problems.append("冻结文档里没有解析到任何 DTO 签名，检查文档格式是否被改动")

    for name, expected in frozen.items():
        actual = found.get(name)
        if actual is None:
            problems.append(f"缺失类型：{name}")
            continue
        if actual != expected:
            problems.append(f"签名漂移：{name}\n    文档: {expected}\n    源码: {actual}")

    pending = set(PENDING_REVIEW)
    for name in sorted(set(found) - set(frozen)):
        if name in pending:
            continue
        problems.append(f"多出未冻结类型：{name}（{meta[name]['file']}）")

    for name in sorted(pending - set(found)):
        problems.append(f"待评审清单登记的 {name} 在源码中不存在，请从 PENDING_REVIEW 删除")

    for name, info in sorted(meta.items()):
        if not info["sealed"]:
            problems.append(f"未 sealed：{name}（{info['file']}）")
        if info["file"] != f"{name}.cs":
            problems.append(f"文件名与类型名不一致：{name} 在 {info['file']}")

    print(f"docs  : {doc_path}")
    print(f"source: {contracts_dir}")
    print(f"冻结文档类型数 {len(frozen)}，源码 record 数 {len(found)}")
    for name in sorted(frozen):
        status = "OK  " if name in found and found[name] == frozen[name] else "FAIL"
        print(f"  [{status}] {name}({', '.join(found.get(name, ['<缺失>']))})")
    for name in sorted(pending & set(found)):
        print(f"  [待评审] {name}({', '.join(found[name])})")

    if problems:
        print("\n失败项：")
        for item in problems:
            print("  - " + item)
        print("\n结果：契约漂移，需修正后再提交 PR")
        return 1

    extra = f"，另有 {len(pending & set(found))} 个待评审模型" if pending & set(found) else ""
    print(f"\n结果：全部通过（签名逐字一致、无多余类型、均 sealed、文件名匹配{extra}）")
    print("注意：本检查不编译 C#；编译与用例执行需 .NET SDK。")
    return 0


if __name__ == "__main__":
    # GitHub Actions Windows consoles may default to cp1252; force Unicode-safe output.
    for stream in (sys.stdout, sys.stderr):
        if stream is not None and hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    sys.exit(main())
