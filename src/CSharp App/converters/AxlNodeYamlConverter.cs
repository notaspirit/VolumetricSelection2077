using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using VolumetricSelection2077.Models;

namespace VolumetricSelection2077.Converters
{
    public class AxlNodeDeletionTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(AxlNodeDeletion);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
        {
            var dict = nestedObjectDeserializer(typeof(Dictionary<string, object>)) as Dictionary<string, object>;
            Type targetType;
            if (dict.ContainsKey("actorDeletions") && dict.ContainsKey("expectedActors"))
            {
                targetType = typeof(AxlCollisionNodeDeletion);
            }
            else if (dict.ContainsKey("instanceDeletions") && dict.ContainsKey("expectedInstances"))
            {
                targetType = typeof(AxlInstancedNodeDeletion);
            }
            else
            {
                targetType = typeof(AxlNodeDeletion);
            }

            var deserializerBuilder = new DeserializerBuilder().IgnoreUnmatchedProperties();
            var safeDeserializer = deserializerBuilder.Build();

            var serializer = new SerializerBuilder().Build();
            var yamlString = serializer.Serialize(dict);
            
            return safeDeserializer.Deserialize(yamlString, targetType);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer nestedObjectSerializer)
        {
            nestedObjectSerializer(value);
        }
    }

    public class AxlNodeMutationTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(AxlNodeMutation);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
        {
            var dict = nestedObjectDeserializer(typeof(Dictionary<string, object>)) as Dictionary<string, object>;

            if (dict == null)
                return null;

            Type targetType;
            if (dict.ContainsKey("nbNodesUnderProxyDiff"))
            {
                targetType = typeof(AxlProxyNodeMutationMutation);
            }
            else
            {
                targetType = typeof(AxlNodeMutation);
            }
            
            var deserializerBuilder = new DeserializerBuilder().IgnoreUnmatchedProperties();
            var safeDeserializer = deserializerBuilder.Build();

            var serializer = new SerializerBuilder().Build();
            var yamlString = serializer.Serialize(dict);
            
            return safeDeserializer.Deserialize(yamlString, targetType);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer nestedObjectSerializer)
        {
            nestedObjectSerializer(value);
        }
    }
}
