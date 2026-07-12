using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace VolumetricSelection2077.ViewModels;

public enum BenchmarkResultSide { Left, Right }

public enum ComparisonStatus
{
    Neutral,
    Faster,
    Slower
}

public class ComparisonItem
{
    public string Text { get; set; }
    public ComparisonStatus Status { get; set; }

    public ComparisonItem(string text, ComparisonStatus status = ComparisonStatus.Neutral)
    {
        Text = text;
        Status = status;
    }
}

public class BenchmarkResult
{
    public string Name { get; set; }
    public Dictionary<string, List<TimeSpan>> Times { get; set; }
    public TimeSpan TotalTime => Times.Values.SelectMany(x => x).Aggregate(TimeSpan.Zero, (current, time) => current + time);

    public BenchmarkResult(string name, Dictionary<string, List<TimeSpan>> times)
    {
        Name = name;
        Times = times;
    }
}

public partial class BenchmarkResultComparerViewModel : ObservableObject
{
    public List<string> ResultNames => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "BenchmarkResults"))
        .EnumerateDirectories()
        .Select(x => x.Name)
        .ToList();

    [ObservableProperty] private BenchmarkResult _selectedResultLeft;
    [ObservableProperty] private BenchmarkResult _selectedResultRight;

    [ObservableProperty] private List<ComparisonItem> _rowsLeft = new();
    [ObservableProperty] private List<ComparisonItem> _rowsRight = new();
    [ObservableProperty] private List<ComparisonItem> _rowsDiff = new();

    public BenchmarkResultComparerViewModel()
    {
        SelectedResultLeft = new BenchmarkResult("Left", new Dictionary<string, List<TimeSpan>>());
        SelectedResultRight = new BenchmarkResult("Right", new Dictionary<string, List<TimeSpan>>());
    }

    public void SetBenchmarkResult(BenchmarkResultSide side, string name)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "BenchmarkResults", name, "results.json");
        if (!File.Exists(path))
            return;
        
        var dict = JsonConvert.DeserializeObject<Dictionary<string, List<TimeSpan>>>(File.ReadAllText(path));
        if (dict == null)
            return;
        
        var target = new BenchmarkResult(name, dict);

        switch (side)
        {
            case BenchmarkResultSide.Left:
                SelectedResultLeft = target;
                break;
            case BenchmarkResultSide.Right:
                SelectedResultRight = target;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(side), side, null);
        }

        UpdateComparison();
    }

    private void UpdateComparison()
    {
        var left = SelectedResultLeft;
        var right = SelectedResultRight;

        var newLeft = new List<ComparisonItem>();
        var newRight = new List<ComparisonItem>();
        var newDiff = new List<ComparisonItem>();

        // Total Time
        newLeft.Add(new ComparisonItem("Sum"));
        newLeft.Add(new ComparisonItem($"{left.TotalTime.TotalMilliseconds:F2}"));

        newRight.Add(new ComparisonItem("Sum"));
        newRight.Add(new ComparisonItem($"{right.TotalTime.TotalMilliseconds:F2}"));

        newDiff.Add(new ComparisonItem("Sum"));
        if (left.TotalTime == TimeSpan.Zero || right.TotalTime == TimeSpan.Zero)
        {
            newDiff.Add(new ComparisonItem("-"));
        }
        else
        {
            var ratio = left.TotalTime.TotalMilliseconds / right.TotalTime.TotalMilliseconds;
            var status = ratio < 0.999 ? ComparisonStatus.Faster : (ratio > 1.001 ? ComparisonStatus.Slower : ComparisonStatus.Neutral);
            // Swapped subtraction to: Left - Right. If Left is smaller (faster), result is negative.
            newDiff.Add(new ComparisonItem($"{left.TotalTime.TotalMilliseconds - right.TotalTime.TotalMilliseconds:F2} ({(ratio - 1.0) * 100:F1}%)", status));
        }

        var allKeys = left.Times.Keys.Union(right.Times.Keys).OrderBy(x => x).ToList();

        foreach (var key in allKeys)
        {
            newLeft.Add(new ComparisonItem("")); // Spacer
            newRight.Add(new ComparisonItem(""));
            newDiff.Add(new ComparisonItem(""));

            newLeft.Add(new ComparisonItem($"--- {key} ---"));
            newRight.Add(new ComparisonItem($"--- {key} ---"));
            newDiff.Add(new ComparisonItem($"--- {key} ---"));

            var leftTimes = left.Times.TryGetValue(key, out var lt) ? lt : new List<TimeSpan>();
            var rightTimes = right.Times.TryGetValue(key, out var rt) ? rt : new List<TimeSpan>();

            var maxCount = Math.Max(leftTimes.Count, rightTimes.Count);

            for (int i = 0; i < maxCount; i++)
            {
                var lVal = i < leftTimes.Count ? leftTimes[i] : (TimeSpan?)null;
                var rVal = i < rightTimes.Count ? rightTimes[i] : (TimeSpan?)null;

                newLeft.Add(new ComparisonItem(lVal.HasValue ? $"{lVal.Value.TotalMilliseconds:F2}" : "-"));
                newRight.Add(new ComparisonItem(rVal.HasValue ? $"{rVal.Value.TotalMilliseconds:F2}" : "-"));

                if (lVal.HasValue && rVal.HasValue)
                {
                    var ratio = lVal.Value.TotalMilliseconds / rVal.Value.TotalMilliseconds;
                    var status = ratio < 0.999 ? ComparisonStatus.Faster : (ratio > 1.001 ? ComparisonStatus.Slower : ComparisonStatus.Neutral);
                    newDiff.Add(new ComparisonItem($"{lVal.Value.TotalMilliseconds - rVal.Value.TotalMilliseconds:F2} ({(ratio - 1.0) * 100:F1}%)", status));
                }
                else
                {
                    newDiff.Add(new ComparisonItem("-"));
                }
            }
        }

        RowsLeft = newLeft;
        RowsRight = newRight;
        RowsDiff = newDiff;
    }
}