using System;
using System.Collections.Generic;
using System.Text.Json;
using VolumetricSelection2077.Models;
namespace VolumetricSelection2077.Services
{
    public class PreprocessingService
    {
        private Dictionary<string, JsonDocument>? ParseJson(Dictionary<string, byte[]> data)
        {
            if (data == null || data.Count == 0)
            {
                Logger.Error("No data provided to parse");
                return null;
            }

            Dictionary<string, JsonDocument> parsedJson = new();

            try
            {
                foreach (var (key, value) in data)
                {
                    try
                    {
                        parsedJson[key] = JsonDocument.Parse(value);
                    }
                    catch (JsonException ex)
                    {
                        Logger.Error($"Failed to parse JSON for key {key}: {ex.Message}");
                        // Clean up already parsed documents if you want to fail on any error
                        foreach (var doc in parsedJson.Values)
                        {
                            doc.Dispose();
                        }
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error while parsing JSON: {ex.Message}");
                // Clean up
                foreach (var doc in parsedJson.Values)
                {
                    doc.Dispose();
                }
                return null;
            }

            return parsedJson;
        }

        public byte[]? PreprocessStreamingSector(Dictionary<string, byte[]> data)
        {
            var parsedJson = ParseJson(data);
            if (parsedJson == null)
            {
                return null;
            }
            var sectorMPack = new StreamingSector();
            foreach (var (key, value) in parsedJson)
            {
                Logger.Info($"Processing {key}");
                Logger.Info(value.RootElement.GetProperty("Data").ToString());
            }

            return null;
        }
    }
}
