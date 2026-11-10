using VolumetricSelection2077.App.Models.WorldBuilder.Spawn;

namespace VolumetricSelection2077.App.models.WorldBuilder.Spawn.Visual;

public class Effect : Visualized
{
    public Effect()
    {
        DataType = "Effects";
        ModulePath = "visual/effect";
        NodeType = "worldEffectNode";
    }
}