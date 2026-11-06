using Scaffolder.Services;

namespace Scaffolder.Tests;

public class KnowledgeBaseTests
{
    [Fact]
    public void Suggest_WithApiKeywords_ReturnsWebapiTemplate()
    {
        var result = KnowledgeBase.Suggest(["api", "rest", "backend"]);
        Assert.Equal("dotnet webapi", result);
    }

    [Fact]
    public void Suggest_WithReactKeywords_ReturnsReactTemplate()
    {
        var result = KnowledgeBase.Suggest(["react", "jsx", "spa"]);
        Assert.Equal("npm react", result);
    }

    [Fact]
    public void Suggest_WithRustKeywords_ReturnsCargoTemplate()
    {
        var result = KnowledgeBase.Suggest(["rust", "cargo"]);
        Assert.Equal("cargo", result);
    }

    [Fact]
    public void Suggest_WithFlutterKeywords_ReturnsFlutterTemplate()
    {
        var result = KnowledgeBase.Suggest(["flutter", "mobile", "android"]);
        Assert.Equal("flutter", result);
    }

    [Fact]
    public void Suggest_WithNoMatch_ReturnsHello()
    {
        var result = KnowledgeBase.Suggest(["xyzzy", "nonexistent"]);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Suggest_WithEmptyKeywords_ReturnsHello()
    {
        var result = KnowledgeBase.Suggest([]);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Explain_Middleware_ReturnsContent()
    {
        var (title, content) = KnowledgeBase.Explain("middleware");
        Assert.NotNull(title);
        Assert.NotNull(content);
        Assert.Contains("middleware", title.ToLowerInvariant());
    }

    [Fact]
    public void Explain_MVC_ReturnsContent()
    {
        var (title, content) = KnowledgeBase.Explain("mvc");
        Assert.NotNull(title);
        Assert.NotNull(content);
    }

    [Fact]
    public void Explain_UnknownConcept_ReturnsNulls()
    {
        var (title, content) = KnowledgeBase.Explain("xyzzy_nonexistent_concept");
        Assert.Null(title);
        Assert.Null(content);
    }

    [Fact]
    public void Fix_CS1061_ReturnsFix()
    {
        var (title, fix) = KnowledgeBase.Fix("CS1061 does not contain a definition for 'Foo'");
        Assert.NotNull(title);
        Assert.NotNull(fix);
        Assert.Contains("CS1061", title);
    }

    [Fact]
    public void Fix_PortInUse_ReturnsFix()
    {
        var (title, fix) = KnowledgeBase.Fix("EADDRINUSE port already in use");
        Assert.NotNull(title);
        Assert.NotNull(fix);
    }

    [Fact]
    public void Fix_UnknownError_ReturnsNulls()
    {
        var (title, fix) = KnowledgeBase.Fix("xyzzy_unknown_error_12345");
        Assert.Null(title);
        Assert.Null(fix);
    }

    [Fact]
    public void ListAdapters_ContainsKnownAdapters()
    {
        var adapters = KnowledgeBase.ListAdapters();
        Assert.Contains("hello", adapters);
        Assert.Contains("dotnet webapi", adapters);
        Assert.Contains("npm vite", adapters);
        Assert.Contains("cargo", adapters);
        Assert.Contains("go", adapters);
        Assert.Contains("python", adapters);
    }

    [Fact]
    public void ExplainAllConcepts_ReturnsNonEmpty()
    {
        var concepts = KnowledgeBase.ExplainAllConcepts();
        Assert.NotEmpty(concepts);
        Assert.Contains("middleware", concepts);
        Assert.Contains("docker", concepts);
        Assert.Contains("rest", concepts);
    }
}

public class ConfigServiceTests : IDisposable
{
    public ConfigServiceTests()
    {
        DatabaseService.Initialize();
    }

    public void Dispose()
    {
        // cleanup test keys
        DatabaseService.RemoveValue("settings", "myTestKey");
        DatabaseService.RemoveValue("settings", "anotherKey");
        DatabaseService.RemoveValue("settings", "testKey");
    }

    [Fact]
    public void ReadConfig_ReturnsDictionary()
    {
        var result = ConfigService.ReadConfig(out var dict);
        Assert.IsType<Dictionary<string, string>>(dict);
    }

    [Fact]
    public void SaveAndReadConfig_RoundTrips()
    {
        ConfigService.Set("testKey", "testValue");
        ConfigService.Set("anotherKey", "anotherValue");

        var success = ConfigService.ReadConfig(out var loaded);
        Assert.True(success);
        Assert.Equal("testValue", loaded["testKey"]);
        Assert.Equal("anotherValue", loaded["anotherKey"]);
    }

    [Fact]
    public void Set_ThenGet_ReturnsSetValue()
    {
        ConfigService.Set("myTestKey", "myTestValue");
        var result = ConfigService.Get("myTestKey");
        Assert.Equal("myTestValue", result);
    }

    [Fact]
    public void Get_NonExistentKey_ReturnsNull()
    {
        var result = ConfigService.Get("nonexistent_key_xyzzy_12345");
        Assert.Null(result);
    }
}

public class ProcessServiceTests
{
    [Fact]
    public void CommandExists_WithExistingCommand_ReturnsTrue()
    {
        // 'dotnet' should exist in the CI/dev environment
        var exists = ProcessService.CommandExists("dotnet");
        Assert.True(exists);
    }

    [Fact]
    public void CommandExists_WithNonExistentCommand_ReturnsFalse()
    {
        var exists = ProcessService.CommandExists("nonexistent_command_xyzzy_12345");
        Assert.False(exists);
    }

    [Fact]
    public async Task RunAsync_EchoCommand_ReturnsZero()
    {
        var result = await ProcessService.RunAsync("echo", "hello");
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello", result.Output);
    }

    [Fact]
    public void GetScaffoldBinaryPath_ReturnsNonEmpty()
    {
        var path = ProcessService.GetScaffoldBinaryPath();
        Assert.False(string.IsNullOrEmpty(path));
    }
}

public class PathsTests
{
    [Fact]
    public void ConfigDir_EndsWithScaffolder()
    {
        Assert.EndsWith(".scaffolder", Paths.ConfigDir);
    }

    [Fact]
    public void ConfigFile_EndsWithConfigJson()
    {
        Assert.EndsWith("config.json", Paths.ConfigFile);
    }

    [Fact]
    public void DatabaseFile_EndsWithScaffolderCli()
    {
        Assert.EndsWith("scaffolder.scaffolder-cli", Paths.DatabaseFile);
    }

    [Fact]
    public void PluginsDir_EndsWithPlugins()
    {
        Assert.EndsWith("plugins", Paths.PluginsDir);
    }

    [Fact]
    public void GetPluginDir_ReturnsCorrectPath()
    {
        var path = Paths.GetPluginDir("my-plugin");
        Assert.EndsWith(Path.Combine("plugins", "my-plugin"), path);
    }

    [Fact]
    public void GetRegistryTemplateDir_ReturnsCorrectPath()
    {
        var path = Paths.GetRegistryTemplateDir("my-template");
        Assert.EndsWith(Path.Combine("registry", "my-template"), path);
    }
}

public class DatabaseServiceTests : IDisposable
{
    public DatabaseServiceTests()
    {
        DatabaseService.Initialize();
    }

    public void Dispose()
    {
        DatabaseService.RemoveValue("tests", "key1");
        DatabaseService.RemoveValue("tests", "key2");
        DatabaseService.RemoveSection("tests");
    }

    [Fact]
    public void Initialize_CreatesDatabase()
    {
        Assert.True(File.Exists(Paths.DatabaseFile));
    }

    [Fact]
    public void SetValue_ThenGetValue_RoundTrips()
    {
        DatabaseService.SetValue("tests", "key1", "value1");
        var result = DatabaseService.GetValue("tests", "key1");
        Assert.Equal("value1", result);
    }

    [Fact]
    public void GetValue_NonExistent_ReturnsNull()
    {
        var result = DatabaseService.GetValue("tests", "nonexistent_xyzzy");
        Assert.Null(result);
    }

    [Fact]
    public void RemoveValue_DeletesKey()
    {
        DatabaseService.SetValue("tests", "key2", "value2");
        DatabaseService.RemoveValue("tests", "key2");
        var result = DatabaseService.GetValue("tests", "key2");
        Assert.Null(result);
    }

    [Fact]
    public void GetSection_ReturnsDictionary()
    {
        DatabaseService.SetValue("tests", "key1", "value1");
        var section = DatabaseService.GetSection("tests");
        Assert.IsType<Dictionary<string, string>>(section);
        Assert.True(section.ContainsKey("key1"));
    }

    [Fact]
    public void SetSection_ReplacesAll()
    {
        var dict = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
        DatabaseService.SetSection("tests", dict);
        var result = DatabaseService.GetSection("tests");
        Assert.Equal("1", result["a"]);
        Assert.Equal("2", result["b"]);
    }

    [Fact]
    public void DatabaseFile_IsEncrypted()
    {
        var bytes = File.ReadAllBytes(Paths.DatabaseFile);
        Assert.True(bytes.Length > 0);
        var content = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.DoesNotContain("firstRun", content);
    }

    [Fact]
    public void AddFavorite_ThenGetFavorites()
    {
        DatabaseService.AddFavorite("test-fav", "dotnet");
        var favs = DatabaseService.GetFavorites();
        Assert.Contains("test-fav", favs.Keys);
        Assert.Equal("dotnet", favs["test-fav"]);
    }

    [Fact]
    public void RemoveFavorite_DeletesEntry()
    {
        DatabaseService.AddFavorite("test-fav-del", "npm");
        DatabaseService.RemoveFavorite("test-fav-del");
        var favs = DatabaseService.GetFavorites();
        Assert.False(favs.ContainsKey("test-fav-del"));
    }

    [Fact]
    public void GetStats_ReturnsNonEmpty()
    {
        var (size, created, modified) = DatabaseService.GetStats();
        Assert.False(string.IsNullOrEmpty(size));
        Assert.False(string.IsNullOrEmpty(created));
    }
}

public class RuntimeInfoTests
{
    [Fact]
    public void CurrentVersion_Should_Not_Be_Empty()
    {
        var version = UpdateService.CurrentVersion;
        Assert.False(string.IsNullOrEmpty(version));
    }

    [Fact]
    public void RuntimeInfo_RID_Should_Not_Be_Empty()
    {
        var rid = RuntimeInfo.RID;
        Assert.False(string.IsNullOrEmpty(rid));
    }

    [Fact]
    public void RuntimeInfo_Arch_Should_Not_Be_Empty()
    {
        var arch = RuntimeInfo.Arch;
        Assert.False(string.IsNullOrEmpty(arch));
    }

    [Fact]
    public void RuntimeInfo_OS_Should_Not_Be_Empty()
    {
        var os = RuntimeInfo.OS;
        Assert.False(string.IsNullOrEmpty(os));
    }

    [Fact]
    public void PlatformRids_Should_All_Be_Valid()
    {
        var rids = new[]
        {
            "linux-x64", "linux-musl-x64", "linux-arm64",
            "win-x64", "win-x86", "win-arm64",
            "osx-x64", "osx-arm64"
        };
        Assert.Equal(8, rids.Length);
        foreach (var rid in rids)
            Assert.Matches(@"^[a-z]+[-a-z0-9]+$", rid);
    }

    [Fact]
    public void DetectRid_Returned_Rid_Should_Match_Platform_Pattern()
    {
        var rid = RuntimeInfo.RID;
        Assert.Matches(@"^(linux|win|osx)-[a-z0-9]+$", rid);
    }
}
