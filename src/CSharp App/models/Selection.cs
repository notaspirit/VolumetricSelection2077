using BulletSharp.Math;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace VolumetricSelection2077.Models
{
    public class SelectionBox
    {
        [JsonPropertyName("origin")]
        public Vector3 Origin { get; set; }

        [JsonPropertyName("max")]
        public Vector3 Max { get; set; }

        [JsonPropertyName("min")]
        public Vector3 Min { get; set; }

        [JsonPropertyName("scale")]
        public Vector3 Scale { get; set; }

        [JsonPropertyName("rotation")]
        public Quaternion Rotation { get; set; }

        [JsonPropertyName("vertices")]
        public required List<Vector3> Vertices { get; set; }
    }

    public class SelectionInput
    {
        [JsonPropertyName("box")]
        public required SelectionBox Box { get; set; }

        [JsonPropertyName("sectors")]
        public required string[] Sectors { get; set; }
    }
}