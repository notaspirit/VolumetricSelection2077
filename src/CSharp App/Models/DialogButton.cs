using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Resources;

namespace VolumetricSelection2077.Models;

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