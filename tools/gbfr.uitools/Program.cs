#nullable enable

using System.Reflection;

using GBFRDataTools.Files.UI.Serialization;

// Adds our UI component classes to GBFRDataTools' type table, replacing same-named ones, then runs its CLI.

var allUiTypes = (Dictionary<string, Type>)typeof(UIComponentSerializationCache)
    .GetField("_allUiTypes", BindingFlags.NonPublic | BindingFlags.Static)!
    .GetValue(null)!;

foreach (Type type in typeof(Program).Assembly.GetTypes())
{
    if (type.Namespace?.StartsWith("gbfr.uitools.Components") == true)
    {
        allUiTypes[type.Name] = type;
        UIComponentSerializationCache.GetOrRegisterType(type.Name);
    }
}

// probe <input> [output]: field dump for writing component classes.
if (args.Length >= 2 && args[0] == "probe")
{
    using TextWriter output = args.Length >= 3 ? File.CreateText(args[2]) : Console.Out;
    new Probe(File.ReadAllBytes(args[1]), output).Run();
    return;
}

// survey <output> <inputs...>: per component type, support status and unknown fields with type guesses.
if (args.Length >= 3 && args[0] == "survey")
{
    string[] inputs = args[2..];
    foreach (string input in inputs) // First pass learns component names for objref guesses.
        new Probe(File.ReadAllBytes(input), TextWriter.Null).Run();

    var merged = new SortedDictionary<string, Dictionary<uint, HashSet<string>>?>();
    var mismatches = new SortedSet<string>();
    foreach (string input in inputs)
    {
        var probe = new Probe(File.ReadAllBytes(input), TextWriter.Null);
        probe.Run();
        mismatches.UnionWith(probe.Mismatches);
        foreach (var (type, fields) in probe.Survey)
        {
            if (!merged.TryGetValue(type, out var into))
                merged[type] = fields is null ? null : new(fields);
            else if (fields is not null)
            {
                into ??= merged[type] = [];
                foreach (var (hash, kinds) in fields)
                {
                    if (!into.TryGetValue(hash, out var set))
                        into[hash] = set = [];
                    set.UnionWith(kinds);
                }
            }
        }
    }

    using var writer = File.CreateText(args[1]);
    foreach (var (type, fields) in merged)
    {
        bool supported = UIComponentSerializationCache.GetOrRegisterType(type) is not null;
        if (supported && fields is { Count: 0 })
            continue;
        writer.WriteLine($"{type} ({(supported ? "incomplete" : "unsupported")})");
        foreach (var (hash, kinds) in fields ?? [])
        {
            string name = UIComponentSerializationCache.KnownHashNames.TryGetValue(hash, out string? n) ? n : "";
            writer.WriteLine($"  0x{hash:X8} {name,-24} {string.Join(" | ", kinds)}");
        }
    }

    writer.WriteLine($"Type mismatches ({mismatches.Count})");
    foreach (string mismatch in mismatches)
        writer.WriteLine($"  {mismatch}");
    return;
}

// compare <original> <rebuilt>: fields whose content differs between the game's file and ours.
if (args.Length >= 3 && args[0] == "compare")
{
    var compare = new Compare(File.ReadAllBytes(args[1]), File.ReadAllBytes(args[2]), Console.Out);
    compare.Run();
    foreach (var (field, count) in compare.Added)
        Console.WriteLine($"added {field} x{count}");
    Console.WriteLine($"{compare.Errors} error(s), {compare.Warnings} warning(s), {compare.Added.Count} added field kind(s)");
    return;
}

MethodInfo main = Type.GetType("GBFRDataTools.Program, GBFRDataTools", throwOnError: true)!
    .GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static)!;
main.Invoke(null, [args]);
