using System.Collections.Generic;

namespace VolumetricSelection2077.Models
{
    public class Selection
    {
        public required SelectionBox SelectionBox { get; set; }
        public required List<string> Sectors { get; set; }
    }

    public class SelectionBox
    {
        public required Position Pos1 { get; set; }
        public required Position Pos2 { get; set; }
        public required Quaternion Quat { get; set; }
    }

    public class Position
    {
        public required float X { get; set; }
        public required float Y { get; set; }
        public required float Z { get; set; }
        public required float W { get; set; }
    }

    public class Quaternion
    {
        public required float I { get; set; }
        public required float J { get; set; }
        public required float K { get; set; }
        public required float R { get; set; }
    }
}