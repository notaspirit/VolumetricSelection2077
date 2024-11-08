using System.Collections.Generic;

public class Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public class SelectionBox
{
    public Vector3 Origin { get; set; } = new();
    public Vector3 Max { get; set; } = new();
    public Vector3 Min { get; set; } = new();
    public Vector3 Scale { get; set; } = new();
    public Vector3 Rotation { get; set; } = new();
    public List<Vector3> Vertices { get; set; } = new();
}

public class Selection
{
    public SelectionBox Box { get; set; } = new();
    public List<string> Sectors { get; set; } = new();
}