#nullable enable

using System.Text;

using GBFRDataTools.Files.UI;
using GBFRDataTools.Files.UI.Serialization;
using GBFRDataTools.Hashing;

// Walks a UI bulk file (.prfb/.viewb/...) without class definitions and prints every table field
// with its hash, known name, byte span and a type guess. Fields a component's class lacks are marked '?'.
public class Probe
{
    private readonly byte[] _data;
    internal byte[] Data => _data;
    private readonly TextWriter _out;

    private static readonly uint ComponentNameHash = XXHash32Custom.Hash("ComponentName");
    private static readonly uint ComponentHash = XXHash32Custom.Hash("Component");

    // Component type -> unknown field hash -> observed kinds. Null field map = type unsupported.
    public Dictionary<string, Dictionary<uint, HashSet<string>>?> Survey { get; } = [];

    // "Type.Field: declared X, observed Y" for declared fields whose bytes don't fit the declared type.
    public HashSet<string> Mismatches { get; } = [];

    private static bool Fits(UIPropertyTypeDef def, string observed)
    {
        bool isArray = observed.StartsWith("array[");
        bool nonEmptyArray = isArray && !observed.StartsWith("array[0]");
        if (def.IsArray)
            return isArray || observed.StartsWith("4b?"); // count 0 can read as a scalar
        return def.Type switch
        {
            UIFieldType.ObjectRef => !nonEmptyArray && observed != "string" && observed != "table",
            UIFieldType.String => observed == "string" || observed.StartsWith("array[0]") || observed.Contains("b?"),
            UIFieldType.Object => observed == "table",
            _ => !nonEmptyArray && observed != "string" && observed != "table" && !observed.StartsWith("objref(<object>"),
        };
    }

    public Probe(byte[] data, TextWriter output)
    {
        _data = data;
        _out = output;
    }

    public void Run()
    {
        int root = I32(0);
        DumpTable(root, 0, null);
    }

    // Short type guess for the survey.
    private string Kind(int ofs, int span)
    {
        if (LooksLikeElementArray(ofs))
        {
            int count = I32(ofs);
            if (count == 0)
                return "array[0]";
            int elem = ofs + I32(ofs + 4);
            if (UIComponentSerializationCache.KnownHashNames.TryGetValue(U32(elem), out string? elemTarget))
                return $"array[{count}] of objref({(elemTarget.Length > 0 ? elemTarget : "<object>")})";
            if (LooksLikeTable(elem))
                return $"array[{count}] of table";
            if (LooksLikeString(elem, int.MaxValue))
                return $"array[{count}] of string";
            return $"array[{count}] of ? {Convert.ToHexString(_data, elem, Math.Min(8, _data.Length - elem))}";
        }
        if (LooksLikeString(ofs, span))
            return "string";
        if (span >= 8 && UIComponentSerializationCache.KnownHashNames.TryGetValue(U32(ofs), out string? target))
            return $"objref({(target.Length > 0 ? target : "<object>")})";
        if (LooksLikeTable(ofs))
            return "table";
        return span switch
        {
            1 => $"1b {_data[ofs]}",
            2 => $"2b {BitConverter.ToInt16(_data, ofs)}",
            _ => $"{span}b? i32={I32(ofs)} f32={BitConverter.ToSingle(_data, ofs):G6} hex={Convert.ToHexString(_data, ofs, Math.Min(span, 16))}",
        };
    }

    private readonly HashSet<(int, string?)> _visited = [];

    private void DumpTable(int baseOfs, int depth, string? componentName)
    {
        string pad = new(' ', depth * 2);

        // Tables can be shared, and a misread value can point back up; dump each once.
        if (!_visited.Add((baseOfs, componentName)) || depth > 40)
        {
            _out.WriteLine($"{pad}(table @0x{baseOfs:X5} already dumped)");
            return;
        }
        int tableOfs = baseOfs + I32(baseOfs);
        int n = I32(tableOfs);
        uint[] hashes = Enumerable.Range(0, n).Select(i => U32(tableOfs + 4 + i * 4)).ToArray();
        int[] offsets = Enumerable.Range(0, n).Select(i => baseOfs + I32(baseOfs + 4 + i * 4)).ToArray();

        HashSet<uint>? knownHashes = null;
        Dictionary<uint, UIPropertyTypeDef> declared = [];
        if (componentName is not null)
        {
            var reflected = UIComponentSerializationCache.GetOrRegisterType(componentName);
            knownHashes = reflected?.Properties.Select(p => p.Hash).ToHashSet() ?? [];
            if (reflected is not null)
                declared = reflected.Properties.ToDictionary(p => p.Hash);
            if (!Survey.ContainsKey(componentName))
                Survey[componentName] = reflected is null ? null : [];
        }

        // A field's span ends at the next offset in this table, or at the table header.
        int[] sorted = offsets.Append(tableOfs).Order().ToArray();

        for (int i = 0; i < n; i++)
        {
            int ofs = offsets[i];
            int end = sorted.FirstOrDefault(o => o > ofs, _data.Length);
            int span = end - ofs;

            string name = UIComponentSerializationCache.KnownHashNames.TryGetValue(hashes[i], out string? known) ? known : "";
            string mark = knownHashes is not null && !knownHashes.Contains(hashes[i]) ? "?" : " ";
            if (componentName is not null && mark == "?")
            {
                var fields = Survey[componentName] ??= [];
                if (!fields.TryGetValue(hashes[i], out var kinds))
                    fields[hashes[i]] = kinds = [];
                kinds.Add(Kind(ofs, span));
            }
            else if (declared.TryGetValue(hashes[i], out UIPropertyTypeDef? def))
            {
                string observed = Kind(ofs, span);
                if (!Fits(def, observed))
                    Mismatches.Add($"{componentName}.{def.PropertyInfo.Name}: declared {def.Type}{(def.IsArray ? "[]" : "")}, observed {observed}");
            }
            _out.Write($"{pad}{mark} 0x{hashes[i]:X8} {name,-24} @0x{ofs:X5} span={span,-5} ");

            if (hashes[i] == ComponentHash && componentName is null)
            {
                _out.WriteLine("(component, see ComponentName)");
                continue;
            }

            DescribeValue(ofs, span, depth, hashes, offsets);
        }
    }

    private void DescribeValue(int ofs, int span, int depth, uint[] siblingHashes, int[] siblingOffsets)
    {
        // Arrays first: a small array also fits a scalar or a one-char string.
        if (LooksLikeElementArray(ofs))
        {
            int count = I32(ofs);
            _out.WriteLine($"array[{count}]");
            for (int e = 0; e < count; e++)
            {
                int elem = ofs + I32(ofs + 4 + e * 4);
                string pad = new(' ', (depth + 1) * 2);
                if (LooksLikeTable(elem))
                {
                    string? comp = ComponentNameOf(elem);
                    _out.WriteLine($"{pad}[{e}] table{(comp is null ? "" : $" component={comp}")}");
                    DumpTable(elem, depth + 2, null);
                    if (comp is not null)
                    {
                        _out.WriteLine($"{pad}  component {comp}:");
                        DumpTable(ComponentTableOf(elem), depth + 3, comp);
                    }
                }
                else if (LooksLikeString(elem, int.MaxValue))
                    _out.WriteLine($"{pad}[{e}] string \"{Encoding.UTF8.GetString(_data, elem + 4, I32(elem))}\"");
                else
                    _out.WriteLine($"{pad}[{e}] {Scalar(elem, 8)}");
            }
            return;
        }

        if (LooksLikeString(ofs, span))
        {
            _out.WriteLine($"string \"{Encoding.UTF8.GetString(_data, ofs + 4, I32(ofs))}\"");
            return;
        }

        if (span <= 16 && !LooksLikeTable(ofs))
        {
            _out.WriteLine(Scalar(ofs, span));
            return;
        }

        if (LooksLikeTable(ofs))
        {
            _out.WriteLine("table");
            DumpTable(ofs, depth + 1, null);
            return;
        }

        _out.WriteLine(Scalar(ofs, Math.Min(span, 16)));
    }

    internal string? ComponentNameOf(int tableBase)
    {
        int tableOfs = tableBase + I32(tableBase);
        int n = I32(tableOfs);
        for (int i = 0; i < n; i++)
        {
            if (U32(tableOfs + 4 + i * 4) == ComponentNameHash)
            {
                int strOfs = tableBase + I32(tableBase + 4 + i * 4);
                string name = Encoding.UTF8.GetString(_data, strOfs + 4, I32(strOfs));
                UIComponentSerializationCache.KnownHashNames.TryAdd(XXHash32Custom.Hash(name), name);
                return name;
            }
        }
        return null;
    }

    internal int ComponentTableOf(int tableBase)
    {
        int tableOfs = tableBase + I32(tableBase);
        int n = I32(tableOfs);
        for (int i = 0; i < n; i++)
        {
            if (U32(tableOfs + 4 + i * 4) == ComponentHash)
                return tableBase + I32(tableBase + 4 + i * 4);
        }
        throw new InvalidDataException("Component entry without Component field");
    }

    internal bool LooksLikeTable(int ofs)
    {
        if (ofs + 4 > _data.Length)
            return false;
        // Offsets are relative and may point backwards.
        int tableOfs = ofs + I32(ofs);
        if (tableOfs == ofs || tableOfs < 0 || tableOfs + 4 > _data.Length)
            return false;
        int n = I32(tableOfs);
        if (n <= 0 || n > 256 || tableOfs + 4 + n * 4 > _data.Length || ofs + 4 + n * 4 > _data.Length)
            return false;
        for (int i = 0; i < n; i++)
        {
            int field = ofs + I32(ofs + 4 + i * 4);
            if (field < 0 || field >= _data.Length)
                return false;
        }
        return true;
    }

    internal bool LooksLikeString(int ofs, int span)
    {
        if (ofs + 4 > _data.Length)
            return false;
        int len = I32(ofs);
        if (len <= 0 || len > 512 || ofs + 4 + len > _data.Length || 4 + len > span)
            return false;
        for (int i = 0; i < len; i++)
        {
            byte b = _data[ofs + 4 + i];
            if (b < 0x20 || b > 0x7E)
                return false;
        }
        return true;
    }

    internal bool LooksLikeArray(int ofs)
    {
        if (ofs + 4 > _data.Length)
            return false;
        int count = I32(ofs);
        if (count < 0 || count > 4096 || ofs + 4 + count * 4 > _data.Length)
            return false;
        // Element offsets are relative to the array start, may point backwards, never into the offset list.
        for (int i = 0; i < count; i++)
        {
            int elem = ofs + I32(ofs + 4 + i * 4);
            if (elem < 0 || elem >= _data.Length || (elem >= ofs && elem < ofs + 4 + count * 4))
                return false;
        }
        return true;
    }

    // An array whose first element is a table, a known hash (objref) or a string.
    internal bool LooksLikeElementArray(int ofs)
    {
        if (!LooksLikeArray(ofs))
            return false;
        if (I32(ofs) == 0)
            return true;
        int elem = ofs + I32(ofs + 4);
        return LooksLikeTable(elem)
            || (elem + 4 <= _data.Length && UIComponentSerializationCache.KnownHashNames.ContainsKey(U32(elem)))
            || LooksLikeString(elem, int.MaxValue);
    }

    private string Scalar(int ofs, int span)
    {
        int len = Math.Clamp(span, 1, Math.Min(16, _data.Length - ofs));
        string hex = Convert.ToHexString(_data, ofs, len);
        var sb = new StringBuilder($"bytes={hex}");
        if (len >= 4)
        {
            uint u = U32(ofs);
            sb.Append($" i32={I32(ofs)} f32={BitConverter.ToSingle(_data, ofs):G6}");
            if (UIComponentSerializationCache.KnownHashNames.TryGetValue(u, out string? hn))
                sb.Append($" hash={hn}");
        }
        if (len >= 8)
            sb.Append($" objref?(idx={BitConverter.ToInt16(_data, ofs + 4)},id={BitConverter.ToInt16(_data, ofs + 6)})");
        return sb.ToString();
    }

    internal int I32(int ofs) => BitConverter.ToInt32(_data, ofs);
    internal uint U32(int ofs) => BitConverter.ToUInt32(_data, ofs);
}
