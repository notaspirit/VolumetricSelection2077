using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using VolumetricSelection2077.views;
using WolvenKit.Common.PhysX;

namespace VolumetricSelection2077.Services;

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
        var dialog = new Dialog(title, message, buttonContents);
        await dialog.ShowDialog(owner);
        return dialog.DialogResult;
    }
}