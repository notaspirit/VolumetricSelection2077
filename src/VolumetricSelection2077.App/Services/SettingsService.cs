using System;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using VolumetricSelection2077.App.Enums;
using VolumetricSelection2077.App.Models.SettingsDTO;
using VolumetricSelection2077.Core.Enums;

namespace VolumetricSelection2077.App.Services;
public partial class SettingsService : ObservableObject
{
    private readonly string _settingsFilePath;

    public SettingsService(string settingsFilePath)
    {
        ProgramVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split("+")[0] ?? "Version not found.";
        LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "Logs");
        _settingsFilePath = settingsFilePath;
        Data = new SettingsDTOV1();
        
        LoadSettings();
        
        if (ValidationService.ValidateGamePath(Data.GameDirectory).Item1 ==
            GamePathValidationResult.InvalidGamePath && 
            TryGetGamePathFromWolvenKit(out var wkitGamePath) && 
            wkitGamePath is not null)
        {
            Data.GameDirectory = wkitGamePath;
            SaveSettings();
        }
    }
    
    [ObservableProperty] private SettingsDTOV1 _data;
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
                var dtoRes = SettingsDtoHelper.SmartDeserialize(File.ReadAllText(_settingsFilePath));
                if (dtoRes.Status != ResultStatus.Ok)
                {
                    Logger.Error($"Failed to load settings with error: {dtoRes.Message}");
                    SaveSettings();
                }
                else
                {
                    Data = dtoRes.Data!;
                }
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
    
    private static bool TryGetGamePathFromWolvenKit(out string? gamePath)
    {
        gamePath = null;
        var wkitConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "REDModding", "WolvenKit", "config.json");
        if (!File.Exists(wkitConfigFile))
            return false;
        var config = File.ReadAllText(wkitConfigFile);
        var json = Newtonsoft.Json.Linq.JObject.Parse(config);
        var gamepath = json["CP77ExecutablePath"]?.ToString();
        if (string.IsNullOrEmpty(gamepath))
            return false;
        gamePath = gamepath.Replace(@"\bin\x64\Cyberpunk2077.exe", "");
        return true;
    }
}