using System.Collections.Generic;
using Newtonsoft.Json;

namespace VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Favorites
{
    public class Category
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("icon")]
        public string Icon { get; set; }
        
        [JsonProperty("headerOpen")]
        public bool HeaderOpen { get; set; }
        
        [JsonProperty("favorites")]
        public List<Favorite> Favorites { get; set; }
        
        [JsonProperty("grouped")]
        public bool Grouped { get; set; }
        
        [JsonProperty("favoritesUI")]
        public object? FavoritesUI { get; set; }
        
        [JsonProperty("fileName")]
        public string FileName { get; set; }
        
        [JsonProperty("openPopup")]
        public bool OpenPopup { get; set; }
        
        [JsonProperty("editName")]
        public string EditName { get; set; }
        
        [JsonProperty("changeIconSearch")]
        public string ChangeIconSearch { get; set; }
        
        [JsonProperty("mergeCategorySearch")]
        public string MergeCategorySearch { get; set; }
        
        [JsonProperty("isVirtualGroup")]
        public bool IsVirtualGroup { get; set; }
        
        [JsonProperty("virtualGroupTags")]
        public List<string> VirtualGroupTags { get; set; }
        
        [JsonProperty("virtualGroups")]
        public List<Category> VirtualGroups { get; set; }
        
        [JsonProperty("virtualGroupsPS")]
        public Dictionary<string, object> VirtualGroupsPS { get; set; }
        
        [JsonProperty("virtualGroupPath")]
        public string VirtualGroupPath { get; set; }
        
        [JsonProperty("numFavoritesFiltered")]
        public int NumFavoritesFiltered { get; set; }
        
        [JsonProperty("root")]
        public Category? Root { get; set; }
        
        public Category()
        {
            Name = "New Category";
            Icon = string.Empty;
            HeaderOpen = false;
            Favorites = new List<Favorite>();
            Grouped = false;
            FavoritesUI = null;
            FileName = string.Empty;
            OpenPopup = false;
            EditName = string.Empty;
            ChangeIconSearch = string.Empty;
            MergeCategorySearch = "Merge Target";
            IsVirtualGroup = false;
            VirtualGroupTags = new List<string>();
            VirtualGroups = new List<Category>();
            VirtualGroupsPS = new Dictionary<string, object>();
            VirtualGroupPath = string.Empty;
            NumFavoritesFiltered = 0;
            Root = null;
        }
    }
}
