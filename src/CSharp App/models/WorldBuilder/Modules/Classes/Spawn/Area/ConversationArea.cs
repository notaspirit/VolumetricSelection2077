using System.Collections.Generic;
using Newtonsoft.Json;

namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Area
{
    // Class for worldConversationAreaNode
    public class ConversationArea : Area
    {
        [JsonProperty("groups")]
        public List<string> Groups;
        
        [JsonProperty("scenes")]
        public List<string> Scenes;
        
        public ConversationArea() : base()
        {
            DataType = "ConversationArea";
            ModulePath = "area/conversationArea";
            Node = "worldConversationAreaNode";
            Groups = new List<string>();
            Scenes = new List<string>();
        }
    }
}
