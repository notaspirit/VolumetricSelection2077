using VolumetricSelection2077.Resources;

namespace VolumetricSelection2077.Models;

public class DialogButton
{
    public string Content { get; set; }
    public DialogButtonStyling.Enum Style { get; set; }
    
    public DialogButton(string content, DialogButtonStyling.Enum style)
    {
        Content = content;
        Style = style;
    }
}