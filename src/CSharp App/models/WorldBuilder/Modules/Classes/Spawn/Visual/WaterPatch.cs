namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Visual
{
    // Class for worldWaterPatchNode
    public class WaterPatch : Mesh.Mesh
    {
        public float Depth;
        public bool HideGenerate;
        public WaterPatch() : base()
        {
            DataType = "Water Patch";
            ModulePath = "visual/waterPatch";
            Node = "worldWaterPatchNode";
            Depth = 2;
            HideGenerate = true;
        }
    }
}
