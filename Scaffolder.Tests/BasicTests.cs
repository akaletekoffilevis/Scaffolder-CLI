using Scaffolder.Services;

namespace Scaffolder.Tests;

public class BasicTests
{
    [Fact]
    public void CurrentVersion_Should_Not_Be_Empty()
    {
        var version = UpdateService.CurrentVersion;
        Assert.False(string.IsNullOrEmpty(version));
    }

    [Fact]
    public void KnowledgeBaseAdapterList_Should_Contain_Known()
    {
        var adapters = KnowledgeBase.ListAdapters();
        Assert.Contains("hello", adapters);
        Assert.Contains("dotnet webapi", adapters);
        Assert.Contains("npm vite", adapters);
        Assert.Contains("cargo", adapters);
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
