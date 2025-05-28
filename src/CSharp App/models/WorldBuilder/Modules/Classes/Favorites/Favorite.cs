using System.Collections.Generic;
using Newtonsoft.Json;

namespace VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Favorites
{
    public class Favorite
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("tags")]
        public Dictionary<string, bool> Tags { get; set; }
        
        [JsonProperty("data")]
        public object? Data { get; set; } // Should be Positionable
        
        [JsonProperty("category")]
        public Category? Category { get; set; }
        
        [JsonProperty("icon")]
        public string Icon { get; set; }
        
        [JsonProperty("favoritesUI")]
        public object? FavoritesUI { get; set; }
        
        [JsonProperty("spawnUI")]
        public object? SpawnUI { get; set; }
        
        public Favorite()
        {
            Name = "New Favorite";
            Tags = new Dictionary<string, bool>();
            Data = null;
            Category = null;
            Icon = string.Empty;
            FavoritesUI = null;
            SpawnUI = null;
        }
    }
}
