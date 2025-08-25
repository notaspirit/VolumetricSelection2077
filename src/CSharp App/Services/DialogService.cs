using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Views;
using WolvenKit.Common.PhysX;

namespace VolumetricSelection2077.Services;

public class DialogService
{
    private Window _owner;
    
    public DialogService(Window owner)
    {
        _owner = owner;
    }
    
    public Task<int> ShowDialog(string title, string message, DialogButton[] buttonContents)
    {
        return ShowDialog(title, message, buttonContents, _owner);
    }
    
    public static async Task<int> ShowDialog(string title, string message, DialogButton[] buttonContents, Window owner)
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new Dialog(title, message, buttonContents);
            await dialog.ShowDialog(owner);
            return dialog.DialogResult;
        });
    }
}