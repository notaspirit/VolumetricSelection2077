using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Helpers;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Services;


namespace VolumetricSelection2077.Converters;

public class WorldBuilderElementJsonConverter : JsonConverter<Element>
{
    public override bool CanWrite => true;

    public override Element? ReadJson(
        JsonReader reader,
        Type objectType,
        Element? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        
        Element element;
        switch (obj["modulePath"]?.Value<string>())
        {
            case "modules/classes/editor/spawnableElement":
                element = new SpawnableElement();
                break;
            case "modules/classes/editor/positionable":
                element = new Positionable();
                break;
            case "modules/classes/editor/positionableGroup":
                element = new PositionableGroup();
                break;
            default:
                element = new Element();
                break;
        }
        
        try
        {
            using var jsonReader = obj.CreateReader();
            serializer.Populate(jsonReader, element);
        }
        catch (Exception ex)
        {
            Logger.Debug($"{ex}");
        }
        return element;
    }

    public override void WriteJson(JsonWriter writer, Element? value, JsonSerializer serializer)
    {
        JsonSerializerUtils.CloneWithoutConverters(serializer).Serialize(writer, value);
    }
}