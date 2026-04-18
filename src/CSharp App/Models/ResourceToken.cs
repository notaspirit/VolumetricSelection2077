using VolumetricSelection2077.Enums;

namespace VolumetricSelection2077.Models;

public struct ResourceToken
{
    public ResourceTokenResult Result { get; set; }
    public object? Resource { get; set; }
}