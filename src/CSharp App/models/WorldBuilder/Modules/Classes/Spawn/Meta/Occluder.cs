namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Meta
{
    // Class for worldStaticOccluderMeshNode
    public class Occluder : Spawnable
    {
        public SharpDX.Vector3 Scale;
        public int OccluderType;
        public int OccluderMesh;
        public bool Previewed;
        public object OccluderTypes;
        public float MaxPropertyWidth;
        public Occluder() : base()
        {
            DataType = "Occluder";
            ModulePath = "meta/occluder";
            Node = "worldStaticOccluderMeshNode";
            Scale = new SharpDX.Vector3(1, 1, 1);
            OccluderType = 0;
            OccluderMesh = 0;
            Previewed = false;
            MaxPropertyWidth = 0;
        }
    }
}
