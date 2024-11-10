using System;
using System.Text.Json;

namespace VolumetricSelection2077.Services
{
    public class PreprocessingService
    {
        private JsonDocument? ParseJson(byte[] data)
        {
            JsonDocument? streamingSectorJson = null;

            try
            {
                streamingSectorJson = JsonDocument.Parse(data);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to deserialize StreamingSector: {ex.Message}");
                return null;
            }
            return streamingSectorJson;
        }
        public byte[]? PreprocessStreamingSector(byte[] data)
        {
            var streamingSectorJson = ParseJson(data);
            if (streamingSectorJson == null)
            {
                return null;
            }
            
            return null;
        }
    }
}
