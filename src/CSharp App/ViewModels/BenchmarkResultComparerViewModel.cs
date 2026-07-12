using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace VolumetricSelection2077.ViewModels;

public enum BenchmarkResultSide { Left, Right }

public class BenchmarkResult
{
    public string Name { get; set; }
    public Dictionary<string, List<TimeSpan>> Times { get; set; }
    public TimeSpan TotalTime => Times.Values.SelectMany(x => x).Aggregate(TimeSpan.Zero, (current, time) => current + time);

    private BenchmarkResult? _other;
    public string Formatted
    {
        get
        {
            if (_other == null)
                return FormatSelf();
            return FormatDiff();
        }
    }

    public BenchmarkResult(string name, Dictionary<string, List<TimeSpan>> times, BenchmarkResult? other = null)
    {
        Name = name;
        Times = times;
        _other = other;
    }

    private string FormatSelf()
    {
        var s = $"Sum\n{TotalTime.TotalMilliseconds}\n";
            
        foreach (var br in Times)
        {
            s += $"\n{br.Key}\n";
            foreach (var time in br.Value)
            {
                s += $"{time.TotalMilliseconds}\n";
            }
        }
        return s;
    }

    private string FormatDiff()
    {
        if (_other == null)
            return "";
        
        var s = $"Sum\n{TotalTime.TotalMilliseconds} ({(TotalTime.TotalMilliseconds / _other.TotalTime.TotalMilliseconds * 100):F}%)\n";
            
        foreach (var (key, value) in Times)
        {
            s += $"\n{key}\n";
            for (var i = 0; i < value.Count; i++)
            {
                var time = value[i];
                var otherTime = _other.Times[key][i];
                s += $"{time.TotalMilliseconds} ({(time.TotalMilliseconds / otherTime.TotalMilliseconds * 100):F}%)\n";
            }
        }
        return s;
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
    [ObservableProperty] private BenchmarkResult _diff;

    public BenchmarkResultComparerViewModel()
    {
        SelectedResultLeft = new BenchmarkResult("Left", new Dictionary<string, List<TimeSpan>>());
        SelectedResultRight = new BenchmarkResult("Right", new Dictionary<string, List<TimeSpan>>());
        Diff = new BenchmarkResult("Diff", new Dictionary<string, List<TimeSpan>>());
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

        Diff = CalcDiff(SelectedResultLeft, SelectedResultRight);
    }
    
    private BenchmarkResult CalcDiff(BenchmarkResult left, BenchmarkResult right)
    {
        var dict = new Dictionary<string, List<TimeSpan>>();
        foreach (var (key, value) in right.Times)
            dict.Add(key, value);

        foreach (var (key, value) in left.Times)
        {
            var list = TryGetOrCreateValue(dict, key);
            FillList(list, Math.Max(list.Count, value.Count));
            for (var i = 0; i < list.Count; i++)
                list[i] -= value[i];
        }
        
        foreach (var (key, value) in right.Times)
        {
            var list = TryGetOrCreateValue(dict, key);
            FillList(list, Math.Max(list.Count, value.Count));
        }
        
        return new BenchmarkResult("diff", dict, SelectedResultLeft);
    }

    private List<TimeSpan> TryGetOrCreateValue(Dictionary<string, List<TimeSpan>> dict, string key)
    {
        if (dict.TryGetValue(key, out var list))
            return list;
        
        dict.Add(key, []);
        return dict[key];
    }

    private void FillList(List<TimeSpan> list, int targetLength)
    {
        if (list.Count >= targetLength)
            return;
        
        while (list.Count < targetLength)
            list.Add(TimeSpan.Zero);
    }
}