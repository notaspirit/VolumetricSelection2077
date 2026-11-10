using MessagePack;

namespace VolumetricSelection2077.App.MessagePack.Helpers;

public static class MessagePackHelper
{
    public static bool TryDeserialize<T>(byte[] data, out T result)
    {
        try
        {
            result = MessagePackSerializer.Deserialize<T>(data);
            return true;
        }
        catch
        {
            result = default!;
            return false;
        }
    }
}
