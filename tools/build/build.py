# Usage: python build.py [--stock <dir>] [--check [--tolerance <t>]]
# Builds the mod's prefabs and Generated/CardIds.g.cs from the steps below.
import argparse
import os
import sys

import codegen
import legacy
from context import REPO, TARGETS, Context
from diff import diff
from model.prefab import Prefab

# the build steps in order, each with apply(ctx)
STEPS = [legacy]


def build(stock=None):
    ctx = Context(REPO, stock)
    for step in STEPS:
        step.apply(ctx)
    return ctx


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--stock")
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--tolerance", type=float, default=0)
    args = parser.parse_args()

    ctx = build(args.stock)
    stale = False
    outputs = [(name, path, ctx.prefab(name).text()) for name, path in TARGETS.items()]
    outputs.append(("CardIds", codegen.OUTPUT, codegen.generate(ctx.prefab("status01"), ctx.exports)))
    for name, path, text in outputs:
        full = os.path.join(REPO, path)
        committed = None
        if os.path.exists(full):
            with open(full, encoding="utf-8", newline="") as file:
                committed = file.read()
        if text == committed:
            print(f"{name}: up to date")
            continue
        if args.check:
            stale = True
            print(f"{name}: differs from a fresh build")
            if name in TARGETS and committed is not None:
                report = diff(Prefab.parse(committed), ctx.prefab(name), tolerance=args.tolerance)
                for line in report or ["(no differences beyond the tolerance)" if args.tolerance else
                                       "(same objects, different text)"]:
                    print(f"  {line}")
        else:
            os.makedirs(os.path.dirname(full), exist_ok=True)
            with open(full, "w", encoding="utf-8", newline="") as file:
                file.write(text)
            print(f"{name}: written")
    sys.exit(1 if stale else 0)


if __name__ == "__main__":
    main()
