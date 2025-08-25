using System.Collections.Generic;
using System.Linq;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Resources;

namespace VolumetricSelection2077.ViewModels;

public class DialogWindowViewModel
{
    public DialogWindowViewModel(string title, string message, DialogButton[] buttons)
    {
        Title = title;
        Message = message;
        ButtonContents = buttons;
    }
    public IEnumerable<DialogButton> ButtonContents { get; set; }

    public IEnumerable<DialogButton> PrimaryButtons => ButtonContents.Where(btn => btn.Style == DialogButtonStyling.Enum.Primary);
    public IEnumerable<DialogButton> SecondaryButtons => ButtonContents.Where(btn => btn.Style == DialogButtonStyling.Enum.Secondary);
    public IEnumerable<DialogButton> DestructiveButtons => ButtonContents.Where(btn => btn.Style == DialogButtonStyling.Enum.Destructive);
    
    public string Title { get; set; }
    public string Message { get; set;  }
}