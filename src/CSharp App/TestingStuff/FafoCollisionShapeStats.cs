using System.Threading.Tasks;
using System.Linq;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Types;

namespace VolumetricSelection2077.TestingStuff;

public class FafoCollisionShapeStats
{
    private struct CollisionShapeStats
    {
        public int Box { get; set; }
        public int Sphere { get; set; }
        public int Capsule { get; set; }
        public int ConvexMesh { get; set; }
        public int TriangleMesh { get; set; }
    }
    
    private static CollisionShapeStats _stats;
    
    public static async Task Run()
    {
        Logger.Info("Starting Fafo Collision Shape Stats Test...");
        
        _stats = new CollisionShapeStats();
        
        var gfs = GameFileService.Instance;
        
        var sectorPaths = gfs.ArchiveManager.GetGameArchives()
            .SelectMany(x => x.Files.Values.Where(y => y.Extension == ".streamingsector")
                .Select(y => y.FileName))
            .Distinct()
            .ToList();

        var tasks = sectorPaths.Select(x => Task.Run(() => GetStats(x, gfs)));
        await Task.WhenAll(tasks);
        Logger.Info("Shape stats:");
        Logger.Info( "\n" +
                     "type,count\n" +
                    $"box,{_stats.Box}\n" +
                    $"sphere,{_stats.Sphere}\n" +
                    $"capsule,{_stats.Capsule}\n" +
                    $"convex mesh,{_stats.ConvexMesh}\n" +
                    $"triangle mesh,{_stats.TriangleMesh}\n");
    }
    
    private static void GetStats(string sectorPath, GameFileService gfs)
    {
        var sector = gfs.GetSector(sectorPath);
        if (sector == null)
        {
            Logger.Error($"Failed to get sector {sectorPath}");
            return;
        }

        foreach (var colnode in sector.NodeData.Where(nd =>
                     sector.Nodes[nd.NodeIndex].Type == NodeTypeProcessingOptions.Enum.worldCollisionNode))
        {
            var node = sector.Nodes[colnode.NodeIndex];
            if (node.Actors == null)
                continue;
            foreach (var actor in node.Actors)
            {
                if (actor.Shapes == null)
                    continue;

                foreach (var shape in actor.Shapes)
                {
                    switch (shape.ShapeType)
                    {
                        case Enums.physicsShapeType.Box:
                            _stats.Box++;
                            break;
                        case Enums.physicsShapeType.Sphere:
                            _stats.Sphere++;
                            break;
                        case Enums.physicsShapeType.Capsule:
                            _stats.Capsule++;
                            break;
                        case Enums.physicsShapeType.ConvexMesh:
                            _stats.ConvexMesh++;
                            break;
                        case Enums.physicsShapeType.TriangleMesh:
                            _stats.TriangleMesh++;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}