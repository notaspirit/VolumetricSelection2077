using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace VolumetricSelection2077.Services;
public partial class SettingsService : ObservableObject
{
    private static SettingsService? _instance;
    private static readonly object _lock = new object();
    private static readonly string SettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "settings.json");
    public SettingsService()
    {
        GameDirectory = "";
        CacheEnabled = true;
        CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "cache");
        SaveFileLocation = SaveFileLocation.GameDirectory;
        OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "output");
        OutputFilename = "";
        DebugMode = false;
        NodeTypeFilter = new BitArray(122, true);
        ProgramVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split("+")[0] ?? "Version not found.";
        ResourceNameFilter = new();
        DebugNameFilter = new();
        FilterModeOr = true;
        IsFiltersMWVisible = false;
        IsParametersMWVisible = false;
        SaveFileFormat = SaveFileFormat.ArchiveXLJson;
        DestructibleMeshTreatment = DestructibleMeshTreatment.DynamicMesh;
        SaveMode = SaveFileMode.New;
        SupportModdedResources = false;
        CacheModdedResources = true;
        AutoUpdate = true;
        DidUpdate = false;
        GameRunningDuringUpdate = false;
        CETInstallLocation = "";
        WindowRecoveryState = new();
        CustomSelectionFilePath = "";
        BackupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "OutputBackup");
        MaxBackupFiles = 10;
        AutoScrollLogViewer = true;
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
    
    public string GameDirectory { get; set; }
    public bool CacheEnabled { get; set; }
    public string CacheDirectory { get; set; }
    public SaveFileLocation SaveFileLocation { get; set; }
    public string OutputDirectory { get; set; }
    public string OutputFilename { get; set; }
    public bool SupportModdedResources { get; set; }
    
    [JsonIgnore]
    public BitArray NodeTypeFilter { get; set; }
    
    [JsonProperty(nameof(NodeTypeFilter))]
    public bool[] NodeTypeFilterProxy
    {
        get => NodeTypeFilter.Cast<bool>().ToArray();
        set => NodeTypeFilter = value != null ? new BitArray(value) : new BitArray(122, true);
    }
    
    [JsonIgnore]
    public string ProgramVersion { get; set; }
    
    public ObservableCollection<string> ResourceNameFilter { get; set; }
    public ObservableCollection<string> DebugNameFilter { get; set; }
    
    public bool FilterModeOr { get; set; }
    public bool IsFiltersMWVisible { get; set; }
    public bool IsParametersMWVisible { get; set; }
    public SaveFileMode SaveMode { get; set; }
    public DestructibleMeshTreatment DestructibleMeshTreatment { get; set; }
    public bool AutoUpdate { get; set; }
    public bool DidUpdate { get; set; }
    public bool GameRunningDuringUpdate { get; set; }
    public string CETInstallLocation { get; set; }
    public bool CacheModdedResources { get; set; }
    public string MinimumCacheVersion { get; } = "1000.0.0-beta11";
    public WindowRecoveryState WindowRecoveryState { get; set; }
    public string CustomSelectionFilePath { get; set; }
    public string BackupDirectory { get; set; }
    public SaveFileFormat SaveFileFormat { get; set; }
    public int MaxBackupFiles { get; set; }
    public bool AutoScrollLogViewer { get; set; }
    public string LogDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "Logs");

    #region ExperimentalSettings

    [ObservableProperty]
    private bool _debugMode;
    public Enums.ExperimentalSettingsEnum.ProxyMeshTreatment ProxyMeshTreatment { get; set; } = Enums.ExperimentalSettingsEnum.ProxyMeshTreatment.RegularMesh;

    #endregion
    
    /// <summary>
    /// Loads the settings or creates a new settings file if it doesn't exist
    /// </summary>
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
                var j = JObject.Parse(json);
                
                GameDirectory = j.Value<string>(nameof(GameDirectory)) ?? GameDirectory;
                CacheEnabled = j.Value<bool?>(nameof(CacheEnabled)) ?? CacheEnabled;
                
                var tempCacheDirectory = j.Value<string>(nameof(CacheDirectory));
                if (!string.IsNullOrEmpty(tempCacheDirectory))
                    CacheDirectory = tempCacheDirectory;
                
                // replaced with SaveFileLocation after 1000.0.0-beta12
                var OLDSaveToArchiveMods = j.Value<bool?>("SaveToArchiveMods");
                if (OLDSaveToArchiveMods != null)
                    SaveFileLocation = (bool)OLDSaveToArchiveMods
                        ? SaveFileLocation.GameDirectory
                        : SaveFileLocation.OutputDirectory;
                else
                    SaveFileLocation = (SaveFileLocation?)j.Value<long?>(nameof(SaveFileLocation)) ?? SaveFileLocation;

                var tempOutputDirectory = j.Value<string>(nameof(OutputDirectory));
                if (!string.IsNullOrEmpty(tempOutputDirectory))
                    OutputDirectory = tempOutputDirectory;

                OutputFilename = j.Value<string>(nameof(OutputFilename)) ?? OutputFilename;
                
                NodeTypeFilterProxy = j[nameof(NodeTypeFilter)]?.ToObject<bool[]>() ?? NodeTypeFilterProxy;
                
                var tempResourceNameFilter = j[nameof(ResourceNameFilter)]?.ToObject<ObservableCollection<string>>() ?? ResourceNameFilter;
                ResourceNameFilter.Clear();
                foreach (var s in tempResourceNameFilter)
                    ResourceNameFilter.Add(s);

                var tempDebugNameFilter = j[nameof(DebugNameFilter)]?.ToObject<ObservableCollection<string>>() ?? DebugNameFilter;
                DebugNameFilter.Clear();
                foreach (var s in tempDebugNameFilter)
                    DebugNameFilter.Add(s);
                
                FilterModeOr = j.Value<bool?>(nameof(FilterModeOr)) ?? FilterModeOr;
                IsFiltersMWVisible = j.Value<bool?>(nameof(IsFiltersMWVisible)) ?? IsFiltersMWVisible;
                IsParametersMWVisible = j.Value<bool?>(nameof(IsParametersMWVisible)) ?? IsParametersMWVisible;

                SaveMode = (SaveFileMode?)j.Value<long?>(nameof(SaveMode)) ?? SaveMode;
                SaveFileFormat = (SaveFileFormat?)j.Value<long?>(nameof(SaveFileFormat)) ?? SaveFileFormat;
                DestructibleMeshTreatment = (DestructibleMeshTreatment?)j.Value<long?>(nameof(DestructibleMeshTreatment)) ?? DestructibleMeshTreatment;
                
                SupportModdedResources = j.Value<bool?>(nameof(SupportModdedResources)) ?? SupportModdedResources;
                AutoUpdate = j.Value<bool?>(nameof(AutoUpdate)) ?? AutoUpdate;
                DidUpdate = j.Value<bool?>(nameof(DidUpdate)) ?? DidUpdate;
                GameRunningDuringUpdate = j.Value<bool?>(nameof(GameRunningDuringUpdate)) ?? GameRunningDuringUpdate;

                CETInstallLocation = j.Value<string>(nameof(CETInstallLocation)) ?? CETInstallLocation;
                CacheModdedResources = j.Value<bool?>(nameof(CacheModdedResources)) ?? CacheModdedResources;
                
                WindowRecoveryState = j[nameof(WindowRecoveryState)]?.ToObject<WindowRecoveryState>() ?? WindowRecoveryState;

                CustomSelectionFilePath = j.Value<string>(nameof(CustomSelectionFilePath)) ?? CustomSelectionFilePath;
                BackupDirectory = j.Value<string>(nameof(BackupDirectory)) ?? BackupDirectory;
                MaxBackupFiles = j.Value<int?>(nameof(MaxBackupFiles)) ?? MaxBackupFiles;
                AutoScrollLogViewer = j.Value<bool?>(nameof(AutoScrollLogViewer)) ?? AutoScrollLogViewer;
                
                DebugMode = j.Value<bool?>(nameof(DebugMode)) ?? DebugMode;
                ProxyMeshTreatment = (Enums.ExperimentalSettingsEnum.ProxyMeshTreatment?)j.Value<long?>(nameof(ProxyMeshTreatment)) ?? ProxyMeshTreatment;
                
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Failed to load settings.");
            }
        }
    }

    /// <summary>
    /// Saves the settings
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            var json = JsonConvert.SerializeObject(this, JsonSerializerPresets.Default);
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!Directory.Exists(directory) && directory != null)
                Directory.CreateDirectory(directory);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Failed to save settings.");
        }
    }
}