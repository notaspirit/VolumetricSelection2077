using System.Collections.Generic;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class testGettingFailingMeshes
{
    public static void Run()
    {
        List<string> failedMeshes = new();
        failedMeshes.Add(@"engine\\meshes\\editor\\plane_occluder_twosided.mesh");
        failedMeshes.Add(@"engine\meshes\editor\plane_occluder_twosided.mesh");

        var gfs = GameFileService.Instance;
        
        foreach (string failedMesh in failedMeshes)
        {
            var (success, error, output) = gfs.GetGameFileAsGlb(failedMesh);
            Logger.Info($"Success: {success}\nError: {error}\nOutput: {output}");
        }
    }
}