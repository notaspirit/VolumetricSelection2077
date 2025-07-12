using VolumetricSelection2077.Models.WorldBuilder.Spawn;

namespace VolumetricSelection2077.models.WorldBuilder.Spawn.Visual;

public class Effect : Visualized
{
    public Effect()
    {
        DataType = "Effects";
        ModulePath = "visual/effect";
        NodeType = "worldEffectNode";
    }
}