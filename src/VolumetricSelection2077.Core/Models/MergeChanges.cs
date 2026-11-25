using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.Core.Models;

public class MergeChanges
{
    public int NewSectors { get; set; }
    public int NewNodes { get; set; }
    public int NewActors { get; set; }
    
    public bool HasChanges => NewSectors > 0 || NewNodes > 0 || NewActors > 0;
    
    public string ToSubtractionString()
    {
        var s = new List<string>();
        if (NewSectors > 0)
            s.Add($"Removed sectors: {NewSectors}");
        if (NewNodes > 0)
            s.Add($"Removed nodes: {NewNodes}");
        if (NewActors > 0)
            s.Add($"Removed actors: {NewActors}");
        
        if (s.Count == 0)
            return "No removed changes";
        
        return string.Join(", ", s);
    }
    
    public string ToAdditionString()
    {
        var s = new List<string>();
        if (NewSectors > 0)
            s.Add($"New sectors: {NewSectors}");
        if (NewNodes > 0)
            s.Add($"New nodes: {NewNodes}");
        if (NewActors > 0)
            s.Add($"New actors: {NewActors}");
        
        if (s.Count == 0)
            return "No added changes";
        
        return string.Join(", ", s);
    }
}