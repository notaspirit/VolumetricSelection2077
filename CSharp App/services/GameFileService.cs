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
        public async void buildFileMap()
        {
            // if the filemap already exists return
            string ep1ArchivePath = Path.Combine(_settings.GameDirectory, "archive", "pc", "ep1");
            string contentArchivePath = Path.Combine(_settings.GameDirectory, "archive", "pc", "content");

            if (!Directory.Exists(ep1ArchivePath) && !Directory.Exists(contentArchivePath))
            {
                Logger.Error("No archive found, please select the correct game directory");
                return;
            }
            Logger.Info("Building filemap...");

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
                    await _cacheService.SaveEntry("FileMap", contentFile, BitConverter.GetBytes(archiveIndex));

                }
                string contentArchiveFilePath = Path.Combine("archive", "pc", "content", Path.GetFileName(contentArchiveFile));
                await _cacheService.SaveEntry("FileMap", archiveIndex.ToString(), Encoding.UTF8.GetBytes(contentArchiveFilePath));
                archiveIndex++;
            }
        }
        public async void GetFiles()
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
