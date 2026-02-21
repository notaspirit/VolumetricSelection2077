using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.CR2W.Archive;

namespace VolumetricSelection2077.TestingStuff;

public class DumpEmbeddedFileList : IDebugTool
{
    private Progress _progress = Progress.Instance;
    private ArchiveManager am => GameFileService.Instance.ArchiveManager;
    
    public void Run()
    {
        Logger.Info("Dumping embedded file list...");
        var outPath = Path.Join(SettingsService.Instance.OutputDirectory, "embedded_files.json");

        HashSet<string> paths = [];
        foreach (var archive in am.GetGameArchives())
        {
            foreach (var file in archive.Files.Values)
            {
                // if (file.Extension != ".streamingsector") continue;
                paths.Add(file.FileName);
            }
        }
        _progress.Reset();
        _progress.SetWeight(0f, 1f, 0f);
        _progress.AddTarget(paths.Count, Progress.ProgressSections.Processing);
        _progress.AddTarget(1, Progress.ProgressSections.Startup);
        _progress.AddCurrent(1, Progress.ProgressSections.Startup);
        Logger.Info($"Processing {paths.Count} files...");
        
        var tasks = paths.Select(ProcessFile).ToArray();
        
        Task.WaitAll(tasks);

        var results = tasks.Select(task => task.Result).OfType<(string, List<string>)?>().Where(x => x is { Item2.Count: > 0 }).ToDictionary(result => result.Value.Item1, result => result.Value.Item2);

        File.WriteAllText(outPath, JsonConvert.SerializeObject(results, Formatting.Indented));
        Logger.Info($"Wrote results to {outPath}");
    }

    private async Task<(string, List<string>)?> ProcessFile(string path)
    {
        _progress.AddCurrent(1, Progress.ProgressSections.Processing);
        var cr2w = am.GetCR2WFile(path);
        if (cr2w == null)
            return null;

        return (path,
            cr2w.EmbeddedFiles.Select(x => x.FileName.ToString()).Where(x => x != null).Select(x => x!).ToList());
    }
}