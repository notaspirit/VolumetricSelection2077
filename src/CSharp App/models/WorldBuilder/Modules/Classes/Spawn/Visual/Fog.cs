namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Visual
{
    // Class for worldStaticFogVolumeNode
    public class Fog : Spawnable
    {
        public SharpDX.Vector3 Scale;
        public float[] Color;
        public float Absorption;
        public float BlendFalloff;
        public float DensityFactor;
        public float DensityFalloff;
        public Fog() : base()
        {
            DataType = "Fog";
            ModulePath = "visual/fog";
            Node = "worldStaticFogVolumeNode";
            Scale = new SharpDX.Vector3(1, 1, 1);
            Color = new float[4];
            Absorption = 0;
            BlendFalloff = 0;
            DensityFactor = 0;
            DensityFalloff = 0;
        }
    }
}
