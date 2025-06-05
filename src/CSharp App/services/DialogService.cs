using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using VolumetricSelection2077.views;

namespace VolumetricSelection2077.services;

public class DialogService
{
    private Window _owner;
    
    public DialogService(Window owner)
    {
        _owner = owner;
    }
    
    public Task<int> ShowDialog(string title, string message, string[] buttonContents)
    {
        return ShowDialog(title, message, buttonContents, _owner);
    }
    
    public static async Task<int> ShowDialog(string title, string message, string[] buttonContents, Window owner)
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new Dialog(title, message, buttonContents);
            await dialog.ShowDialog(owner);
            return dialog.DialogResult;
        });
    }
}