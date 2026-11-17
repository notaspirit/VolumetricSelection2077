using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using VolumetricSelection2077.App.Services;
using VolumetricSelection2077.Core.Json.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.App.Enums;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Extensions;

namespace VolumetricSelection2077.App.Models.SettingsDTO;

public partial class SettingsDTOV1 : SettingsDTO
{
    [ObservableProperty] private string _gameDirectory;
    
    [JsonIgnore]
    public string GameExecutable => Path.Combine(GameDirectory, "bin", "x64", "Cyberpunk2077.exe");
    [ObservableProperty] private bool _cacheEnabled;
    [ObservableProperty] private string _cacheDirectory;
    [ObservableProperty] private SaveFileLocation _saveFileLocation;
    [ObservableProperty] private string _outputDirectory;
    [ObservableProperty] private string _outputFilename;
    [ObservableProperty] private bool _supportModdedResources;

    [ObservableProperty] private HashSet<string> _nodeTypeFilter;

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
    [ObservableProperty] private string _CETInstallLocation;
    [ObservableProperty] private bool _cacheModdedResources;
    [ObservableProperty] private WindowRecoveryState _windowRecoveryState;
    [ObservableProperty] private string _customSelectionFilePath;
    [ObservableProperty] private string _backupDirectory;
    [ObservableProperty] private SaveFileFormat _saveFileFormat;
    [ObservableProperty] private int _maxBackupFiles;
    [ObservableProperty] private bool _autoScrollLogViewer;
    
    [ObservableProperty] private bool _debugMode;
    
    
    public SettingsDTOV1()
    {
        DTOVersion = 1;
        
        GameDirectory = "";
        CacheEnabled = true;
        CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "cache");
        SaveFileLocation = SaveFileLocation.GameDirectory;
        OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "output");
        OutputFilename = "";
        NodeTypeFilter = [.. AppUtils.WorldNodeNames];
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

        DebugMode = false;
    }
    
    public override void Deserialize(string json) => Deserialize(JObject.Parse(json));

    public override void Deserialize(JObject json) => json.Populate(this, JsonSerializer.Create(JsonSerializerPresets.Default));
    
    public override string Serialize() => JsonConvert.SerializeObject(this, JsonSerializerPresets.Default);
}