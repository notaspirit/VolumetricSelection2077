using SharpDX;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace VolumetricSelection2077.Models
{
    public class SelectionInput
    {
        public required OrientedBoundingBox Obb { get; set; }
        public required BoundingBox Aabb { get; set; }
        public required List<string> Sectors { get; set; }
    }
}