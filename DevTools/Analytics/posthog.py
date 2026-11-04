#!/usr/bin/env python3
"""PostHog CLI: HogQL queries and the game dashboards kept as .sql files in dashboards/.

Auth: personal API key, never stored in the repo. Taken from KVINTER_POSTHOG_KEY, else read
from ~/.config/kvinter/posthog-key. The key needs access to the LD-60 project and
Project read, Query read, Insight write and Dashboard write.

Usage:
  posthog.py query "<hogql>"             run a query, print TSV
  posthog.py query <file.sql>            run a dashboard file with its placeholders filled in
  posthog.py query -                     query from stdin
  posthog.py render <file.sql>           print the query as sent, e.g. for the PostHog SQL editor
  posthog.py check [--source editor]     run every dashboard query, print row counts
  posthog.py sync                        create or update the dashboards in PostHog
  posthog.py url                         print dashboard links

Layout, one folder per dashboard, one file per insight:
  dashboards/<slug>/_dashboard.md        "# Name" and the description
  dashboards/<slug>/_<name>.sql          shared fragment, inlined wherever {<name>} appears
  dashboards/<slug>/NN_<insight>.sql     "-- name:", "-- description:", optional "-- chart:" header
{build} becomes the source = 'build' filter; {filters} is left to PostHog, which fills in the
dashboard's date range and property filters.

Standard library only.
"""
import json, os, pathlib, re, sys, urllib.error, urllib.request

HOST = "https://eu.posthog.com"
PROJECT = int(os.environ.get("KVINTER_POSTHOG_PROJECT", "311321"))
KEY_ENV = "KVINTER_POSTHOG_KEY"
DEFAULT_KEY = pathlib.Path.home() / ".config" / "kvinter" / "posthog-key"
DASHBOARDS = pathlib.Path(__file__).resolve().parent / "dashboards"
CHARTS = {"bar": "ActionsBar", "line": "ActionsLineGraph", "area": "ActionsAreaGraph"}


def key():
    k = os.environ.get(KEY_ENV)
    if not k:
        if not DEFAULT_KEY.exists():
            sys.exit("No PostHog key: set %s or put it in %s, see DevTools/Analytics/README.md." % (KEY_ENV, DEFAULT_KEY))
        k = DEFAULT_KEY.read_text()
    return k.strip()


def call(method, path, body=None):
    url = "%s/api/projects/%s/%s" % (HOST, PROJECT, path)
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method, headers={
        "Authorization": "Bearer " + key(), "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            text = r.read().decode()
    except urllib.error.HTTPError as e:
        sys.exit("HTTP %s %s %s\n%s" % (e.code, method, path, e.read().decode()[:2000]))
    return json.loads(text) if text else {}


def query(sql):
    r = call("POST", "query/", {"query": {"kind": "HogQLQuery", "query": sql}})
    return r["columns"], r["results"]


def cell(v):
    return json.dumps(v, ensure_ascii=False) if isinstance(v, (list, dict)) else "" if v is None else str(v)


def print_tsv(columns, rows):
    print("\t".join(columns))
    for row in rows:
        print("\t".join(cell(v) for v in row))


def split_header(path):
    """Leading "-- key: value" lines (indented "--" lines continue the previous key) and the body."""
    lines = path.read_text(encoding="utf-8").splitlines()
    meta, last, i = {}, None, 0
    while i < len(lines) and lines[i].startswith("--"):
        text = lines[i][2:]
        m = re.match(r" (\w+):\s*(.*)", text)
        if m:
            last = m.group(1)
            meta[last] = m.group(2).strip()
        elif last and text.strip():
            meta[last] += " " + text.strip()
        i += 1
    return meta, "\n".join(lines[i:]).strip()


def render(sql, folder, source="build"):
    def fill(m):
        name = m.group(1)
        if name == "filters":
            return m.group(0)
        if name == "build":
            return "properties.source = '%s'" % source
        fragment = folder / ("_%s.sql" % name)
        if not fragment.exists():
            sys.exit("Unknown placeholder {%s}: no %s" % (name, fragment))
        return render(split_header(fragment)[1], folder, source)
    return re.sub(r"\{(\w+)\}", fill, sql)


def load_dashboards():
    result = []
    for folder in sorted(p for p in DASHBOARDS.iterdir() if (p / "_dashboard.md").exists()):
        head, _, rest = (folder / "_dashboard.md").read_text(encoding="utf-8").partition("\n")
        insights = []
        for f in sorted(folder.glob("[!_]*.sql")):
            meta, body = split_header(f)
            if "name" not in meta:
                sys.exit("%s: no '-- name:' header" % f)
            insights.append(dict(meta, file=f, sql=body))
        result.append({"name": head.lstrip("# ").strip(), "description": " ".join(rest.split()),
                       "folder": folder, "insights": insights})
    return result


def insight_query(ins, folder):
    node = {"kind": "DataVisualizationNode", "source": {"kind": "HogQLQuery", "query": render(ins["sql"], folder)}}
    if "chart" in ins:
        kind, *axes = ins["chart"].split()
        axes = dict(a.split("=", 1) for a in axes)
        node["display"] = CHARTS[kind]
        node["chartSettings"] = {"xAxis": {"column": axes["x"]},
                                 "yAxis": [{"column": y} for y in axes["y"].split(",")]}
    return node


def live_dashboards():
    return [d for d in call("GET", "dashboards/?limit=200")["results"] if not d.get("deleted")]


def sync():
    live = {d["name"]: d for d in live_dashboards()}
    for dash in load_dashboards():
        d = live.get(dash["name"]) or call("POST", "dashboards/", {"name": dash["name"]})
        call("PATCH", "dashboards/%s/" % d["id"], {"description": dash["description"]})
        tiles = call("GET", "dashboards/%s/" % d["id"]).get("tiles", [])
        existing = {t["insight"]["name"]: t["insight"]["id"] for t in tiles if t.get("insight")}
        for ins in dash["insights"]:
            body = {"name": ins["name"], "description": ins.get("description", ""),
                    "query": insight_query(ins, dash["folder"])}
            if ins["name"] in existing:
                call("PATCH", "insights/%s/" % existing[ins["name"]], body)
                print("updated  %s" % ins["name"])
            else:
                call("POST", "insights/", dict(body, dashboards=[d["id"]]))
                print("created  %s" % ins["name"])
        wanted = {ins["name"] for ins in dash["insights"]}
        for name, iid in existing.items():
            if name not in wanted:
                call("PATCH", "insights/%s/" % iid, {"deleted": True})
                print("removed  %s" % name)
        arrange(d["id"], [ins["name"] for ins in dash["insights"]])
        print("%s/project/%s/dashboard/%s" % (HOST, PROJECT, d["id"]))


def arrange(dashboard_id, order):
    """Two columns in file order; PostHog itself puts every new insight on top."""
    tiles = call("GET", "dashboards/%s/" % dashboard_id).get("tiles", [])
    by_name = {t["insight"]["name"]: t for t in tiles if t.get("insight") and not t["insight"].get("deleted")}
    layout = []
    for i, name in enumerate(n for n in order if n in by_name):
        tile = by_name[name]
        cell = {"x": (i % 2) * 6, "y": (i // 2) * 5, "w": 6, "h": 5}
        layout.append({"id": tile["id"], "layouts": {"sm": dict(cell, i=str(tile["id"])), "xs": dict(cell, x=0, w=1, i=str(tile["id"]))}})
    call("PATCH", "dashboards/%s/" % dashboard_id, {"tiles": layout})


def check(source):
    for dash in load_dashboards():
        for ins in dash["insights"]:
            columns, rows = query(render(ins["sql"], dash["folder"], source))
            print("%4d rows  %s" % (len(rows), ins["file"].relative_to(DASHBOARDS)))
            if rows:
                print_tsv(columns, rows[:5])
            print()


def sql_file(arg):
    path = pathlib.Path(arg)
    if path.suffix != ".sql" or not path.exists():
        return None
    return render(split_header(path)[1], path.resolve().parent)


def main():
    a = sys.argv[1:]
    if not a or a[0] in ("-h", "--help"):
        sys.exit(__doc__)
    cmd = a[0]
    if cmd == "query":
        arg = a[1] if len(a) > 1 else "-"
        sql = sys.stdin.read() if arg == "-" else sql_file(arg) or arg
        print_tsv(*query(sql))
    elif cmd == "render":
        print(sql_file(a[1]) or sys.exit("Not a .sql file: %s" % a[1]))
    elif cmd == "check":
        check(a[a.index("--source") + 1] if "--source" in a else "build")
    elif cmd == "sync":
        sync()
    elif cmd == "url":
        for d in live_dashboards():
            print("%s\t%s/project/%s/dashboard/%s" % (d["name"], HOST, PROJECT, d["id"]))
    else:
        sys.exit("Unknown command %r\n%s" % (cmd, __doc__))


if __name__ == "__main__":
    main()
