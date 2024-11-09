using VolumetricSelection2077.Services;
using System;
using System.Collections.Generic;
using System.IO;
using WolvenKit.Core.Interfaces;
using WolvenKit.RED4.CR2W;
using WolvenKit.RED4.Types;
using WolvenKit.RED4.Archive.IO;
using VolumetricSelection2077.Resources;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;

namespace VolumetricSelection2077.Services
{
    public class GameFileService
    {
        private readonly SettingsService _settings;
        private readonly WolvenkitCLIService _wolvenkitCLIService;
        private readonly WolvenkitCLISettings _wolvenkitCLISettings;
        private readonly CacheService _cacheService;
        public GameFileService()
        {
            _settings = SettingsService.Instance;
            _wolvenkitCLIService = new WolvenkitCLIService();
            _wolvenkitCLISettings = new WolvenkitCLISettings();
            _cacheService = new CacheService();
        }
        // Functions: 
        //- build filemap -> builds a map of the relevant game files in the archive files, and uses lmdb to store them (will get it's own caching service)
        //- get file -> gets a file from the game archive (checks cache first, if not cached, fetches from archive) returns the file as CR2W, or the glb if it's a mesh
        // after that deletes the file form disk that the cli created
        
        private void DropFileMap()
        {
            if (!_cacheService.DropDatabase(CacheDatabase.FileMap.ToString()))
            {
                Logger.Error("Failed to drop filemap");
            } else {
                Logger.Success("Filemap dropped");
            }
        }
        public async Task<(bool success, string error)> buildFileMap()
        {
            int totalProcessedEntries = 0;
            var (exists, value, error) = _cacheService.GetEntry(CacheDatabase.FileMap.ToString(), "completed");
            if (exists && error == "" && value != null)
            {
                {
                    try
                    {
                        bool isCompleted = BitConverter.ToBoolean(value, 0);
                        if (isCompleted)
                        {
                            Logger.Info("Filemap already exists, skipping...");
                            return (true, string.Empty);
                        }
                        Logger.Info("Filemap exists but is marked as incomplete, rebuilding...");
                        DropFileMap();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to get filemap completed value: {ex.Message}");
                        Logger.Info("Will attempt to rebuild filemap...");
                        DropFileMap();
                    }
                }
            } else {
                Logger.Info("Filemap does not exist, building...");
                DropFileMap();
            }
            string ep1ArchivePath = Path.Combine(_settings.GameDirectory, "archive", "pc", "ep1");
            string contentArchivePath = Path.Combine(_settings.GameDirectory, "archive", "pc", "content");

            if (!Directory.Exists(ep1ArchivePath) && !Directory.Exists(contentArchivePath))
            {
                Logger.Error("No archive found, please select the correct game directory");
                return (false, "No archive found, please select the correct game directory");
            }

            int archiveIndex = 0;

            Logger.Info("Processing content directory...");
            var contentArchiveFiles = Directory.GetFiles(contentArchivePath, "*.archive");
            Logger.Info($"Found {contentArchiveFiles.Length} content archive files");
            foreach (var contentArchiveFile in contentArchiveFiles)
            {
                if (!RelevantArchiveFiles.IsRelevant(Path.GetFileName(contentArchiveFile)))
                {
                    Logger.Info($"Skipping {contentArchiveFile} as it is not a relevant archive file");
                    continue;
                }
                int processedEntryCount = 0;
                int batchSize = 5000;
                var contentFiles = await _wolvenkitCLIService.ListFilesInArchiveFile(contentArchiveFile, _wolvenkitCLISettings.AllExtensionsRegex);
                int contentFileCount = contentFiles.Count - 1;
                if (contentFileCount <= 0)
                {
                    Logger.Info($"No files found in {contentArchiveFile}");
                    continue;
                }
                Logger.Info($"Found {contentFileCount} files in {contentArchiveFile}");
                // Process in chunks
                foreach (var chunk in contentFiles.Where(f => !string.IsNullOrEmpty(f))
                                            .Chunk(batchSize))
                {
                    var entries = chunk.Select(file => (
                        key: file,
                        value: BitConverter.GetBytes(archiveIndex)
                    ));

                    var (success, error2) = _cacheService.SaveBatch(CacheDatabase.FileMap.ToString(), entries);
                    if (!success)
                    {
                        Logger.Error($"Failed to save batch: {error2}");
                        return (false, error2);
                    }

                    processedEntryCount += chunk.Length;
                    totalProcessedEntries += chunk.Length;
                    Logger.Info($"Processed {processedEntryCount:N0} entries...");
                    Logger.Info($"Total processed entries: {totalProcessedEntries:N0}");
                    await Task.Delay(10);
                }

                string contentArchiveFilePath = Path.Combine("archive", "pc", "content", Path.GetFileName(contentArchiveFile));
                _cacheService.SaveEntry(CacheDatabase.FileMap.ToString(), archiveIndex.ToString(), Encoding.UTF8.GetBytes(contentArchiveFilePath));
                archiveIndex++;
            }
            Logger.Info("Processing ep1 directory...");
            var ep1ArchiveFiles = Directory.GetFiles(ep1ArchivePath, "*.archive");
            Logger.Info($"Found {ep1ArchiveFiles.Length} ep1 archive files");
            foreach (var ep1ArchiveFile in ep1ArchiveFiles)
            {
                if (!RelevantArchiveFiles.IsRelevant(Path.GetFileName(ep1ArchiveFile)))
                {
                    Logger.Info($"Skipping {ep1ArchiveFile} as it is not a relevant archive file");
                    continue;
                }
                int processedEntryCount = 0;
                int batchSize = 5000;
                var ep1Files = await _wolvenkitCLIService.ListFilesInArchiveFile(ep1ArchiveFile, _wolvenkitCLISettings.AllExtensionsRegex);
                int ep1FileCount = ep1Files.Count - 1;
                if (ep1FileCount <= 0)
                {
                    Logger.Info($"No files found in {ep1ArchiveFile}");
                    continue;
                }
                Logger.Info($"Found {ep1FileCount} files in {ep1ArchiveFile}");
                // Process in chunks
                foreach (var chunk in ep1Files.Where(f => !string.IsNullOrEmpty(f))
                                            .Chunk(batchSize))
                {
                    var entries = chunk.Select(file => (
                        key: file,
                        value: BitConverter.GetBytes(archiveIndex)
                    ));

                    var (success, error2) = _cacheService.SaveBatch(CacheDatabase.FileMap.ToString(), entries);
                    if (!success)
                    {
                        Logger.Error($"Failed to save batch: {error2}");
                        return (false, error2);
                    }

                    processedEntryCount += chunk.Length;
                    totalProcessedEntries += chunk.Length;
                    Logger.Info($"Processed {processedEntryCount:N0} entries...");
                    Logger.Info($"Total processed entries: {totalProcessedEntries:N0}");
                    await Task.Delay(10);
                }

                string ep1ArchiveFilePath = Path.Combine("archive", "pc", "ep1", Path.GetFileName(ep1ArchiveFile));
                _cacheService.SaveEntry(CacheDatabase.FileMap.ToString(), archiveIndex.ToString(), Encoding.UTF8.GetBytes(ep1ArchiveFilePath));
                archiveIndex++;
            }
            _cacheService.SaveEntry(CacheDatabase.FileMap.ToString(), "completed", BitConverter.GetBytes(true));
            Logger.Success("Filemap build complete");
            return (true, string.Empty);
        }
        public void GetFiles()
        {
            string selectionFilePath = Path.Combine(_settings.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077", "data", "selection.json");
            string jsonString = File.ReadAllText(selectionFilePath);
            var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;
            var sectorsElement = root[1];
            
            var sectors = sectorsElement.EnumerateArray()
                    .Select(s => s.GetString()?.Trim())  // Trim any whitespace
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

            foreach (var sector in sectors)
            {
                if (sector == null) continue;

                // Trim any control characters (like CR/LF) for the lookup
                var cleanSector = new string(sector.Where(c => !char.IsControl(c)).ToArray());
                
                Logger.Info($"Looking up sector: '{cleanSector}'");
                var (exists, data, error) = _cacheService.GetEntry("FileMap", cleanSector);
                if (exists && data != null)
                {
                    Logger.Info($"Found: {BitConverter.ToInt32(data)}");
                    var (exists2, data2, error2) = _cacheService.GetEntry(CacheDatabase.FileMap.ToString(), BitConverter.ToInt32(data).ToString());
                    if (exists2 && data2 != null)
                    {
                        Logger.Info($"Archive file: {Encoding.UTF8.GetString(data2)}");
                    } else {
                        Logger.Error($"Failed to get archive file: {error2}");
                    }
                }
                else
                {
                    Logger.Error($"Not found in cache: {error}");
                }
            }
        }
    }
}
       

