# Usage: python diff.py <old.prfb.yaml> <new.prfb.yaml> [path]
# Compares two prefabs by object path instead of Id, optionally within one subtree.
import difflib
import sys

from model.prefab import Prefab, Ref


def rendered(node, paths):
    # the node's lines with references as paths, and its children
    lines = [f"{line.prefix}-> {paths.get(line.node, '?')}" if isinstance(line, Ref) else line for line in node.lines]
    return lines + [f"  child {paths[c].rsplit('/', 1)[-1]}" for c in node.children]


def diff(old, new, under=""):
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
        if a != b:
            out.append(f"~ {path or '(root)'}")
            out.extend(f"    {line}" for line in difflib.unified_diff(a, b, n=0, lineterm="")
                       if not line.startswith(("---", "+++", "@@")))
    return out


def main():
    under = sys.argv[3] if len(sys.argv) > 3 else ""
    out = diff(Prefab.load(sys.argv[1]), Prefab.load(sys.argv[2]), under)
    print("\n".join(out) if out else "no differences")
    sys.exit(1 if out else 0)


if __name__ == "__main__":
    main()
