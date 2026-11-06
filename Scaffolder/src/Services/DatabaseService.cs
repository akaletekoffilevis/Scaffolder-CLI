using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Scaffolder.Services;

public static class DatabaseService
{
    private static readonly object _lock = new();
    private static Dictionary<string, Dictionary<string, string>>? _cache;

    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int Iterations = 100_000;

    // En-tete du format chiffre v2 : "SCDB" + version.
    private static readonly byte[] Magic = "SCDB"u8.ToArray();
    private const byte FormatVersion = 2;

    private static byte[] DeriveKey()
    {
        // La cle reste derivee de la machine pour rester dechiffrable entre deux executions.
        var machineId = Environment.MachineName + Environment.UserName + Environment.OSVersion.Platform;
        var salt = Encoding.UTF8.GetBytes("Scaffolder-LocalDB-Salt-v1");
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(machineId), salt, Iterations, HashAlgorithmName.SHA256, KeySize);
    }

    public static void Initialize()
    {
        lock (_lock)
        {
            EnsureConfigDirectory();

            if (!File.Exists(Paths.DatabaseFile))
            {
                var empty = new Dictionary<string, Dictionary<string, string>>
                {
                    ["settings"] = new()
                    {
                        // true = assistant de bienvenue a afficher au premier lancement.
                        ["firstRun"] = "true",
                        ["theme"] = "default",
                        ["experience"] = "beginner",
                        ["db.version"] = "2",
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

                if (!_cache.ContainsKey("settings"))
                    _cache["settings"] = new Dictionary<string, string>();

                return _cache;
            }
            catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException)
            {
                QuarantineCorruptedDatabase(ex);
                Initialize();
                return _cache!;
            }
        }
    }

    private static void QuarantineCorruptedDatabase(Exception ex)
    {
        try
        {
            var backup = Paths.DatabaseFile + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            File.Move(Paths.DatabaseFile, backup, overwrite: true);
            ConsoleService.Warning($"Base de donnees illisible ({ex.GetType().Name}). Sauvegarde : {backup}");
        }
        catch
        {
            ConsoleService.Warning("Base de donnees illisible. Reinitialisation...");
        }
    }

    public static void SaveDatabase(Dictionary<string, Dictionary<string, string>> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        lock (_lock)
        {
            _cache = data;
            var json = JsonSerializer.Serialize(data, JsonContext.Default.DictionaryStringDictionaryStringString);
            var encrypted = Encrypt(json);

            // Ecriture atomique : fichier temporaire puis renommage.
            var tempFile = Paths.DatabaseFile + ".tmp-" + Guid.NewGuid().ToString("N")[..8];
            try
            {
                File.WriteAllBytes(tempFile, encrypted);
                RestrictFilePermissions(tempFile);
                File.Move(tempFile, Paths.DatabaseFile, overwrite: true);
            }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { /* ignore */ }
            }
        }
    }

    private static void EnsureConfigDirectory()
    {
        Directory.CreateDirectory(Paths.ConfigDir);

        if (!OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(
                    Paths.ConfigDir,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            catch { /* systeme de fichiers sans permissions POSIX */ }
        }
    }

    private static void RestrictFilePermissions(string path)
    {
        if (OperatingSystem.IsWindows()) return;

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch { /* ignore */ }
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
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentNullException.ThrowIfNull(values);

        var db = LoadDatabase();
        db[section] = values;
        SaveDatabase(db);
    }

    public static void RemoveValue(string section, string key)
    {
        var db = LoadDatabase();
        if (db.TryGetValue(section, out var dict) && dict.Remove(key))
            SaveDatabase(db);
    }

    public static void RemoveSection(string section)
    {
        var db = LoadDatabase();
        if (db.Remove(section))
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

        var key = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var entry = new Dictionary<string, string>
        {
            ["name"] = projectName,
            ["template"] = template,
            ["dir"] = outputDir,
            ["date"] = DateTime.UtcNow.ToString("O")
        };

        history[key] = JsonSerializer.Serialize(entry, JsonContext.Default.DictionaryStringString);

        if (history.Count > 100)
        {
            var oldest = history.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Take(history.Count - 50)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var k in oldest) history.Remove(k);
        }

        SaveDatabase(db);
    }

    /// <summary>Retourne l'historique des generations, du plus recent au plus ancien.</summary>
    public static List<Dictionary<string, string>> GetHistory(int limit = 20)
    {
        var history = GetSection("history");
        var result = new List<Dictionary<string, string>>(Math.Min(limit, history.Count));

        foreach (var value in history.OrderByDescending(kv => kv.Key, StringComparer.Ordinal).Take(limit).Select(kv => kv.Value))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize(
                    value, JsonContext.Default.DictionaryStringString);
                if (parsed != null) result.Add(parsed);
            }
            catch (JsonException)
            {
                // Ancienne entree non JSON : on l'ignore sans faire echouer la lecture.
            }
        }

        return result;
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

    public static Dictionary<string, string> GetFavorites() => GetSection("favorites");

    public static void RemoveFavorite(string name) => RemoveValue("favorites", name);

    public static void AddPlugin(string name, string source) => SetValue("plugins", name, source);

    public static void RemovePlugin(string name) => RemoveValue("plugins", name);

    public static Dictionary<string, string> GetPlugins() => GetSection("plugins");

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
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            ConsoleService.Error($"Erreur d'import : {ex.Message}");
            return false;
        }
    }

    public static void Reset()
    {
        lock (_lock)
        {
            if (File.Exists(Paths.DatabaseFile))
                File.Delete(Paths.DatabaseFile);
            _cache = null;
        }

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

        var created = GetValue("settings", "db.created") ?? "N/A";
        var modified = info.LastWriteTimeUtc.ToString("o");

        return (size, created, modified);
    }

    private static byte[] Encrypt(string plainText)
    {
        var key = DeriveKey();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var cipher = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipher, tag, Magic);

        var result = new byte[Magic.Length + 1 + NonceSize + TagSize + cipher.Length];
        var offset = 0;
        Buffer.BlockCopy(Magic, 0, result, offset, Magic.Length); offset += Magic.Length;
        result[offset++] = FormatVersion;
        Buffer.BlockCopy(nonce, 0, result, offset, NonceSize); offset += NonceSize;
        Buffer.BlockCopy(tag, 0, result, offset, TagSize); offset += TagSize;
        Buffer.BlockCopy(cipher, 0, result, offset, cipher.Length);
        return result;
    }

    private static string Decrypt(byte[] payload)
    {
        if (payload.Length == 0)
            throw new CryptographicException("Base vide.");

        var isV2 = payload.Length >= Magic.Length + 1 + NonceSize + TagSize &&
                   payload.AsSpan(0, Magic.Length).SequenceEqual(Magic) &&
                   payload[Magic.Length] == FormatVersion;

        return isV2 ? DecryptV2(payload) : DecryptLegacyCbc(payload);
    }

    private static string DecryptV2(byte[] payload)
    {
        var key = DeriveKey();
        var offset = Magic.Length + 1;

        var nonce = payload.AsSpan(offset, NonceSize).ToArray(); offset += NonceSize;
        var tag = payload.AsSpan(offset, TagSize).ToArray(); offset += TagSize;
        var cipher = payload.AsSpan(offset).ToArray();

        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain, Magic);

        return Encoding.UTF8.GetString(plain);
    }

    // Ancien format (v1) : IV (16 octets) + AES-CBC/PKCS7. Conserve pour migrer sans perte.
    private static string DecryptLegacyCbc(byte[] payload)
    {
        const int legacyIvSize = 16;
        if (payload.Length <= legacyIvSize)
            throw new CryptographicException("Base trop courte.");

        var key = DeriveKey();
        var iv = payload.AsSpan(0, legacyIvSize).ToArray();
        var cipher = payload.AsSpan(legacyIvSize).ToArray();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
