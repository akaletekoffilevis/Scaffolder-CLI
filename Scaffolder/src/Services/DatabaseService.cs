using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Scaffolder.Services;

public static class DatabaseService
{
    private static readonly object _lock = new();
    private static Dictionary<string, Dictionary<string, string>>? _cache;

    private const int KeySize = 32;
    private const int IvSize = 16;
    private const int Iterations = 100_000;

    private static byte[] DeriveKey()
    {
        var machineId = Environment.MachineName + Environment.UserName + Environment.OSVersion.Platform;
        var salt = Encoding.UTF8.GetBytes("Scaffolder-LocalDB-Salt-v1");
        using var derive = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(machineId), salt, Iterations, HashAlgorithmName.SHA256);
        return derive.GetBytes(KeySize);
    }

    public static void Initialize()
    {
        Directory.CreateDirectory(Paths.ConfigDir);

        if (!File.Exists(Paths.DatabaseFile))
        {
            var empty = new Dictionary<string, Dictionary<string, string>>
            {
                ["settings"] = new()
                {
                    ["firstRun"] = "false",
                    ["theme"] = "default",
                    ["experience"] = "beginner",
                    ["db.version"] = "1",
                    ["db.created"] = DateTime.UtcNow.ToString("o")
                },
                ["templates"] = new(),
                ["plugins"] = new(),
                ["history"] = new(),
                ["favorites"] = new()
            };
            SaveDatabase(empty);
            ConsoleService.Debug("Base de donnees initialisee.");
        }

        _cache = null;
    }

    public static Dictionary<string, Dictionary<string, string>> LoadDatabase()
    {
        lock (_lock)
        {
            if (_cache != null) return _cache;

            if (!File.Exists(Paths.DatabaseFile))
            {
                Initialize();
                return _cache!;
            }

            try
            {
                var encrypted = File.ReadAllBytes(Paths.DatabaseFile);
                var json = Decrypt(encrypted);
                _cache = JsonSerializer.Deserialize(
                    json, JsonContext.Default.DictionaryStringDictionaryStringString) ?? [];
                return _cache;
            }
            catch (CryptographicException)
            {
                ConsoleService.Warning("Base de donnees corrompue. Reinitialisation...");
                Initialize();
                return _cache!;
            }
            catch
            {
                ConsoleService.Warning("Erreur de lecture de la base. Reinitialisation...");
                Initialize();
                return _cache!;
            }
        }
    }

    public static void SaveDatabase(Dictionary<string, Dictionary<string, string>> data)
    {
        lock (_lock)
        {
            _cache = data;
            var json = JsonSerializer.Serialize(data, JsonContext.Default.DictionaryStringDictionaryStringString);
            var encrypted = Encrypt(json);
            File.WriteAllBytes(Paths.DatabaseFile, encrypted);
        }
    }

    public static string? GetValue(string section, string key)
    {
        var db = LoadDatabase();
        if (db.TryGetValue(section, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        return null;
    }

    public static void SetValue(string section, string key, string value)
    {
        var db = LoadDatabase();
        if (!db.TryGetValue(section, out var dict))
        {
            dict = new Dictionary<string, string>();
            db[section] = dict;
        }
        dict[key] = value;
        SaveDatabase(db);
    }

    public static Dictionary<string, string> GetSection(string section)
    {
        var db = LoadDatabase();
        return db.TryGetValue(section, out var dict) ? new Dictionary<string, string>(dict) : [];
    }

    public static void SetSection(string section, Dictionary<string, string> values)
    {
        var db = LoadDatabase();
        db[section] = values;
        SaveDatabase(db);
    }

    public static void RemoveValue(string section, string key)
    {
        var db = LoadDatabase();
        if (db.TryGetValue(section, out var dict))
        {
            dict.Remove(key);
            SaveDatabase(db);
        }
    }

    public static void RemoveSection(string section)
    {
        var db = LoadDatabase();
        db.Remove(section);
        SaveDatabase(db);
    }

    public static void AddToHistory(string projectName, string template, string outputDir)
    {
        var db = LoadDatabase();
        if (!db.TryGetValue("history", out var history))
        {
            history = new Dictionary<string, string>();
            db["history"] = history;
        }

        var key = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var value = $"{{\"name\":\"{projectName}\",\"template\":\"{template}\",\"dir\":\"{outputDir}\",\"date\":\"{DateTime.UtcNow:O}\"}}";
        history[key] = value;

        if (history.Count > 100)
        {
            var sorted = history.OrderBy(kv => kv.Key).Take(50).Select(kv => kv.Key).ToList();
            foreach (var k in sorted) history.Remove(k);
        }

        SaveDatabase(db);
    }

    public static void AddFavorite(string name, string template)
    {
        var db = LoadDatabase();
        if (!db.TryGetValue("favorites", out var favs))
        {
            favs = new Dictionary<string, string>();
            db["favorites"] = favs;
        }
        favs[name] = template;
        SaveDatabase(db);
    }

    public static Dictionary<string, string> GetFavorites()
    {
        return GetSection("favorites");
    }

    public static void RemoveFavorite(string name)
    {
        RemoveValue("favorites", name);
    }

    public static void AddPlugin(string name, string source)
    {
        SetValue("plugins", name, source);
    }

    public static void RemovePlugin(string name)
    {
        RemoveValue("plugins", name);
    }

    public static Dictionary<string, string> GetPlugins()
    {
        return GetSection("plugins");
    }

    public static void ExportToFile(string filePath)
    {
        var db = LoadDatabase();
        var json = JsonSerializer.Serialize(db, JsonContext.Default.DictionaryStringDictionaryStringString);
        File.WriteAllText(filePath, json);
        ConsoleService.Success($"Base exportee vers {filePath}");
    }

    public static bool ImportFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            ConsoleService.Error($"Fichier introuvable : {filePath}");
            return false;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize(json, JsonContext.Default.DictionaryStringDictionaryStringString);
            if (data == null)
            {
                ConsoleService.Error("Fichier invalide.");
                return false;
            }

            SaveDatabase(data);
            ConsoleService.Success($"Base importee depuis {filePath} ({data.Count} sections).");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleService.Error($"Erreur d'import : {ex.Message}");
            return false;
        }
    }

    public static void Reset()
    {
        if (File.Exists(Paths.DatabaseFile))
            File.Delete(Paths.DatabaseFile);
        _cache = null;
        Initialize();
        ConsoleService.Success("Base de donnees reinitialisee.");
    }

    public static (string Size, string Created, string Modified) GetStats()
    {
        if (!File.Exists(Paths.DatabaseFile))
            return ("0 B", "N/A", "N/A");

        var info = new FileInfo(Paths.DatabaseFile);
        var size = info.Length < 1024 ? $"{info.Length} B" :
                   info.Length < 1024 * 1024 ? $"{info.Length / 1024.0:F1} KB" :
                   $"{info.Length / (1024.0 * 1024.0):F1} MB";

        var db = LoadDatabase();
        var created = GetValue("settings", "db.created") ?? "N/A";
        var modified = info.LastWriteTimeUtc.ToString("o");

        return (size, created, modified);
    }

    private static byte[] Encrypt(string plainText)
    {
        var key = DeriveKey();
        var iv = new byte[IvSize];
        RandomNumberGenerator.Fill(iv);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[iv.Length + encrypted.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);
        return result;
    }

    private static string Decrypt(byte[] cipherText)
    {
        var key = DeriveKey();
        var iv = new byte[IvSize];
        var encrypted = new byte[cipherText.Length - IvSize];

        Buffer.BlockCopy(cipherText, 0, iv, 0, IvSize);
        Buffer.BlockCopy(cipherText, IvSize, encrypted, 0, encrypted.Length);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}
