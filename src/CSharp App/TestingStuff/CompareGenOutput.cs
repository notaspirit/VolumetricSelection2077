using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class CompareGenOutput
{

    public static void Run()
    {
        string oldBenchmarks =
            @"C:\Users\zweit\AppData\Roaming\VolumetricSelection2077\benchmarks\results\20250218080807\";
        
        string newBenchmarks =
            @"C:\Users\zweit\AppData\Roaming\VolumetricSelection2077\benchmarks\results\20250218071848\";
        
        string midSizedNew = newBenchmarks + "midSizedBuildingInSouthHeywood.xl";
        string pacificaTescNew = newBenchmarks + "pacificaTesco.xl";
        string washBoxNew = newBenchmarks + "washBoxVsEdgerunnerMansion.xl";
        
        string midSizedOld = oldBenchmarks + "midSizedBuildingInSouthHeywood.xl";
        string pacificaTescOld = oldBenchmarks + "pacificaTesco.xl";
        string washBoxOld = oldBenchmarks + "washBoxVsEdgerunnerMansion.xl";
        
        CompareJsonFiles(midSizedNew, midSizedOld);
        CompareJsonFiles(pacificaTescNew, pacificaTescOld);
        CompareJsonFiles(washBoxNew, washBoxOld);
    }
    
    /// <summary>
    /// Compares two JSON files and logs any differences
    /// </summary>
    /// <param name="file1Path">Path to the first JSON file</param>
    /// <param name="file2Path">Path to the second JSON file</param>
    /// <returns>True if differences were found, False if the files are identical</returns>
    public static bool CompareJsonFiles(string file1Path, string file2Path)
    {
        try
        {
            // Read JSON files
            string json1 = File.ReadAllText(file1Path);
            string json2 = File.ReadAllText(file2Path);

            // Parse JSON
            JToken obj1 = JToken.Parse(json1);
            JToken obj2 = JToken.Parse(json2);

            // Compare and log differences
            bool hasDifferences = false;
            List<string> differences = FindDifferences(obj1, obj2, "$");

            if (differences.Count > 0)
            {
                hasDifferences = true;
                Logger.Info($"Found {differences.Count} differences between {file1Path} and {file2Path}:");
                foreach (string diff in differences)
                {
                    Logger.Info(diff);
                }
            }
            else
            {
                Logger.Info($"The JSON files {file1Path} and {file2Path} are identical.");
            }

            return hasDifferences;
        }
        catch (FileNotFoundException ex)
        {
            Logger.Error($"Error: One of the files was not found. {ex.Message}");
            return true;
        }
        catch (JsonException ex)
        {
            Logger.Error($"Error: Invalid JSON format. {ex.Message}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error comparing JSON files: {ex.Message}");
            return true;
        }
    }

    /// <summary>
    /// Find differences between two JToken objects
    /// </summary>
    /// <param name="token1">First JToken object</param>
    /// <param name="token2">Second JToken object</param>
    /// <param name="path">Current JSON path</param>
    /// <returns>List of string descriptions of the differences</returns>
    private static List<string> FindDifferences(JToken token1, JToken token2, string path)
    {
        List<string> differences = new List<string>();

        // Compare token types
        if (token1.Type != token2.Type)
        {
            differences.Add($"Type mismatch at {path}: {token1.Type} vs {token2.Type}");
            return differences;
        }

        switch (token1.Type)
        {
            case JTokenType.Object:
                // Compare properties
                JObject obj1 = (JObject)token1;
                JObject obj2 = (JObject)token2;

                // Check for properties in obj1 that are missing or different in obj2
                foreach (var property in obj1.Properties())
                {
                    string propertyPath = $"{path}.{property.Name}";
                    if (obj2[property.Name] == null)
                    {
                        differences.Add($"Property missing in second file: {propertyPath}");
                    }
                    else
                    {
                        differences.AddRange(FindDifferences(property.Value, obj2[property.Name], propertyPath));
                    }
                }

                // Check for properties in obj2 that are missing in obj1
                foreach (var property in obj2.Properties())
                {
                    if (obj1[property.Name] == null)
                    {
                        differences.Add($"Property missing in first file: {path}.{property.Name}");
                    }
                }
                break;

            case JTokenType.Array:
                JArray array1 = (JArray)token1;
                JArray array2 = (JArray)token2;

                // Check array lengths
                if (array1.Count != array2.Count)
                {
                    differences.Add($"Array length mismatch at {path}: {array1.Count} vs {array2.Count}");
                }

                // Compare array elements (up to the common length)
                int minLength = Math.Min(array1.Count, array2.Count);
                for (int i = 0; i < minLength; i++)
                {
                    differences.AddRange(FindDifferences(array1[i], array2[i], $"{path}[{i}]"));
                }
                break;

            default:
                // Compare primitive values
                if (!JToken.DeepEquals(token1, token2))
                {
                    differences.Add($"Value mismatch at {path}: {token1} vs {token2}");
                }
                break;
        }

        return differences;
    }
}