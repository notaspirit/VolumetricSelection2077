using System.Text.Json;
using System.Text.Json.Nodes;
using VolumetricSelection2077.Models;
using SharpDX;
using System.Collections.Generic;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Parsers;

public class SelectionParser
{
    public static (bool, string, SelectionInput?) ParseSelection(string input)
    {
        JsonObject? jsonInput = JsonSerializer.Deserialize<JsonObject>(input);
    
        float? rotX = jsonInput?["box"]?["rotation"]?["x"]?.GetValue<float>();
        float? rotY = jsonInput?["box"]?["rotation"]?["y"]?.GetValue<float>();
        float? rotZ = jsonInput?["box"]?["rotation"]?["z"]?.GetValue<float>();

        if (!rotX.HasValue || !rotY.HasValue || !rotZ.HasValue)
        {
            return (false, "No rotation values found!", null);
        }
        
        float? originX = jsonInput?["box"]?["origin"]?["x"]?.GetValue<float>();
        float? originY = jsonInput?["box"]?["origin"]?["y"]?.GetValue<float>();
        float? originZ = jsonInput?["box"]?["origin"]?["z"]?.GetValue<float>();

        if (!originX.HasValue || !originY.HasValue || !originZ.HasValue)
        {
            return (false, "No origin values found!", null);
        }
        
        float? scaleX = jsonInput?["box"]?["scale"]?["x"]?.GetValue<float>();
        float? scaleY = jsonInput?["box"]?["scale"]?["y"]?.GetValue<float>();
        float? scaleZ = jsonInput?["box"]?["scale"]?["z"]?.GetValue<float>();

        if (!scaleX.HasValue || !scaleY.HasValue || !scaleZ.HasValue)
        {
            return (false, "No scale values found!", null);
        }
        /*
        Matrix selectionBoxMatrix = Matrix.RotationYawPitchRoll(-(float)rotX, -(float)rotY, -(float)rotZ);
        OrientedBoundingBox obb = new OrientedBoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        obb.Scale(new Vector3((float)scaleX, (float)scaleY, (float)scaleZ));
        obb.Transform(selectionBoxMatrix);
        obb.Translate(new Vector3((float)originX, (float)originY, (float)originZ));
        */
        Matrix selectionBoxMatrix = Matrix.RotationYawPitchRoll(
            MathUtil.DegreesToRadians((float)rotX), MathUtil.DegreesToRadians((float)rotY), MathUtil.DegreesToRadians((float)rotZ));
        Vector3 halfScale = new Vector3((float)scaleX / 2, (float)scaleY / 2, (float)scaleZ / 2);
        OrientedBoundingBox obb =
            new OrientedBoundingBox(new Vector3(-halfScale.X, -halfScale.Y, -halfScale.Z), halfScale);
        obb.Transform(selectionBoxMatrix);
        obb.Translate(new Vector3((float)originX, (float)originY, (float)originZ));
        
        var sectorListJson = jsonInput?["sectors"] as JsonArray;
        if (sectorListJson == null)
        {
            return (false, "No sectors found!", null);
        }
        List<string> sectorList = new List<string>();
        foreach (var sector in sectorListJson)
        {
            string? sectorName = sector?.GetValue<string>();
            if (sectorName == null)
            {
                Logger.Warning("Failed to parse sector name to string! Skipping.");
                continue;
            }

            if (!sectorName.EndsWith(".streamingsector"))
            {
                Logger.Warning("Sector name does not end with streamingsector! Skipping.");
                continue;
            }
            
            sectorList.Add(sectorName);
        }
        
        if (sectorList.Count == 0)
        {
            return (false, "Sector list contains 0 entries!", null);
        }
        
        SelectionInput parsedSelection = new SelectionInput()
        {
            Obb = obb,
            Aabb = obb.GetBoundingBox(),
            Sectors = sectorList
        };
        return (true, "", parsedSelection);
    }
    
}