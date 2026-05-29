using System.IO;
using VGStockpile.Transfers.Persistence;
using Xunit;

namespace VGStockpile.Tests.Transfers.Persistence;

public class SavePathResolverTests
{
    [Fact]
    public void Sidecar_ReplacesSaveExtension()
    {
        var dir = Path.Combine(Path.GetTempPath(), "vgsp");
        var save = Path.Combine(dir, "myslot.save");
        var sidecar = SavePathResolver.Sidecar(save);
        Assert.Equal(Path.Combine(dir, "myslot" + SavePathResolver.SidecarSuffix), sidecar);
    }

    [Fact]
    public void Sidecar_WithNoExtension_AppendsSuffix()
    {
        var dir = Path.Combine(Path.GetTempPath(), "vgsp");
        var save = Path.Combine(dir, "myslot");
        var sidecar = SavePathResolver.Sidecar(save);
        Assert.Equal(Path.Combine(dir, "myslot" + SavePathResolver.SidecarSuffix), sidecar);
    }

    [Fact]
    public void Sidecar_NeverReturnsTheSavePath()
    {
        var save = Path.Combine(Path.GetTempPath(), "myslot.save");
        Assert.NotEqual(save, SavePathResolver.Sidecar(save));
    }
}
