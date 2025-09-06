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
        
        float? quatRotX = jsonInput?["box"]?["rotationQuat"]?["i"]?.GetValue<float>();
        float? quatRotY = jsonInput?["box"]?["rotationQuat"]?["j"]?.GetValue<float>();
        float? quatRotZ = jsonInput?["box"]?["rotationQuat"]?["k"]?.GetValue<float>();
        float? quatRotW = jsonInput?["box"]?["rotationQuat"]?["r"]?.GetValue<float>();
        
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

        Matrix selectionBoxMatrix;
        if (quatRotX.HasValue && quatRotY.HasValue && quatRotZ.HasValue && quatRotW.HasValue)
        {
            selectionBoxMatrix = Matrix.RotationQuaternion(new Quaternion(
                (float)quatRotX, (float)quatRotY, (float)quatRotZ, (float)quatRotW));
        }
        else
        {
            selectionBoxMatrix = Matrix.RotationYawPitchRoll(
                MathUtil.DegreesToRadians((float)rotX), MathUtil.DegreesToRadians((float)rotY), MathUtil.DegreesToRadians((float)rotZ));
        }
        
        Vector3 halfScale = new Vector3((float)scaleX / 2, (float)scaleY / 2, (float)scaleZ / 2);
        OrientedBoundingBox obb =
            new OrientedBoundingBox(new Vector3(-halfScale.X, -halfScale.Y, -halfScale.Z), halfScale);
        obb.Transform(selectionBoxMatrix);
        obb.Translate(new Vector3((float)originX, (float)originY, (float)originZ));
        
        SelectionInput parsedSelection = new SelectionInput()
        {
            Obb = obb,
            Aabb = obb.GetBoundingBox(),
            Sectors = new List<string>()
        };
        return (true, "", parsedSelection);
    }
    
}