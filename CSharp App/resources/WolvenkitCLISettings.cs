namespace VolumetricSelection2077.Resources
{
    public class WolvenkitCLISettings
    {
        public string WolvenkitCLISettingsJson { get; set; } = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft"": ""Warning"",
      ""Microsoft.Hosting.Lifetime"": ""Warning""
    }
  },
  ""XbmExportArgs"": {
    ""Flip"": false,
    ""UncookExtension"": ""dds""
  },
  ""MeshExportArgs"": {
    ""UncookExtension"": ""glb"",
    ""lodFilter"": false,
    ""WithMaterials"": false
  }
}";
        public string AllExtensionsRegex { get; set; } = @"\.(app|ent|geometry_cache|mesh|w2mesh|streamingsector)$";
    }
}