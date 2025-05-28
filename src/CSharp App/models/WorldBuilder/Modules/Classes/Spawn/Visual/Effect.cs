namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Visual
{
    // Class for worldEffectNode
    public class Effect : Visualized
    {
        public int DisableCron;
        public Effect() : base()
        {
            DataType = "Effects";
            ModulePath = "visual/effect";
            Node = "worldEffectNode";
            DisableCron = 0;
        }
    }
}
