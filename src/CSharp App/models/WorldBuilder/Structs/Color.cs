using WolvenKit.RED4.Types;

namespace VolumetricSelection2077.models.WorldBuilder.Structs;

public class Color
{
    public float r;
    public float g;
    public float b;

    public Color(float r, float g, float b)
    {
        this.r = r;
        this.g = g;
        this.b = b;
    }

    public Color()
    {
        r = 1;
        g = 1;
        b = 1;
    }

    public static implicit operator Color(CColor value) => new (value.Red, value.Green, value.Blue);
}