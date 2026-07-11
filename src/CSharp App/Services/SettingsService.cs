using System;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using VolumetricSelection2077.Models.SettingsDTO;

namespace VolumetricSelection2077.Services;
public partial class SettingsService : ObservableObject
{
    private readonly string _settingsFilePath;

    public SettingsService(string settingsFilePath)
    {
        ProgramVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split("+")[0] ?? "Version not found.";
        LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "Logs");
        _settingsFilePath = settingsFilePath;
        Data = new SettingsDTOV0();
        
        LoadSettings();
    }
    
    [ObservableProperty] private SettingsDTOV0 _data;
    public static string MinimumCacheVersion => "1000.0.0-beta11";
    public readonly string LogDirectory;
    public string ProgramVersion { get; }
    
    /// <summary>
    /// Loads the settings or creates a new settings file if it doesn't exist
    /// </summary>
    public void LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            SaveSettings();
        }
        else
        {
            try
            {
                Data.Deserialize(File.ReadAllText(_settingsFilePath));
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Failed to load settings.");
            }
        }
        
        Directory.CreateDirectory(Data.CacheDirectory);
        Directory.CreateDirectory(Data.BackupDirectory);
        Directory.CreateDirectory(Data.OutputDirectory);
        Directory.CreateDirectory(LogDirectory);
    }

    /// <summary>
    /// Saves the settings
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            var json = Data.Serialize();
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Failed to save settings.");
        }
    }
}