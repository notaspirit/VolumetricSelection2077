using SharpDX;

namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Meta
{
    // Class for worldReflectionProbeNode
    public class ReflectionProbe : Spawnable
    {
        public Vector3 Scale;
        public Vector3 EdgeScale;
        public bool Previewed;
        public object AmbientModes;
        public object NeighborModes;
        public int AmbientMode;
        public int NeighborMode;
        public float EmissiveScale;
        public float StreamingDistance;
        public float Priority;
        public bool AllInShadow;
        public float MaxPropertyWidth;
        public int Uk10;
        public int Uk11;
        public ReflectionProbe() : base()
        {
            DataType = "Reflection Probe";
            ModulePath = "meta/reflectionProbe";
            Node = "worldReflectionProbeNode";
            Description = "Places a reflection probe of variable size. Can be used to make indoors have appropriate base lighting.";
            Icon = "HomeLightbulbOutline";
            Scale = new Vector3(5, 5, 5);
            EdgeScale = new Vector3(0.5f, 0.5f, 0.5f);
            Previewed = true;
            AmbientModes = null;
            NeighborModes = null;
            AmbientMode = 2;
            NeighborMode = 3;
            EmissiveScale = 1;
            StreamingDistance = 50;
            Priority = 25;
            AllInShadow = false;
            MaxPropertyWidth = 0;
            Uk10 = 1056;
            Uk11 = 512;
        }
    }
}
