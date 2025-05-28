using Newtonsoft.Json;
using VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn;

namespace VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Editor
{
    // Element holding a spawnable
    public class SpawnableElement : Positionable
    {
        [JsonProperty("spawnable")]
        public Spawnable Spawnable { get; set; }
        
        [JsonProperty("parent")]
        public PositionableGroup Parent { get; set; }
        
        [JsonProperty("silent")]
        public bool Silent { get; set; }
        
        public SpawnableElement() : base()
        {
            Name = "New Element";
            ModulePath = "modules/classes/editor/spawnableElement";
            Spawnable = null;
            Class.Add("spawnableElement");
            Expandable = false;
            Silent = false;
            // Lua: randomizationSettings = utils.combineHashTable(o.randomizationSettings, ...)
            if (RandomizationSettings != null)
            {
                RandomizationSettings["randomizeRotation"] = false;
                RandomizationSettings["randomizeRotationAxis"] = 2;
                RandomizationSettings["randomizeAppearance"] = false;
            }
        }
    }
}
