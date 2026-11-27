from pathlib import Path
import re,html
root=Path('Docs/Odbrana')
source=(root/'Govor_za_prezentaciju_15_minuta.md').read_text(encoding='utf-8')
def inline(s):
    s=html.escape(s)
    s=re.sub(r'\*\*(.+?)\*\*',r'<strong>\1</strong>',s)
    s=re.sub(r'\[([^\]]+)\]\(([^)]+)\)',r'<a href="\2">\1</a>',s)
    return s
out=[];para=[];table=False;listing=False;counter=0;toc=[]
def flush():
    if para:out.append('<p>'+inline(' '.join(para))+'</p>');para.clear()
for line in source.splitlines()+['']:
    if not line.startswith('|') and table:out.append('</tbody></table></div>');table=False
    if not line.startswith('- ') and listing:out.append('</ul>');listing=False
    if not line.strip():flush();continue
    if line.startswith('# '):flush();out.append('<h1>'+inline(line[2:])+'</h1>')
    elif line.startswith('## '):
        flush();counter+=1;title=line[3:];key='section'+str(counter);out.append(f'<h2 id="{key}">'+inline(title)+'</h2>')
        if re.match(r'\d+\.',title):toc.append(f'<a href="#{key}">'+inline(title)+'</a>')
    elif line.startswith('|'):
        flush()
        if re.match(r'^\|[-| :]+\|$',line):continue
        first=not table
        if first:out.append('<div class="table"><table><tbody>');table=True
        tag='th' if first else 'td';out.append('<tr>'+''.join(f'<{tag}>'+inline(x.strip())+f'</{tag}>' for x in line.strip('|').split('|'))+'</tr>')
    elif line.startswith('- '):
        flush()
        if not listing:out.append('<ul>');listing=True
        out.append('<li>'+inline(line[2:])+'</li>')
    elif line.startswith('[') and line.endswith(']'):flush();out.append('<aside>'+inline(line[1:-1])+'</aside>')
    else:para.append(line)
page='''<!doctype html><html lang="sr-Latn"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Govor za odbranu — 15 minuta</title><style>
*{box-sizing:border-box}body{margin:0;background:#f2f0f7;color:#222331;font:18px/1.75 Georgia,serif}header{background:#181624;color:#fff;padding:25px max(24px,calc((100vw - 920px)/2));font:16px/1.5 system-ui}header a{color:#d7c7ff}main{max-width:920px;margin:30px auto;padding:44px 54px;background:white}h1,h2,th,aside,nav{font-family:system-ui}h1{font-size:36px;line-height:1.25}h2{font-size:25px;line-height:1.35;border-top:2px solid #ddd3ef;padding-top:28px;margin-top:46px;scroll-margin-top:20px;color:#513187}p{margin:18px 0}a{color:#65449d}aside{padding:15px 20px;border-left:4px solid #9672c6;background:#f4f0fa;color:#604581;font-size:16px}nav{font-size:15px;padding:18px;background:#eeebf5}nav a{display:block;margin:5px 0}.table{overflow-x:auto}table{border-collapse:collapse;width:100%;font:15px/1.5 system-ui}th,td{text-align:left;padding:10px;border-bottom:1px solid #ddd}th{background:#eee8f7}button{font:inherit;padding:8px 16px;cursor:pointer;margin-top:12px}li{margin-bottom:12px}@media(max-width:650px){main{margin:0;padding:24px}h1{font-size:29px}}@media print{body{background:white;font-size:11pt;line-height:1.5}header,nav,button{display:none}main{margin:0;padding:0;max-width:none}h1{font-size:23pt}h2{font-size:15pt;break-after:avoid}aside{break-inside:avoid}table{font-size:9pt}a{color:black}@page{size:A4;margin:20mm}}
</style></head><body><header>Priprema za usmenu odbranu · Mateja Nikolić<br><a href="Vodic_za_odbranu.html">Pitanja i odgovori za Unity</a> · <a href="Prezentacija/Wizards_Fight_Prezentacija_Zakljucak.pptx">PowerPoint prezentacija</a><br><button onclick="window.print()">Štampaj / sačuvaj kao PDF</button></header><main>'''
page+='<nav><strong>Pređi na slajd</strong>'+''.join(toc)+'</nav>'+''.join(out)+'</main></body></html>'
(root/'Govor_za_prezentaciju_15_minuta.html').write_text(page,encoding='utf-8')
assert len(toc)==15
for href in re.findall(r'href="([^"#]+)"',page):assert (root/href).exists(),href
print('Created speech HTML. 15 slide sections; all local links verified.')


