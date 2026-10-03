using System.Numerics;

using GBFRDataTools.Files.UI.Assets;
using GBFRDataTools.Files.UI.Components;

namespace gbfr.uitools.Components.Core;

// ui::component::Mask
// Replaces GBFRDataTools' class: Sprite is a sprite table, not a ref.
public class Mask : Component
{
    public SpriteRef Sprite { get; set; }
    public Vector2 Offset { get; set; }
    public Vector4 ChannelWeights { get; set; }
    public bool InvertMask { get; set; }
    public bool InvertOutsides { get; set; }
}
