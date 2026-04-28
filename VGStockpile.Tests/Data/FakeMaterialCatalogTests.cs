using VGStockpile.Data;
using Xunit;

namespace VGStockpile.Tests.Data;

public class FakeMaterialCatalogTests
{
    [Fact]
    public void Unknown_Id_Returns_Id_As_Name_And_Unknown_Category()
    {
        var c = new FakeMaterialCatalog();
        Assert.Equal("mystery", c.DisplayName("mystery"));
        Assert.Equal(MaterialCategory.Unknown, c.Category("mystery"));
    }

    [Fact]
    public void Known_Id_Returns_Configured_Name_And_Category()
    {
        var c = new FakeMaterialCatalog().Add("ti", "Titanium", MaterialCategory.Refined);
        Assert.Equal("Titanium", c.DisplayName("ti"));
        Assert.Equal(MaterialCategory.Refined, c.Category("ti"));
    }
}
