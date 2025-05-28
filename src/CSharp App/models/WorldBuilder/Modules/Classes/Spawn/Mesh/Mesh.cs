using System.Collections.Generic;
using SharpDX;
using VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Generated;

namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Mesh
{
    // Class for worldMeshNode
    public class Mesh : Spawnable
    {
        public List<string> Apps;
        public int AppIndex;
        public Vector3 Scale;
        public int OccluderType;
        public bool HasOccluder;
        public bool WindImpulseEnabled;
        public int CastLocalShadows;
        public int CastRayTracedGlobalShadows;
        public int CastRayTracedLocalShadows;
        public int CastShadows;
        public object ShadowCastingModeEnum;
        public bool ShadowHeaderState;
        public float? MaxShadowPropertiesWidth;
        public BBoxStruct BBox;
        public bool BBoxLoaded;
        public int ColliderShape;
        public Mesh() : base()
        {
            DataType = "Mesh";
            ModulePath = "mesh/mesh";
            Node = "worldMeshNode";
            Apps = new List<string>();
            AppIndex = 0;
            Scale = new Vector3(1, 1, 1);
            OccluderType = 0;
            HasOccluder = false;
            WindImpulseEnabled = true;
            CastLocalShadows = 0;
            CastRayTracedGlobalShadows = 0;
            CastRayTracedLocalShadows = 0;
            CastShadows = 0;
            ShadowHeaderState = false;
            MaxShadowPropertiesWidth = null;
            BBox = new BBoxStruct { Min = new Vector4(-0.5f, -0.5f, -0.5f, 0), Max = new Vector4(0.5f, 0.5f, 0.5f, 0) };
            BBoxLoaded = false;
            ColliderShape = 0;
        }
    }
}
