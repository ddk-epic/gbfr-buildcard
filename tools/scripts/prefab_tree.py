# Usage: python prefab_tree.py <file.prfb.yaml> [maxdepth]
# Prints a prefab YAML's object tree.
import sys,re
objs={}; order=[]; cur=None; sect=None
for line in open(sys.argv[1],encoding='utf-8'):
    line=line.rstrip('\r\n')
    m=re.match(r'^- Id: (\d+)$',line)
    if m:
        cur={'Id':int(m.group(1)),'Children':[],'Comps':[]}; objs[cur['Id']]=cur; order.append(cur['Id']); sect=None; continue
    if cur is None: continue
    m=re.match(r'^  (\w+):\s*(.*)$',line)
    if m:
        k,v=m.groups(); sect=k
        if v: cur[k]=v
        continue
    m=re.match(r'^  - (\d+)$',line)
    if m and sect=='Children': cur['Children'].append(int(m.group(1))); continue
    m=re.match(r'^  - ComponentName: (\w+)$',line)
    if m and sect=='Components': cur['Comps'].append(m.group(1))
maxdepth=int(sys.argv[2]) if len(sys.argv)>2 else 99
def walk(i,dep):
    o=objs[i]
    if dep>maxdepth: return
    print(f"{'  '*dep}{i} {o['Name']} [{','.join(o['Comps'])}] act={o['Active']} pos={o['Position']} size={o['SizeDelta']} a={o['AnchorMin']}/{o['AnchorMax']} piv={o['Pivot']} sc={o['Scale']}")
    for c in o['Children']: walk(c,dep+1)
walk(order[0],0)
