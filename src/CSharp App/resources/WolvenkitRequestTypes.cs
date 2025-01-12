using System.Collections.Generic;

namespace VolumetricSelection2077.Resources
{
    public class WkitAPITypes
    {
        public enum Types
        {
            Json,
            Ping,
            Glb,
            RefreshSettings,
            Hash
        }
        public static readonly Dictionary<Types, string> Mapping = new()
        {
            { Types.Json, "json" },
            { Types.Ping, "ping" },
            { Types.Glb, "glb" },
            { Types.RefreshSettings, "refreshSettings" },
            { Types.Hash, "hash" }
        };
    }
}