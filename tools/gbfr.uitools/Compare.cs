#nullable enable

using GBFRDataTools.Files.UI;
using GBFRDataTools.Files.UI.Serialization;
using GBFRDataTools.Hashing;

// Walks an original and a rebuilt UI bulk file side by side and reports fields whose content differs.
// The original's bytes decide each field's shape.
public class Compare
{
    private readonly Probe _a;
    private readonly Probe _b;
    private readonly TextWriter _out;
    private readonly HashSet<(int, int)> _visited = [];

    private static readonly uint ComponentHash = XXHash32Custom.Hash("Component");

    public int Errors { get; private set; }
    public int Warnings { get; private set; }

    // Fields the rebuilt file has and the original leaves out.
    public SortedDictionary<string, int> Added { get; } = [];

    public Compare(byte[] original, byte[] rebuilt, TextWriter output)
    {
        _a = new Probe(original, TextWriter.Null);
        _b = new Probe(rebuilt, TextWriter.Null);
        _out = output;
    }

    public void Run()
    {
        _a.Run(); // learns component names
        Table(_a.I32(0), _b.I32(0), "", null);
    }

    private void Error(string path, string what)
    {
        Errors++;
        _out.WriteLine($"ERROR {path}: {what}");
    }

    private void Warn(string path, string what)
    {
        Warnings++;
        _out.WriteLine($"warn  {path}: {what}");
    }

    private static (uint[] hashes, int[] offsets) Fields(Probe p, int baseOfs)
    {
        int tableOfs = baseOfs + p.I32(baseOfs);
        int n = p.I32(tableOfs);
        return (Enumerable.Range(0, n).Select(i => p.U32(tableOfs + 4 + i * 4)).ToArray(),
                Enumerable.Range(0, n).Select(i => baseOfs + p.I32(baseOfs + 4 + i * 4)).ToArray());
    }

    private static string FieldName(uint hash, Dictionary<uint, UIPropertyTypeDef> declared) =>
        declared.TryGetValue(hash, out var d) ? d.PropertyInfo.Name
        : UIComponentSerializationCache.KnownHashNames.TryGetValue(hash, out string? n) && n.Length > 0 ? n
        : $"_{hash:X8}";

    private void Table(int a, int b, string path, string? componentName)
    {
        if (!_visited.Add((a, b)))
            return;

        var (ha, oa) = Fields(_a, a);
        var (hb, ob) = Fields(_b, b);
        var bIndex = hb.Select((h, i) => (h, i)).GroupBy(x => x.h).ToDictionary(g => g.Key, g => ob[g.First().i]);

        Dictionary<uint, UIPropertyTypeDef> declared = [];
        if (componentName is not null && UIComponentSerializationCache.GetOrRegisterType(componentName) is { } reflected)
            declared = reflected.Properties.ToDictionary(p => p.Hash);

        // Span of each original field: up to the next field offset in this table.
        int aTable = a + _a.I32(a);
        int[] sorted = oa.Append(aTable).Order().ToArray();

        foreach (uint h in hb.Except(ha))
        {
            string key = $"{componentName ?? "(table)"}.{FieldName(h, declared)}";
            Added[key] = Added.GetValueOrDefault(key) + 1;
        }

        string? childComponent = null;
        for (int i = 0; i < ha.Length; i++)
        {
            string name = FieldName(ha[i], declared);
            string fieldPath = path.Length == 0 ? name : $"{path}.{name}";
            if (!bIndex.TryGetValue(ha[i], out int fb))
            {
                Error(path, $"missing field {name}");
                continue;
            }
            int fa = oa[i];
            int span = sorted.FirstOrDefault(o => o > fa, _a.Data.Length) - fa;

            if (ha[i] == ComponentHash && componentName is null)
            {
                childComponent ??= _a.ComponentNameOf(a);
                if (childComponent is not null)
                    Table(fa, fb, $"{path}<{childComponent}>", childComponent);
                continue;
            }
            declared.TryGetValue(ha[i], out UIPropertyTypeDef? def);
            Value(fa, fb, span, fieldPath, def);
        }
    }

    private void Value(int a, int b, int span, string path, UIPropertyTypeDef? def)
    {
        if (_a.LooksLikeElementArray(a) && def is not { IsArray: false, Type: UIFieldType.Bool or UIFieldType.S8 })
        {
            int count = _a.I32(a);
            if (!_b.LooksLikeArray(b) || _b.I32(b) < count)
            {
                Error(path, $"array[{count}] became {Hex(_b, b, 8)}");
                return;
            }
            if (_b.I32(b) > count) // appended elements: compare the shared ones
                Warn(path, $"array[{count}] became array[{_b.I32(b)}]");
            for (int e = 0; e < count; e++)
            {
                int ea = a + _a.I32(a + 4 + e * 4);
                int eb = b + _b.I32(b + 4 + e * 4);
                string ep = $"{path}[{e}]";
                if (_a.LooksLikeTable(ea))
                {
                    if (!_b.LooksLikeTable(eb))
                        Error(ep, "table became non-table");
                    else
                        Table(ea, eb, ep, null);
                }
                else if (_a.LooksLikeString(ea, int.MaxValue))
                    Str(ea, eb, ep);
                else if (def is { Type: UIFieldType.ObjectRef } && !IsRef(_a, ea))
                    Error(ep, $"declared ObjectRef[], original element is {Hex(_a, ea, 8)}");
                else
                    Bytes(ea, eb, 8, 8, ep); // objref: hash + index + id
            }
            return;
        }

        if (def is { Type: UIFieldType.String } || (def is null && _a.LooksLikeString(a, span)))
        {
            Str(a, b, path);
            return;
        }

        if (def is { Type: UIFieldType.Object } || (def is null && span > 16 && _a.LooksLikeTable(a)))
        {
            if (_a.LooksLikeTable(a) && _b.LooksLikeTable(b))
                Table(a, b, path, null);
            else if (_a.LooksLikeTable(a) != _b.LooksLikeTable(b))
                Error(path, "table shape differs");
            return;
        }

        // A ref starts with a known component hash.
        if (def is { IsArray: false, Type: UIFieldType.ObjectRef } && !IsRef(_a, a))
        {
            Error(path, $"declared ObjectRef, original is {(_a.LooksLikeTable(a) ? "a table" : Hex(_a, a, 8))}");
            return;
        }

        int size = def?.Type switch
        {
            null => 1, // undeclared: may be a bool
            UIFieldType.Bool or UIFieldType.S8 => 1,
            UIFieldType.S16 => 2,
            UIFieldType.CVec2 or UIFieldType.ObjectRef => 8,
            UIFieldType.CVec3 => 12,
            UIFieldType.CVec4 => 16,
            _ => 4,
        };
        if (def is { IsArray: true })
            size = 4; // empty array in the original: only the count is known
        Bytes(a, b, size, Math.Max(size, def is null ? 4 : 8), path, def);
    }

    private void Str(int a, int b, string path)
    {
        int la = _a.I32(a);
        int lb = b + 4 <= _b.Data.Length ? _b.I32(b) : -1;
        bool same = la == lb && lb >= 0 && b + 4 + lb <= _b.Data.Length
            && _a.Data.AsSpan(a + 4, la).SequenceEqual(_b.Data.AsSpan(b + 4, lb));
        if (!same)
            Error(path, $"string \"{Text(_a, a)}\" became {Hex(_b, b, 8)}");
    }

    // The first 'size' bytes must match; a difference up to 'window' is a warning.
    private void Bytes(int a, int b, int size, int window, string path, UIPropertyTypeDef? def = null)
    {
        window = Math.Min(window, Math.Min(_a.Data.Length - a, _b.Data.Length - b));
        size = Math.Min(size, window);
        string type = def is null ? "undeclared" : $"{def.Type}{(def.IsArray ? "[]" : "")}";
        if (!_a.Data.AsSpan(a, size).SequenceEqual(_b.Data.AsSpan(b, size)))
            Error(path, $"{type} {Hex(_a, a, window)} became {Hex(_b, b, window)}");
        else if (!_a.Data.AsSpan(a, window).SequenceEqual(_b.Data.AsSpan(b, window)))
            Warn(path, $"{type} {Hex(_a, a, window)} became {Hex(_b, b, window)}");
    }

    // Hash 0x887AE0B0 (empty component name) refs a plain object.
    private static bool IsRef(Probe p, int ofs) =>
        ofs + 8 <= p.Data.Length && UIComponentSerializationCache.KnownHashNames.ContainsKey(p.U32(ofs));

    private static string Text(Probe p, int ofs) =>
        System.Text.Encoding.UTF8.GetString(p.Data, ofs + 4, Math.Max(0, p.I32(ofs)));

    private static string Hex(Probe p, int ofs, int len) =>
        Convert.ToHexString(p.Data, ofs, Math.Min(len, p.Data.Length - ofs));
}
