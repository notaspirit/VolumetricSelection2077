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
            // if the filemap already exists return
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
                var contentFiles = await _wolvenkitCLIService.ListFilesInArchiveFile(contentArchiveFile, _wolvenkitCLISettings.AllExtensionsRegex);
                Logger.Info($"Found {contentFiles.Count} files in {contentArchiveFile}");
                foreach (var contentFile in contentFiles)
                {
                    _cacheService.SaveEntry(CacheDatabase.FileMap.ToString(), contentFile, BitConverter.GetBytes(archiveIndex));

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
                var ep1Files = await _wolvenkitCLIService.ListFilesInArchiveFile(ep1ArchiveFile, _wolvenkitCLISettings.AllExtensionsRegex);
                Logger.Info($"Found {ep1Files.Count} files in {ep1ArchiveFile}");
                foreach (var ep1File in ep1Files)
                {
                    _cacheService.SaveEntry(CacheDatabase.FileMap.ToString(), ep1File, BitConverter.GetBytes(archiveIndex));

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
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;
            var sectorsElement = root[1];
            var sectors = sectorsElement.EnumerateArray()
                    .Select(s => s.GetString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

            foreach (var sector in sectors)
            {
                if (sector == null)
                {
                    Logger.Error("Sector is null");
                    continue;
                }
                var (exists, data, error) = _cacheService.GetEntry("FileMap", sector.ToString());
                if (exists && data != null)
                {
                    Logger.Info(Encoding.UTF8.GetString(data));
                }
                else
                {
                    Logger.Error($"Sector {sector} not found in cache");
                }
            }
        }
    }   
}
