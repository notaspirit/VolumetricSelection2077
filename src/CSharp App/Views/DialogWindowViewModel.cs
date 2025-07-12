using System.Collections.Generic;

namespace VolumetricSelection2077.ViewModels;

public class DialogWindowViewModel
{
    public DialogWindowViewModel(string title, string message, string[] buttons)
    {
        Title = title;
        Message = message;
        ButtonContents = buttons;
    }
    public IEnumerable<string> ButtonContents { get; set; }
    public string Title { get; set; }
    public string Message { get; set;  }
}