using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.App.Enums;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Json.Helpers;

namespace VolumetricSelection2077.App.Models.SettingsDTO;

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
    [ObservableProperty] private FilePostProcessingMode _postProcessingMode;
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
    
    [ObservableProperty] private bool _debugMode;
    
    
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
        PostProcessingMode = FilePostProcessingMode.New;
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

        DebugMode = false;
    }
    
    public override void Deserialize(string json) => Deserialize(JObject.Parse(json));
    
    public override void Deserialize(JObject json) 
    {
        GameDirectory = json.Value<string>(nameof(GameDirectory)) ?? GameDirectory;
        CacheEnabled = json.Value<bool?>(nameof(CacheEnabled)) ?? CacheEnabled;
        
        var tempCacheDirectory = json.Value<string>(nameof(CacheDirectory));
        if (!string.IsNullOrEmpty(tempCacheDirectory))
            CacheDirectory = tempCacheDirectory;
        
        // replaced with SaveFileLocation after 1000.0.0-beta12
        var OLDSaveToArchiveMods = json.Value<bool?>("SaveToArchiveMods");
        if (OLDSaveToArchiveMods != null)
            SaveFileLocation = (bool)OLDSaveToArchiveMods
                ? SaveFileLocation.GameDirectory
                : SaveFileLocation.OutputDirectory;
        else
            SaveFileLocation = (SaveFileLocation?)json.Value<long?>(nameof(SaveFileLocation)) ?? SaveFileLocation;

        var tempOutputDirectory = json.Value<string>(nameof(OutputDirectory));
        if (!string.IsNullOrEmpty(tempOutputDirectory))
            OutputDirectory = tempOutputDirectory;

        OutputFilename = json.Value<string>(nameof(OutputFilename)) ?? OutputFilename;
        
        NodeTypeFilterProxy = json[nameof(NodeTypeFilter)]?.ToObject<bool[]>() ?? NodeTypeFilterProxy;
        
        var tempResourceNameFilter = json[nameof(ResourceNameFilter)]?.ToObject<ObservableCollection<string>>() ?? ResourceNameFilter;
        ResourceNameFilter.Clear();
        foreach (var s in tempResourceNameFilter)
            ResourceNameFilter.Add(s);

        var tempDebugNameFilter = json[nameof(DebugNameFilter)]?.ToObject<ObservableCollection<string>>() ?? DebugNameFilter;
        DebugNameFilter.Clear();
        foreach (var s in tempDebugNameFilter)
            DebugNameFilter.Add(s);
        
        FilterModeOr = json.Value<bool?>(nameof(FilterModeOr)) ?? FilterModeOr;
        IsFiltersMWVisible = json.Value<bool?>(nameof(IsFiltersMWVisible)) ?? IsFiltersMWVisible;
        IsParametersMWVisible = json.Value<bool?>(nameof(IsParametersMWVisible)) ?? IsParametersMWVisible;

        PostProcessingMode = (FilePostProcessingMode?)json.Value<long?>("SaveMode") ?? PostProcessingMode;
        SaveFileFormat = (SaveFileFormat?)json.Value<long?>(nameof(SaveFileFormat)) ?? SaveFileFormat;
        DestructibleMeshTreatment = (DestructibleMeshTreatment?)json.Value<long?>(nameof(DestructibleMeshTreatment)) ?? DestructibleMeshTreatment;
        
        SupportModdedResources = json.Value<bool?>(nameof(SupportModdedResources)) ?? SupportModdedResources;
        AutoUpdate = json.Value<bool?>(nameof(AutoUpdate)) ?? AutoUpdate;
        DidUpdate = json.Value<bool?>(nameof(DidUpdate)) ?? DidUpdate;
        GameRunningDuringUpdate = json.Value<bool?>(nameof(GameRunningDuringUpdate)) ?? GameRunningDuringUpdate;

        CETInstallLocation = json.Value<string>(nameof(CETInstallLocation)) ?? CETInstallLocation;
        CacheModdedResources = json.Value<bool?>(nameof(CacheModdedResources)) ?? CacheModdedResources;
        
        WindowRecoveryState = json[nameof(WindowRecoveryState)]?.ToObject<WindowRecoveryState>() ?? WindowRecoveryState;

        CustomSelectionFilePath = json.Value<string>(nameof(CustomSelectionFilePath)) ?? CustomSelectionFilePath;
        BackupDirectory = json.Value<string>(nameof(BackupDirectory)) ?? BackupDirectory;
        MaxBackupFiles = json.Value<int?>(nameof(MaxBackupFiles)) ?? MaxBackupFiles;
        AutoScrollLogViewer = json.Value<bool?>(nameof(AutoScrollLogViewer)) ?? AutoScrollLogViewer;
        
        RememberFailedResources = json.Value<bool?>(nameof(RememberFailedResources)) ?? RememberFailedResources;

        DebugMode = json.Value<bool?>(nameof(DebugMode)) ?? DebugMode;
    }
    
    public override string Serialize() => JsonConvert.SerializeObject(this, JsonSerializerPresets.Default);
}