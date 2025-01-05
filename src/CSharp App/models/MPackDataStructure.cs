using MessagePack;
using System.Collections.Generic;

namespace VolumetricSelection2077.Models
{
    [MessagePackObject]
    public class StreamingSector
    {
        [Key(0)]
        public List<Node> Nodes { get; set; } = new();
    }

    [MessagePackObject]
    public class Node
    {
        [Key(0)]
        public int AXLindex { get; set; }

        [Key(1)]
        public required string Type { get; set; }

        [Key(2)]
        public string? MeshPath { get; set; }

        [Key(3)]
        public string? Entity { get; set; }

        [Key(4)]
        public Transform Transform { get; set; } = new();

        [Key(5)]
        public List<Actor>? Actors { get; set; }
    }

    [MessagePackObject]
    public class Entity
    {
        [Key(0)]
        public List<Component>? Components { get; set; }

        [Key(1)]
        public string? AppearanceFile { get; set; }

        [Key(2)]
        public string? AppearanceName { get; set; }
    }

    [MessagePackObject]
    public class Appearance
    {
        [Key(0)]
        public List<AppearanceAppearance> Appearances { get; set; } = new();
    }

    [MessagePackObject]
    public class Mesh
    {
        [Key(0)]
        public List<float[]> Vertices { get; set; } = new();

        [Key(1)]
        public List<int[]> Triangles { get; set; } = new();

        [Key(2)]
        public BoundingBox BoundingBox { get; set; } = new();
    }

    [MessagePackObject]
    public class BoundingBox
    {
        [Key(0)]
        public float[] Min { get; set; } = new float[3];

        [Key(1)]
        public float[] Max { get; set; } = new float[3];
    }

    [MessagePackObject]
    public class Transform
    {
        [Key(0)]
        public float[] Position { get; set; } = new float[3];

        [Key(1)]
        public float[]? Rotation { get; set; } = new float[4];

        [Key(2)]
        public float[]? Scale { get; set; } = new float[3];
    }

    [MessagePackObject]
    public class Actor
    {
        [Key(0)]
        public int ActorIndex { get; set; }

        [Key(1)]
        public List<Shape> Shapes { get; set; } = new();

        [Key(2)]
        public Transform Transform { get; set; } = new();
    }

    [MessagePackObject]
    public class Shape
    {
        [Key(0)]
        public required string Type { get; set; }

        [Key(1)]
        public int? MeshId { get; set; }

        [Key(2)]
        public Transform Transform { get; set; } = new();
    }

    [MessagePackObject]
    public class Component
    {
        [Key(0)]
        public required string MeshPath { get; set; }

        [Key(1)]
        public Transform Transform { get; set; } = new();
    }

    [MessagePackObject]
    public class AppearanceAppearance
    {
        [Key(0)]
        public required string Name { get; set; }

        [Key(1)]
        public List<Component> Components { get; set; } = new();
    }
}