# Usage: python minidump.py <file.dmp>
# Prints a crash dump's exception, registers and the game return addresses on the faulting thread's stack.
import struct, sys
d = open(sys.argv[1], 'rb').read()
sig, ver, nstreams, dir_rva = struct.unpack_from('<4sIII', d, 0)
streams = {}
for i in range(nstreams):
    t, size, rva = struct.unpack_from('<III', d, dir_rva + i * 12)
    streams[t] = (size, rva)
def mods():
    size, rva = streams[4]
    n, = struct.unpack_from('<I', d, rva)
    out = []
    for i in range(n):
        o = rva + 4 + i * 108
        base, sz = struct.unpack_from('<QI', d, o)
        name_rva, = struct.unpack_from('<I', d, o + 20)
        ln, = struct.unpack_from('<I', d, name_rva)
        name = d[name_rva + 4:name_rva + 4 + ln].decode('utf-16le')
        out.append((base, sz, name))
    return out
M = mods()
def where(a):
    for b, s, n in M:
        if b <= a < b + s: return f"{n.split(chr(92))[-1]}+0x{a-b:X}"
    return "?"
size, rva = streams[6]
tid, = struct.unpack_from('<I', d, rva)
code, flags, rec, addr, nparams = struct.unpack_from('<IIQQI', d, rva + 8)
params = struct.unpack_from('<15Q', d, rva + 8 + 32)
print(f"thread {tid} code 0x{code:08X} at 0x{addr:X} ({where(addr)}) params {[hex(p) for p in params[:nparams]]}")
ctx_size, ctx_rva = struct.unpack_from('<II', d, rva + 8 + 152)
# CONTEXT (x64): Rax at 0x78 .. R15, Rip at 0xF8
regs = ['Rax','Rcx','Rdx','Rbx','Rsp','Rbp','Rsi','Rdi','R8','R9','R10','R11','R12','R13','R14','R15']
vals = struct.unpack_from('<16Q', d, ctx_rva + 0x78)
print(' '.join(f"{r}={v:X}" for r, v in zip(regs, vals)))
rsp = vals[4]
# Scan the faulting thread's stack for return addresses into the game module.
size, rva = streams[3]
n, = struct.unpack_from('<I', d, rva)
for i in range(n):
    o = rva + 4 + i * 48
    t, = struct.unpack_from('<I', d, o)
    if t != tid: continue
    start, msize, mrva = struct.unpack_from('<QII', d, o + 24)
    off = rsp - start
    hits = []
    for p in range(max(off, 0), min(msize, off + 0x1000), 8):
        v, = struct.unpack_from('<Q', d, mrva + p)
        w = where(v)
        if w.startswith('granblue'): hits.append(w)
    print('stack:', hits[:20])
