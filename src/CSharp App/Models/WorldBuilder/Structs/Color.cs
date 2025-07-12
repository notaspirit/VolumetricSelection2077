using WolvenKit.RED4.Types;

namespace VolumetricSelection2077.models.WorldBuilder.Structs;

public class Color
{
    public ushort r;
    public ushort g;
    public ushort b;

    public Color(ushort r, ushort g, ushort b)
    {
        this.r = r;
        this.g = g;
        this.b = b;
    }

    public Color()
    {
        r = 0;
        g = 0;
        b = 0;
    }

    public static implicit operator Color(CColor value) => new (value.Red, value.Green, value.Blue);
}