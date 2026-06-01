using System.Linq;
using VGStockpile.Data;
using VGStockpile.Tests.Data;
using VGStockpile.UI;
using VGStockpile.UI.Transfers;
using Xunit;

namespace VGStockpile.Tests.UI;

public class TransferRowGroupingTests
{
    private static FakeMaterialCatalog Catalog() =>
        new FakeMaterialCatalog()
            .Add("iron-ore",   "Iron Ore",   MaterialCategory.Ore)
            .Add("copper-ore", "Copper Ore", MaterialCategory.Ore)
            .Add("ruby",       "Ruby",       MaterialCategory.Crystal)
            .Add("ti",         "Titanium",   MaterialCategory.RefinedGoods);

    [Fact]
    public void Groups_By_Category_In_Filter_Strip_Order()
    {
        var groups = TransferRowGrouping.Build(
            new[] { "ruby", "ti", "iron-ore" }, Catalog());

        Assert.Equal(
            new[]
            {
                MaterialCategory.Ore,
                MaterialCategory.RefinedGoods,
                MaterialCategory.Crystal,
            },
            groups.Select(g => g.Category).ToArray());
    }

    [Fact]
    public void Within_Group_Sorted_By_GameplayType_Then_Name()
    {
        var groups = TransferRowGrouping.Build(
            new[] { "iron-ore", "copper-ore" }, Catalog());

        var ore = groups.Single(g => g.Category == MaterialCategory.Ore);
        // Same gameplay order (0) -> tie broken by name: Copper before Iron.
        Assert.Equal(new[] { "copper-ore", "iron-ore" }, ore.MaterialIds.ToArray());
    }

    [Fact]
    public void GameplayType_Order_Takes_Precedence_Over_Name()
    {
        var catalog = new FakeMaterialCatalog()
            .Add("aaa", "Aaa", MaterialCategory.RefinedGoods, gameplayOrder: 5)
            .Add("zzz", "Zzz", MaterialCategory.RefinedGoods, gameplayOrder: 1);

        var groups = TransferRowGrouping.Build(new[] { "aaa", "zzz" }, catalog);

        Assert.Equal(new[] { "zzz", "aaa" }, groups.Single().MaterialIds.ToArray());
    }

    [Fact]
    public void Unknown_Category_Sorts_Last()
    {
        var catalog = new FakeMaterialCatalog()
            .Add("iron-ore", "Iron Ore", MaterialCategory.Ore);
        // "mystery" is not registered -> Unknown.
        var groups = TransferRowGrouping.Build(new[] { "mystery", "iron-ore" }, catalog);

        Assert.Equal(MaterialCategory.Ore, groups[0].Category);
        Assert.Equal(MaterialCategory.Unknown, groups[^1].Category);
    }

    [Fact]
    public void Empty_Input_Yields_No_Groups()
    {
        Assert.Empty(TransferRowGrouping.Build(System.Array.Empty<string>(), Catalog()));
    }
}
