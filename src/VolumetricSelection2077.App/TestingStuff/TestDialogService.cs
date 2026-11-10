using System.Threading.Tasks;
using Avalonia.Controls;
using VolumetricSelection2077.App.Enums;
using VolumetricSelection2077.App.Models;
using VolumetricSelection2077.App.Resources;
using VolumetricSelection2077.App.Services;

namespace VolumetricSelection2077.App.TestingStuff;

public class TestDialogService
{
    public static async Task Run(Window owner)
    {
        var dialogService = new DialogService(owner);
        var resultDynamic = await dialogService.ShowDialog("Test This", "Dynamic Method", new[]
        {
            new DialogButton("Retry", DialogButtonStyling.Primary),
            new DialogButton("Build", DialogButtonStyling.Secondary),
            new DialogButton("Cancel", DialogButtonStyling.Destructive)
        });
        Logger.Info($"Result Dynamic: {resultDynamic}");
        
        var resultStatic = await DialogService.ShowDialog("Test This", "Static Method", new[]
        {
            new DialogButton("Retry", DialogButtonStyling.Primary),
            new DialogButton("Build", DialogButtonStyling.Secondary),
            new DialogButton("Cancel", DialogButtonStyling.Destructive)
        }, owner);
        Logger.Info($"Result Static: {resultStatic}");
        
        Logger.Info("Finished Test.");
    }
}