using System;
using System.IO;
using System.Text.Json;
using Serilog;

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
        CacheSectorData = true;
        CacheMeshData = true;
        CacheGeometryCache = true;
        CacheDirectory = "";
        SaveToArchiveMods = false;
        OutputDirectory = "";
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
    public bool CacheSectorData { get; set; }
    public bool CacheMeshData { get; set; }
    public bool CacheGeometryCache { get; set; }
    public string CacheDirectory { get; set; }
    public bool SaveToArchiveMods { get; set; }
    public string OutputDirectory { get; set; }

    // Methods for loading and saving settings
    public void LoadSettings()
    {
        Log.Information($"Loading settings from: {SettingsFilePath}");
        if (!File.Exists(SettingsFilePath))
        {
            SaveSettings();
        }
        else
        {
            try
            {
                var json = File.ReadAllText(SettingsFilePath);
                Log.Information($"Loaded JSON: {json}");
                var settings = JsonSerializer.Deserialize<SettingsService>(json);
                if (settings != null)
                {
                    GameDirectory = settings.GameDirectory;
                    CacheEnabled = settings.CacheEnabled;
                    CacheSectorData = settings.CacheSectorData;
                    CacheMeshData = settings.CacheMeshData;
                    CacheGeometryCache = settings.CacheGeometryCache;
                    CacheDirectory = settings.CacheDirectory;
                    SaveToArchiveMods = settings.SaveToArchiveMods;
                    OutputDirectory = settings.OutputDirectory;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading settings: {ex.Message}");
            }
        }
    }

    public void SaveSettings()
    {
        Log.Information($"Saving settings to: {SettingsFilePath}");
        Log.Error("Test");
        Log.Information("Test");
        Log.Debug("Test");
        Log.Warning("Test");
        Log.Fatal("Test");
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
            Log.Error($"Error saving settings: {ex.Message}");
        }
    }
}