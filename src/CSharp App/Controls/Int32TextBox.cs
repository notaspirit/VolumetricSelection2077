using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace VolumetricSelection2077.Controls;

public class Int32TextBox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);
    public Int32TextBox()
    {
        ApplyTemplate();
    }
    
    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Text) && !int.TryParse(e.Text, out _))
        {
            e.Handled = true;
        }
        
        base.OnTextInput(e);
    }
    
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            Text = "0";
        }
        base.OnLostFocus(e);
    }

}