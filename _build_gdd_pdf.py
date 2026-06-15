import markdown, pathlib
SRC = pathlib.Path(r"D:\Work\naija-empires\Naija-Empires-GDD.md")
OUT = SRC.with_name("_tmp_gdd.html")
html_body = markdown.markdown(SRC.read_text(encoding="utf-8"),
    extensions=["tables","fenced_code","toc","sane_lists","attr_list"])
CSS = """
@page { size: A4; margin: 18mm 15mm 20mm 15mm; }
* { box-sizing: border-box; }
body { font-family:"Segoe UI","Helvetica Neue",Arial,sans-serif; font-size:10.4pt; line-height:1.5; color:#1f2620; margin:0; -webkit-print-color-adjust:exact; print-color-adjust:exact; }
h1 { font-size:21pt; color:#0b5e2f; margin:0 0 4pt; line-height:1.2; border-bottom:3px solid #c79b1a; padding-bottom:8pt; }
h2 { font-size:13.5pt; color:#0b5e2f; margin:18pt 0 6pt; border-left:5px solid #c79b1a; padding-left:9pt; page-break-after:avoid; }
h3 { font-size:11pt; color:#0e7a3d; margin:11pt 0 4pt; page-break-after:avoid; }
p { margin:5pt 0; }
strong { color:#0b3d22; }
a { color:#0e7a3d; text-decoration:none; }
ul, ol { margin:5pt 0; padding-left:20pt; }
li { margin:2.5pt 0; }
hr { border:none; border-top:1px solid #d8e2da; margin:14pt 0; }
table { border-collapse:collapse; width:100%; margin:8pt 0; font-size:9.3pt; page-break-inside:avoid; }
th { background:#0b5e2f; color:#fff; text-align:left; padding:6pt 8pt; border:1px solid #0b5e2f; font-weight:600; }
td { padding:5pt 8pt; border:1px solid #cfded4; vertical-align:top; }
tr:nth-child(even) td { background:#f3f9f5; }
code { font-family:"Cascadia Code","Consolas",monospace; font-size:9pt; color:#0b3d22; }
pre { background:#0b2e1c; color:#e8f3ec; padding:10pt; border-radius:6px; font-size:8.6pt; line-height:1.45; page-break-inside:avoid; overflow:hidden; }
pre code { color:inherit; }
blockquote { border-left:4px solid #c79b1a; background:#fdf8e8; margin:8pt 0; padding:6pt 12pt; color:#5a4a1a; font-weight:600; }
.brandbar { display:flex; justify-content:space-between; align-items:center; font-size:8pt; color:#5d6b62; letter-spacing:.3px; border-bottom:1px solid #d8e2da; padding-bottom:6pt; margin-bottom:14pt; text-transform:uppercase; }
.brandbar b { color:#0b5e2f; }
"""
OUT.write_text(f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Naija Empires — GDD</title><style>{CSS}</style></head><body><div class="brandbar"><span><b>Naija Empires</b> &nbsp;·&nbsp; Game Design Document</span><span>v0.1 · Confidential</span></div>{html_body}</body></html>""", encoding="utf-8")
print(str(OUT))
