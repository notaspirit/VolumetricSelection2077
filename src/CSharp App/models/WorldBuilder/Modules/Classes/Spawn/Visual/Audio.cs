namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Visual
{
    // Class for worldStaticSoundEmitterNode
    public class Audio : Visualized
    {
        public float Radius;
        public string EmitterMetadataName;
        public Audio() : base()
        {
            DataType = "Sounds";
            ModulePath = "visual/audio";
            Node = "worldStaticSoundEmitterNode";
            Radius = 5;
            EmitterMetadataName = string.Empty;
        }
    }
}
