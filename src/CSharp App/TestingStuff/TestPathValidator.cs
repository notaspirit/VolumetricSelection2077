using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestPathValidator : IDebugTool
{
    public void Run()
    {
        var validDir = @"E:\Games\Cyberpunk 2077";
        var validFile = @"E:\Games\Cyberpunk 2077.zip";
        var drivePath = @"E:\";
        var invalidChar = @"E:\<>?";
        var empty = "";
        var empty2 = "     ";
        string? nullPath = null;
        var tooLong = @"E:\TooLongfjhklasd hgfjksalbfkhjasbfghjsagbfhjkashfjsahfjkashbgfhjkasgfhjkasghfjkashfjkashfjkasbfhjkashdfjksahfgjksahfjkashfjkasfhjkasfgbhajksflhjkasfghasjkfghasjkfghsajkfbnsajkfnvuipqwghfriquwygrt iuahgafuifgwauipghasiognbaspiofghasi;gTooLongfjhklasd hgfjksalbfkhjasbfghjsagbfhjkashfjsahfjkashbgfhjkasgfhjkasghfjkashfjkashfjkasbfhjkashdfjksahfgjksahfjkashfjkasfhjkasfgbhajksflhjkasfghasjkfghasjkfghsajkfbnsajkfnvuipqwghfriquwygrt iuahgafuifgwauipghasiognbaspiofghasi;gTooLongfjhklasd hgfjksalbfkhjasbfghjsagbfhjkashfjsahfjkashbgfhjkasgfhjkasghfjkashfjkashfjkasbfhjkashdfjksahfgjksahfjkashfjkasfhjkasfgbhajksflhjkasfghasjkfghasjkfghsajkfbnsajkfnvuipqwghfriquwygrt iuahgafuifgwauipghasiognbaspiofghasi;gTooLongfjhklasd hgfjksalbfkhjasbfghjsagbfhjkashfjsahfjkashbgfhjkasgfhjkasghfjkashfjkashfjkasbfhjkashdfjksahfgjksahfjkashfjkasfhjkasfgbhajksflhjkasfghasjkfghasjkfghsajkfbnsajkfnvuipqwghfriquwygrt iuahgafuifgwauipghasiognbaspiofghasi;g";
        var trailingOrLeadingSpace = @"E:\Games  \Cyberpunk 2077   \    bin";
        var emptyPart = @"E:\Games\\Test";
        var relative = @"some\path";
        
        var tests = new[] {validDir, validFile, invalidChar, empty, empty2, trailingOrLeadingSpace, tooLong, emptyPart, relative};
        foreach (string test in tests)
        {
            Logger.Info($"Testing {test} : {ValidationService.ValidatePath(test)}");
        }
    }
}