using VolumetricSelection2077.App.Enums;
using VolumetricSelection2077.App.Resources;

namespace VolumetricSelection2077.App.Models;

public class DialogButton
{
    public string Content { get; set; }
    public DialogButtonStyling Style { get; set; }
    
    public DialogButton(string content, DialogButtonStyling style)
    {
        Content = content;
        Style = style;
    }
}