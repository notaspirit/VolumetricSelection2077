using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Models;
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
        
        Logger.Warning(obj["modulePath"]?.Value<string>() ?? "not found");
        
        Element element;
        if (obj["modulePath"]?.Value<string>() == "modules/classes/editor/spawnableElement")
            element = new SpawnableElement();
        else
            element = new Element();
        try
        {
            using var jsonReader = obj.CreateReader();
            serializer.Populate(jsonReader, element);
        }
        catch (Exception ex)
        {
            Logger.Warning($"{ex}");
        }
        return element;
    }

    public override void WriteJson(JsonWriter writer, Element? value, JsonSerializer serializer)
    {
        var jObject = JObject.FromObject(value, JsonSerializer.CreateDefault());
        jObject.WriteTo(writer);
    }
}