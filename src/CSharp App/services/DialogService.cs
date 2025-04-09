using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using VolumetricSelection2077.views;
using WolvenKit.Common.PhysX;

namespace VolumetricSelection2077.Services;

public class DialogService
{
    public enum DialogResult
    {
        LeftButton,
        RightButton
    }
    
    public static async Task<DialogResult> ShowDialog(string title, string message, string buttonLeftText, string buttonRightText, Window owner)
    {
        var dialog = new Dialog
        {
            Title = title,
            Message = message,
            ButtonLeftText = buttonLeftText,
            ButtonRightText = buttonRightText
        };
        
        await dialog.ShowDialog(owner);
        
        return dialog.DialogResult ? DialogResult.LeftButton : DialogResult.RightButton;
    }
}