using System.Collections.Generic;
using SharpDX;

namespace VolumetricSelection2077.models
{
    public class SelectionInput
    {
        public required OrientedBoundingBox Obb { get; set; }
        public required BoundingBox Aabb { get; set; }
        public required List<string> Sectors { get; set; }
    }
}