namespace VolumetricSelection2077.Models;

public class FileSize
{
    public ulong Bytes { get; }

    public FileSize(ulong bytes)
    {
        Bytes = bytes;
    }
    
    public double ToKilobytes() => Bytes / 1024.0;
    public double ToMegabytes() => Bytes / (1024.0 * 1024);
    public double ToGigabytes() => Bytes / (1024.0 * 1024 * 1024);

    public string GetFormattedSize()
    {
        if (Bytes >= 1024 * 1024 * 1024)
            return $"{ToGigabytes():0.##} GB";
        if (Bytes >= 1024 * 1024)
            return $"{ToMegabytes():0.##} MB";
        if (Bytes >= 1024)
            return $"{ToKilobytes():0.##} KB";
        return $"{Bytes} B";
    }
}