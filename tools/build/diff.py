# Usage: python diff.py (<old.prfb.yaml> <new.prfb.yaml> | --rev <commit>) [path] [--tolerance <t>]
# Compares two prefabs, or each prefab a commit changed with its parent's, by object path instead of Id.
import argparse
import difflib
import re
import sys

from context import TARGETS, git_show
from model.prefab import Prefab, Ref

NUMBER = re.compile(r"(-?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?)")


def rendered(node, paths):
    # the node's lines with references as paths, and its children
    lines = [f"{line.prefix}-> {paths.get(line.node, '?')}" if isinstance(line, Ref) else line for line in node.lines]
    return lines + [f"  child {paths[c].rsplit('/', 1)[-1]}" for c in node.children]


def close(a, b, tolerance):
    # the lines differ only in numbers, each by at most tolerance
    a, b = NUMBER.split(a), NUMBER.split(b)
    if len(a) != len(b):
        return False
    for i, (x, y) in enumerate(zip(a, b)):
        if i % 2 == 0:
            if x != y:
                return False
        elif x != y and abs(float(x) - float(y)) > tolerance:
            return False
    return True


def tolerate(a, b, tolerance):
    # b with each line close to the line it replaces in a set to that line
    b = list(b)
    for op, i1, i2, j1, j2 in difflib.SequenceMatcher(None, a, b, autojunk=False).get_opcodes():
        if op == "replace" and i2 - i1 == j2 - j1:
            for i, j in zip(range(i1, i2), range(j1, j2)):
                if close(a[i], b[j], tolerance):
                    b[j] = a[i]
    return b


def diff(old, new, under="", tolerance=0):
    # report lines, empty when the prefabs match
    old_paths, new_paths = old.paths(), new.paths()
    old_by, new_by = {p: n for n, p in old_paths.items()}, {p: n for n, p in new_paths.items()}

    def inside(path):
        return not under or path == under or path.startswith(under + "/")

    def subtree_root(path, others):
        parent = path.rsplit("/", 1)[0] if "/" in path else ""
        return parent in others or parent == ""

    out = []
    for path in old_by:
        if inside(path) and path not in new_by and subtree_root(path, new_by):
            out.append(f"- {path} ({sum(1 for _ in old_by[path].walk())} objects)")
    for path in new_by:
        if inside(path) and path not in old_by and subtree_root(path, old_by):
            out.append(f"+ {path} ({sum(1 for _ in new_by[path].walk())} objects)")
    for path, node in old_by.items():
        if not inside(path) or path not in new_by:
            continue
        a, b = rendered(node, old_paths), rendered(new_by[path], new_paths)
        if tolerance and a != b:
            b = tolerate(a, b, tolerance)
        if a != b:
            out.append(f"~ {path or '(root)'}")
            out.extend(f"    {line}" for line in difflib.unified_diff(a, b, n=0, lineterm="")
                       if not line.startswith(("---", "+++", "@@")))
    return out


def revision(rev, under="", tolerance=0):
    # per prefab the commit changed, the report against its parent
    reports = {}
    for name, path in TARGETS.items():
        old, new = git_show(f"{rev}^", path), git_show(rev, path)
        if old != new and old is not None and new is not None:
            reports[name] = diff(Prefab.parse(old), Prefab.parse(new), under, tolerance)
    return reports


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("files", nargs="*")
    parser.add_argument("--rev")
    parser.add_argument("--tolerance", type=float, default=0)
    args = parser.parse_args()

    if args.rev:
        if len(args.files) > 1:
            parser.error("--rev takes at most a path")
        reports = revision(args.rev, args.files[0] if args.files else "", args.tolerance)
        if not reports:
            print("no prefab changed")
        for name, out in reports.items():
            print(f"{name}:")
            print("\n".join(f"  {line}" for line in out) if out else "  no differences")
        sys.exit(1 if any(reports.values()) else 0)
    if len(args.files) not in (2, 3):
        parser.error("pass two prefabs and an optional path, or --rev")
    under = args.files[2] if len(args.files) > 2 else ""
    out = diff(Prefab.load(args.files[0]), Prefab.load(args.files[1]), under, args.tolerance)
    print("\n".join(out) if out else "no differences")
    sys.exit(1 if out else 0)


if __name__ == "__main__":
    main()
