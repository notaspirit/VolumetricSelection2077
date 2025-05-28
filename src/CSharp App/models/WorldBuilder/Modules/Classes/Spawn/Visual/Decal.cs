using SharpDX;

namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Visual
{
    // Class for worldStaticDecalNode
    public class Decal : Spawnable
    {
        public float Alpha;
        public bool HorizontalFlip;
        public bool VerticalFlip;
        public float AutoHideDistance;
        public Vector3 Scale;
        public bool IsTiling;
        public float MaxPropertyWidth;
        public Decal() : base()
        {
            DataType = "Decals";
            ModulePath = "visual/decal";
            Node = "worldStaticDecalNode";
            Alpha = 1.0f;
            HorizontalFlip = false;
            VerticalFlip = false;
            AutoHideDistance = 0;
            Scale = new Vector3(1, 1, 1);
            IsTiling = false;
            MaxPropertyWidth = 0;
        }
    }
}
