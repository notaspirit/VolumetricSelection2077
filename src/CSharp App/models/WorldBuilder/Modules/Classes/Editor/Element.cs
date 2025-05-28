using System.Collections.Generic;
using Newtonsoft.Json;
using VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Generated;

namespace VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Editor
{
    // Base class for hierarchical elements, such as groups and objects
    public class Element
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("newName")]
        public string NewName { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("parent")]
        public Element Parent { get; set; }

        [JsonProperty("childs")]
        public List<Element> Childs { get; set; }

        [JsonProperty("modulePath")]
        public string ModulePath { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("headerOpen")]
        public bool HeaderOpen { get; set; }

        [JsonProperty("propertyHeaderStates")]
        public Dictionary<string, object> PropertyHeaderStates { get; set; }

        [JsonProperty("sUI")]
        public object SUI { get; set; }

        [JsonProperty("expandable")]
        public bool Expandable { get; set; }

        [JsonProperty("hideable")]
        public bool Hideable { get; set; }

        [JsonProperty("visible")]
        public bool Visible { get; set; }
        
        [JsonProperty("hiddenByParent")]
        public bool HiddenByParent { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("class")]
        public List<string> Class { get; set; }

        [JsonProperty("hovered")]
        public bool Hovered { get; set; }

        [JsonProperty("editName")]
        public bool EditName { get; set; }

        [JsonProperty("focusNameEdit")]
        public int FocusNameEdit { get; set; }

        [JsonProperty("quickOperations")]
        public Dictionary<string, QuickOperation> QuickOperations { get; set; }

        [JsonProperty("groupOperationData")]
        public Dictionary<string, object> GroupOperationData { get; set; }
        
        [JsonProperty("selected")]
        public bool Selected { get; set; }

        public Element()
        {
            Name = "New Element";
            NewName = null;
            FileName = string.Empty;
            Parent = null;
            Childs = new List<Element>();
            ModulePath = "modules/classes/editor/element";
            Id = new System.Random().Next(1, 1000000000);
            HeaderOpen = false;
            PropertyHeaderStates = new Dictionary<string, object>();
            SUI = null;
            Expandable = true;
            Hideable = true;
            Visible = true;
            HiddenByParent = false;
            Icon = string.Empty;
            Class = new List<string> { "element" };
            Hovered = false;
            EditName = false;
            FocusNameEdit = 0;
            QuickOperations = new Dictionary<string, QuickOperation>();
            GroupOperationData = new Dictionary<string, object>();
            Selected = false;
        }
    }
}
