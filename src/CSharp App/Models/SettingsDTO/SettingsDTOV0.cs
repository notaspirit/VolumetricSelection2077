using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using VolumetricSelection2077.Json.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Enums;

namespace VolumetricSelection2077.Models.SettingsDTO;

public partial class ExperimentalSettingsDTOV0 : ObservableObject
{
    [ObservableProperty] private bool _debugMode;

    public ExperimentalSettingsDTOV0()
    {
        DebugMode = false;
    }
}

public partial class SettingsDTOV0 : SettingsDTO
{
    [ObservableProperty] private string _gameDirectory;
    [ObservableProperty] private bool _cacheEnabled;
    [ObservableProperty] private string _cacheDirectory;
    [ObservableProperty] private SaveFileLocation _saveFileLocation;
    [ObservableProperty] private string _outputDirectory;
    [ObservableProperty] private string _outputFilename;
    [ObservableProperty] private bool _supportModdedResources;

    [JsonIgnore]
    public BitArray NodeTypeFilter;

    [JsonProperty(nameof(NodeTypeFilter))]
    public bool[] NodeTypeFilterProxy
    {
        get => NodeTypeFilter.Cast<bool>().ToArray();
        set => NodeTypeFilter = new BitArray(value);
    }

    [ObservableProperty] private ObservableCollection<string> _resourceNameFilter;
    [ObservableProperty] private ObservableCollection<string> _debugNameFilter;

    [ObservableProperty] private bool _filterModeOr;
    [ObservableProperty] private bool _isFiltersMWVisible;
    [ObservableProperty] private bool _isParametersMWVisible;
    [ObservableProperty] private SaveFileMode _saveMode;
    [ObservableProperty] private DestructibleMeshTreatment _destructibleMeshTreatment;
    [ObservableProperty] private bool _autoUpdate;
    [ObservableProperty] private bool _didUpdate;
    [ObservableProperty] private bool _gameRunningDuringUpdate;
    [ObservableProperty] private string _cETInstallLocation;
    [ObservableProperty] private bool _cacheModdedResources;
    [ObservableProperty] private WindowRecoveryState _windowRecoveryState;
    [ObservableProperty] private string _customSelectionFilePath;
    [ObservableProperty] private string _backupDirectory;
    [ObservableProperty] private SaveFileFormat _saveFileFormat;
    [ObservableProperty] private int _maxBackupFiles;
    [ObservableProperty] private bool _autoScrollLogViewer;
    [ObservableProperty] private bool _rememberFailedResources;
    
    [ObservableProperty] private ExperimentalSettingsDTOV0 _experimentalSettings;
    
    
    public SettingsDTOV0()
    {
        DTOVersion = 0;
        
        GameDirectory = "";
        CacheEnabled = true;
        CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "cache");
        SaveFileLocation = SaveFileLocation.GameDirectory;
        OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "output");
        OutputFilename = "";
        NodeTypeFilter = new BitArray(122, true);
        ResourceNameFilter = [];
        DebugNameFilter = [];
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
        WindowRecoveryState = new WindowRecoveryState();
        CustomSelectionFilePath = "";
        BackupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "OutputBackup");
        MaxBackupFiles = 10;
        AutoScrollLogViewer = true;
        RememberFailedResources = true;
        
        ExperimentalSettings = new ExperimentalSettingsDTOV0();
    }
    
    public override void Deserialize(string json) 
    {
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
        
        RememberFailedResources = j.Value<bool?>(nameof(RememberFailedResources)) ?? RememberFailedResources;
        
        ExperimentalSettings.DebugMode = j.Value<bool?>("DebugMode") ?? ExperimentalSettings.DebugMode;
    }
    
    public override string Serialize() => JsonConvert.SerializeObject(this, JsonSerializerPresets.Default);
}