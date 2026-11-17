using System;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.App.Enums.Legacy;
using VolumetricSelection2077.App.Models.SettingsDTO;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.App.Services;

public static class SettingsDtoHelper
{
    /// <summary>
    /// Deserializes the provided string into a <see cref="SettingsDTO"/> and upgrades it to the current version.
    /// </summary>
    /// <param name="jsonContent"></param>
    /// <returns>Error status when DTOVersion contains an unknown version or converter is implemented for handling the given version</returns>
    public static Result<SettingsDTOV1> SmartDeserialize(string jsonContent)
    {
        var result = new Result<SettingsDTOV1>() { Status = ResultStatus.Ok };
        var j = JObject.Parse(jsonContent);
        var version = j.Value<int>("DTOVersion");

        SettingsDTO deserializedData;
        
        switch(version)
        {
            case 0:
                deserializedData = new SettingsDTOV0();
                break;
            case 1:
                deserializedData = new SettingsDTOV1();
                break;
            default:
                result.Status = ResultStatus.Error;
                result.AddMessage($"Unknown version {version}");
                return result;
        }

        deserializedData.Deserialize(j);

        return Upgrade(deserializedData);
    }
    
    private static Result<SettingsDTOV1> Upgrade(SettingsDTO settingsDto)
    {
        var result = new Result<SettingsDTOV1>() { Status = ResultStatus.Ok };
        var current = settingsDto;

        while (current is not SettingsDTOV1)
        {
            switch (current)
            {
                case SettingsDTOV0 v0:
                    current = UpgradeV0(v0);
                    break;
                default:
                    result.Status = ResultStatus.Error;
                    result.AddMessage($"Upgrading settings dto {current.GetType().Name} is not implemented");
                    return result;
            }
        }

        result.Data = current as SettingsDTOV1;
        return result;
    }

    private static SettingsDTOV1 UpgradeV0(SettingsDTOV0 v0)
    {
        var v1 = new SettingsDTOV1();

        v1.GameDirectory = v0.GameDirectory;
        v1.CacheEnabled = v0.CacheEnabled;
        v1.CacheDirectory = v0.CacheDirectory;
        v1.SaveFileLocation = v0.SaveFileLocation;
        v1.OutputDirectory = v0.OutputDirectory;
        v1.OutputFilename = v0.OutputFilename;
        v1.SupportModdedResources = v0.SupportModdedResources;
        v1.ResourceNameFilter = v0.ResourceNameFilter;
        v1.DebugNameFilter = v0.DebugNameFilter;
        v1.FilterModeOr = v0.FilterModeOr;
        v1.IsFiltersMWVisible = v0.IsFiltersMWVisible;
        v1.IsParametersMWVisible = v0.IsParametersMWVisible;
        v1.SaveMode = v0.SaveMode;
        v1.DestructibleMeshTreatment = v0.DestructibleMeshTreatment;
        v1.AutoUpdate = v0.AutoUpdate;
        v1.DidUpdate = v0.DidUpdate;
        v1.GameRunningDuringUpdate = v0.GameRunningDuringUpdate;
        v1.CETInstallLocation = v0.CETInstallLocation;
        v1.CacheModdedResources = v0.CacheModdedResources;
        v1.WindowRecoveryState = v0.WindowRecoveryState;
        v1.CustomSelectionFilePath = v0.CustomSelectionFilePath;
        v1.BackupDirectory = v0.BackupDirectory;
        v1.SaveFileFormat = v0.SaveFileFormat;
        v1.MaxBackupFiles = v0.MaxBackupFiles;
        v1.AutoScrollLogViewer = v0.AutoScrollLogViewer;
        v1.DebugMode = v0.DebugMode;

        v1.NodeTypeFilter.Clear();

        for (var i = 0; i < v0.NodeTypeFilterProxy.Length; i++)
        {
            var enabled =  v0.NodeTypeFilterProxy[i];
            if (enabled && Enum.GetName(typeof(NodeTypeProcessingOptions), i) is { } nodeTypeName)
            {
                v1.NodeTypeFilter.Add(nodeTypeName);
            }
        }
        
        return v1;
    }
}