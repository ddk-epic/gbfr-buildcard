using System.Text.Json.Serialization;

namespace gbfr.qol.buildcard.Model;

// BuildCard's JSON metadata, generated at build time; non-ASCII text is escaped.
[JsonSerializable(typeof(BuildCard))]
public partial class BuildCardJson : JsonSerializerContext
{
}
