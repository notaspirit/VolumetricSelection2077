using System.Threading.Tasks;
using Avalonia.Controls;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestDialogService
{
    public static async Task Run(Window owner)
    {
        var dialogService = new DialogService(owner);
        var resultDynamic = await dialogService.ShowDialog("Test This", "Dynamic Method", new[]
        {
            new DialogButton("Retry", DialogButtonStyling.Enum.Primary),
            new DialogButton("Build", DialogButtonStyling.Enum.Secondary),
            new DialogButton("Cancel", DialogButtonStyling.Enum.Destructive)
        });
        Logger.Info($"Result Dynamic: {resultDynamic}");
        
        var resultStatic = await DialogService.ShowDialog("Test This", "Static Method", new[]
        {
            new DialogButton("Retry", DialogButtonStyling.Enum.Primary),
            new DialogButton("Build", DialogButtonStyling.Enum.Secondary),
            new DialogButton("Cancel", DialogButtonStyling.Enum.Destructive)
        }, owner);
        Logger.Info($"Result Static: {resultStatic}");
        
        Logger.Info("Finished Test.");
    }
}