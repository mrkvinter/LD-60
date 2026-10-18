"""Shrink itch-page.css into itch-page.min.css.

itch.io keeps only the first 16384 characters of a page's Custom CSS,
so the readable source doesn't fit. Run: python minify.py
"""
import re
from pathlib import Path

LIMIT = 16384
here = Path(__file__).parent
src = (here / "itch-page.css").read_text(encoding="utf-8")

STRING = r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''

# drop comments in one left-to-right pass, so quotes inside comments
# ("itch.io's") don't look like strings and "/*" inside strings stays
src = re.sub(r"/\*.*?\*/|(" + STRING + ")", lambda m: m.group(1) or "", src, flags=re.S)

# split into quoted strings (kept as is) and everything else
parts = re.split("(" + STRING + ")", src)
out = []
for i, part in enumerate(parts):
    if i % 2:
        out.append(part)
        continue
    part = re.sub(r"\s+", " ", part)
    # spaces around + and - stay (calc), and before ":" (descendant ::selection)
    part = re.sub(r"\s*([{};,>])\s*", r"\1", part)
    part = re.sub(r":\s+", ":", part)
    part = part.replace(";}", "}")
    out.append(part)

css = "".join(out).strip() + "\n"
(here / "itch-page.min.css").write_text(css, encoding="utf-8")

print(f"{len(css)} / {LIMIT} characters")
if len(css) > LIMIT:
    raise SystemExit("too long for itch.io, the end will be cut off")
