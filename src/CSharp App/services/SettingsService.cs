using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        SaveToArchiveMods = true;
        OutputDirectory = "";
        OutputFilename = "";
        DebugMode = false;
        NodeTypeFilter = new BitArray(122, true);
        ProgramVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split("+")[0] ?? "Version not found.";
        SaveAsYaml = false;
        AllowOverwrite = false;
        ExtendExistingFile = false;
        ResourceNameFilter = new();
        DebugNameFilter = new();
    }
    
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
    public bool DebugMode { get; set; }
    
    [JsonIgnore]
    public BitArray NodeTypeFilter { get; set; }
    
    [JsonPropertyName("NodeTypeFilter")]
    public bool[] NodeTypeFilterProxy
    {
        get => NodeTypeFilter.Cast<bool>().ToArray();
        set => NodeTypeFilter = value != null ? new BitArray(value) : new BitArray(122, true);
    }
    
    [JsonIgnore]
    public string ProgramVersion { get; set; }
    
    public bool SaveAsYaml { get; set; }
    
    public bool AllowOverwrite { get; set; }
    public bool ExtendExistingFile { get; set; }
    public List<string> ResourceNameFilter { get; set; }
    public List<string> DebugNameFilter { get; set; }
    
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
                    DebugMode = settings.DebugMode;
                    NodeTypeFilter = settings.NodeTypeFilter;
                    SaveAsYaml = settings.SaveAsYaml;
                    AllowOverwrite = settings.AllowOverwrite;
                    ExtendExistingFile = settings.ExtendExistingFile;
                    ResourceNameFilter = settings.ResourceNameFilter;
                    DebugNameFilter = settings.DebugNameFilter;
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