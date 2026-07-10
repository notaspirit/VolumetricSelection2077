using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VolumetricSelection2077.Models.SettingsDTO;

public class SettingsDTO : ObservableObject
{
    public int DTOVersion { get; set; } = -1;
    
    public SettingsDTO()
    {
        
    }

    public virtual void Deserialize(string json) => throw new NotImplementedException();
    public virtual string Serialize() => throw new NotImplementedException();
}