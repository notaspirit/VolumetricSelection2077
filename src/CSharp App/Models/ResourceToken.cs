using VolumetricSelection2077.App.Enums;

namespace VolumetricSelection2077.App.Models;

public struct ResourceToken
{
    public ResourceTokenResult Result { get; set; }
    public object? Resource { get; set; }
}