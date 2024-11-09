using LightningDB;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using LightningDB.Native;
using System.Collections.Generic;
namespace VolumetricSelection2077.Services
{
    public enum CacheDatabase
    {
        FileMap,
        StreamingSectors,
        Meshes
    }

    public class CacheService : IDisposable
    {
        private readonly string _cachePath;
        private readonly string _workingPath;
        private readonly SettingsService _settings;
        private readonly LightningEnvironment _env;

        public CacheService()
        {
            _settings = SettingsService.Instance;
            string cacheDirectory = Path.Combine(_settings.CacheDirectory, "cache");
            string workingDirectory = Path.Combine(_settings.CacheDirectory, "working");
            
            Logger.Info($"Initializing cache at: {cacheDirectory}");
            Logger.Info($"Initializing working directory at: {workingDirectory}");

            try
            {
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                    Logger.Info("Created cache directory");
                }
                if (!Directory.Exists(workingDirectory))
                {
                    Directory.CreateDirectory(workingDirectory);
                    Logger.Info("Created working directory");
                }
                
                _cachePath = cacheDirectory;
                _workingPath = workingDirectory;

                _env = new LightningEnvironment(_cachePath)
                {
                    MaxDatabases = 10,
                    MapSize = 10485760 * 100  // 1GB
                };
                
                Logger.Info("Opening LMDB environment...");
                _env.Open();
                Logger.Success("LMDB environment opened successfully");

                // Create initial database to ensure it works
                using (var tx = _env.BeginTransaction())
                {
                    using var db = tx.OpenDatabase("FileMap", new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create });
                    Logger.Info("Initial database created successfully");
                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to initialize cache: {ex.Message}");
                throw; // Re-throw to prevent partially initialized service
            }
        }


        private bool IsValidDatabase(string database)
        {
            return Enum.TryParse<CacheDatabase>(database, true, out _);
        }

        public (bool success, string error) SaveEntry(string database, string keyname, byte[] data)
        {
            if (!IsValidDatabase(database))
            {
                return (false, $"Invalid database name: {database}");
            }
            // Add validation for empty keys and data
            if (string.IsNullOrEmpty(keyname))
            {
                Logger.Error("Cannot save entry with empty key");
                return (false, "Empty key is not allowed");
            }

            if (data == null || data.Length == 0)
            {
                Logger.Error($"Cannot save empty data for key: {keyname}");
                return (false, "Empty data is not allowed");
            }

            try
            {   
                using var tx = _env.BeginTransaction();
                using var db = tx.OpenDatabase(database, new DatabaseConfiguration 
                { 
                    Flags = DatabaseOpenFlags.Create 
                });

                var keyBytes = Encoding.UTF8.GetBytes(keyname);
                tx.Put(db, keyBytes, data);
                tx.Commit();

                // Verify the save
                using var verifyTx = _env.BeginTransaction(TransactionBeginFlags.ReadOnly);
                using var verifyDb = verifyTx.OpenDatabase(database);
                var (resultCode, _, value) = verifyTx.Get(verifyDb, keyBytes);
                
                if (resultCode == MDBResultCode.Success)
                {
                    return (true, string.Empty);
                }
                else
                {
                    Logger.Error($"Save succeeded but verification failed for: {keyname}");
                    return (false, "Save verification failed");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save entry to database: {ex.Message}");
                return (false, ex.Message);
            }
        }

        public (bool exists, byte[]? data, string error) GetEntry(string database, string keyname)
        {
            if (!IsValidDatabase(database))
            {
                return (false, null, $"Invalid database name: {database}");
            }

            try
            {
                using var tx = _env.BeginTransaction(TransactionBeginFlags.ReadOnly);
                using var db = tx.OpenDatabase(database);

                var keyBytes = Encoding.UTF8.GetBytes(keyname);
                var (resultCode, _, value) = tx.Get(db, keyBytes);
                
                if (resultCode != MDBResultCode.Success)
                {
                    return (false, null, "Entry not found");
                }

                return (true, value.CopyToNewArray(), string.Empty);
            }
            catch (LightningException ex) when (ex.Message.Contains("not found"))
            {
                return (false, null, "Database not found");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get entry from database: {ex.Message}");
                return (false, null, ex.Message);
            }
        }

        public bool DropDatabase(string database)
        {
            if (!IsValidDatabase(database))
            {
                Logger.Error($"Invalid database name: {database}");
                return false;
            }

            try
            {
                using var tx = _env.BeginTransaction();
                try
                {
                    // Try to open the database first to check if it exists
                    using var db = tx.OpenDatabase(database);
                    
                    // Drop the database
                    tx.DropDatabase(db);
                    tx.Commit();
                    
                    Logger.Success($"Successfully dropped database: {database}");
                    return true;
                }
                catch (LightningException ex) when (ex.Message.Contains("not found"))
                {
                    Logger.Info($"Database {database} doesn't exist, nothing to drop");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to drop database: {ex.Message}");
                return false;
            }
        }
        public (bool success, string error) SaveBatch(string database, IEnumerable<(string key, byte[] value)> entries)
        {
            if (!IsValidDatabase(database))
            {
                return (false, $"Invalid database name: {database}");
            }

            try
            {
                using var tx = _env.BeginTransaction();
                using var db = tx.OpenDatabase(database, new DatabaseConfiguration 
                { 
                    Flags = DatabaseOpenFlags.Create 
                });

                foreach (var (key, value) in entries)
                {
                    var keyBytes = Encoding.UTF8.GetBytes(key);
                    tx.Put(db, keyBytes, value);
                }

                tx.Commit();
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public void Dispose()
        {
            _env?.Dispose();
        }
    }
}

