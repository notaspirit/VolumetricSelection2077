using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Models;


namespace VolumetricSelection2077.Converters;

public class AxlNodeDeletionConverter : JsonConverter<AxlNodeDeletion>
{
    public override bool CanWrite => true;

    public override AxlNodeDeletion? ReadJson(
        JsonReader reader,
        Type objectType,
        AxlNodeDeletion? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);
        
        var newSerializer = new JsonSerializer();
        
        if (obj["actorDeletions"] != null && obj["expectedActors"] != null)
            return obj.ToObject<AxlCollisionNodeDeletion>(newSerializer);

        if (obj["instanceDeletions"] != null && obj["expectedInstances"] != null)
            return obj.ToObject<AxlInstancedNodeDeletion>(newSerializer);

        return obj.ToObject<AxlNodeDeletion>(newSerializer);
    }

    public override void WriteJson(JsonWriter writer, AxlNodeDeletion? value, JsonSerializer serializer)
    {
        var newSerializer = new JsonSerializer();
        
        newSerializer.Serialize(writer, value);
    }
}

public class AxlNodeMutationConverter : JsonConverter<AxlNodeMutation>
{
    public override bool CanWrite => true;

    public override AxlNodeMutation? ReadJson(
        JsonReader reader,
        Type objectType,
        AxlNodeMutation? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        var newSerializer = new JsonSerializer();

        if (obj["nbNodesUnderProxyDiff"] != null)
            return obj.ToObject<AxlProxyNodeMutationMutation>(newSerializer);

        return obj.ToObject<AxlNodeMutation>(newSerializer);
    }

    public override void WriteJson(JsonWriter writer, AxlNodeMutation? value, JsonSerializer serializer)
    {
        var newSerializer = new JsonSerializer();
        
        newSerializer.Serialize(writer, value);
    }
}
