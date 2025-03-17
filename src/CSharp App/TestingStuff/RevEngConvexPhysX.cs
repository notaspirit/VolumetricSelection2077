using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class RevEngConvexPhysX
{
    public static async void Run()
    {
        var gfs = GameFileService.Instance;
        var convexMesh = await gfs.GetPhysXMesh(7243, 2834);
    }
}