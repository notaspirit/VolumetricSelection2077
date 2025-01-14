using System;
using System.IO;
using System.Text.Json;

namespace VolumetricSelection2077.Services;
public class SettingsService
{
    private static SettingsService? _instance;
    private static readonly object _lock = new object();
    private static readonly string SettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "settings.json");

    // Parameterless constructor for deserialization
    public SettingsService()
    {
        // Initialize default settings here
        GameDirectory = "";
        CacheEnabled = true;
        CacheDirectory = "";
        SaveToArchiveMods = false;
        OutputDirectory = "";
        OutputFilename = "";
        WolvenkitCLIPath = "";
        WolvenkitAPIRequestInterval = 1000;
        WolvenkitAPIRequestTimeout = 1 * 1000 * 60;
        WolvenkitAPIInactivityTimeout = 5 * 1000 * 60;
        WolvenkitProjectPath = "";
    }

    // Singleton instance
    public static SettingsService Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new SettingsService();
                    _instance.LoadSettings();
                }
                return _instance;
            }
        }
    }

    // Properties
    public string GameDirectory { get; set; }
    public bool CacheEnabled { get; set; }
    public string CacheDirectory { get; set; }
    public bool SaveToArchiveMods { get; set; }
    public string OutputDirectory { get; set; }
    public string OutputFilename { get; set; }
    public string WolvenkitCLIPath { get; set; }
    public int WolvenkitAPIRequestInterval { get; set; }
    public int WolvenkitAPIRequestTimeout { get; set; }
    public string WolvenkitProjectPath { get; set; }

    public int WolvenkitAPIInactivityTimeout { get; set; }
    // Methods for loading and saving settings
    public void LoadSettings()
    {
        if (!File.Exists(SettingsFilePath))
        {
            SaveSettings();
        }
        else
        {
            try
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<SettingsService>(json);
                if (settings != null)
                {
                    GameDirectory = settings.GameDirectory;
                    CacheEnabled = settings.CacheEnabled;
                    CacheDirectory = settings.CacheDirectory;
                    SaveToArchiveMods = settings.SaveToArchiveMods;
                    OutputDirectory = settings.OutputDirectory;
                    OutputFilename = settings.OutputFilename;
                    WolvenkitCLIPath = settings.WolvenkitCLIPath;
                    WolvenkitAPIRequestInterval = settings.WolvenkitAPIRequestInterval;
                    WolvenkitProjectPath = settings.WolvenkitProjectPath;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading settings: {ex.Message}");
            }
        }
    }

    public void SaveSettings()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(this, options);
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!Directory.Exists(directory) && directory != null)
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error saving settings: {ex.Message}");
        }
    }
}