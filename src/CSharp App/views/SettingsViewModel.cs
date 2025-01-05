using VolumetricSelection2077.Services;
using VolumetricSelection2077.Resources;

namespace VolumetricSelection2077.ViewModels
{
    public class SettingsViewModel
    {
    public Descriptions Descriptions { get; }
       public SettingsService Settings { get; }

       public SettingsViewModel()
       {
           Descriptions = new Descriptions();
           Settings = SettingsService.Instance;
       }
   }
}