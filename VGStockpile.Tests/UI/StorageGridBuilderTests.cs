using System.Collections.Generic;
using System.Linq;
using VGStockpile.Data;
using VGStockpile.Tests.Data;
using VGStockpile.UI;
using Xunit;

namespace VGStockpile.Tests.UI;

public class StorageGridBuilderTests
{
    private static StationStorageSnapshot Snap(
        string id, string name, string system, string faction,
        params (string mat, int qty)[] items)
        => new(
            id, name, system, faction,
            items.ToDictionary(i => i.mat, i => i.qty));

    private static FakeMaterialCatalog DefaultCatalog() =>
        new FakeMaterialCatalog()
            .Add("ti", "Titanium", MaterialCategory.RefinedGoods)
            .Add("ox", "Oxide",    MaterialCategory.RefinedGoods)
            .Add("si", "Silicon",  MaterialCategory.RefinedGoods)
            .Add("canister-of-oxide", "Canister of Refined Oxide", MaterialCategory.RefinedCanister)
            .Add("iron-ore", "Iron Ore", MaterialCategory.Ore)
            .Add("ruby",     "Ruby",     MaterialCategory.Crystal)
            .Add("crate",    "Crate",    MaterialCategory.TradeGoods);

    private static ISet<MaterialCategory> All() =>
        new HashSet<MaterialCategory>
        {
            MaterialCategory.Ore,
            MaterialCategory.RefinedCanister,
            MaterialCategory.RefinedGoods,
            MaterialCategory.Crystal,
            MaterialCategory.TradeGoods,
            MaterialCategory.Salvage,
            MaterialCategory.Other,
        };

    private static ISet<MaterialCategory> NoOres() =>
        new HashSet<MaterialCategory>
        {
            MaterialCategory.RefinedCanister,
            MaterialCategory.RefinedGoods,
            MaterialCategory.Crystal,
            MaterialCategory.TradeGoods,
            MaterialCategory.Salvage,
            MaterialCategory.Other,
        };

    [Fact]
    public void Empty_Snapshot_List_Yields_Empty_Grid()
    {
        var r = new StorageGridBuilder(DefaultCatalog())
            .Build(new List<StationStorageSnapshot>(), All());

        Assert.Empty(r.ColumnMaterialIds);
        Assert.Empty(r.ColumnDisplayNames);
        Assert.Empty(r.Rows);
    }

    [Fact]
    public void Single_Station_Single_Material()
    {
        var snaps = new[] { Snap("s1", "Helios", "Sol", "fac.coalition", ("ti", 100)) };
        var r = new StorageGridBuilder(DefaultCatalog()).Build(snaps, All());

        Assert.Equal(new[] { "ti" }, r.ColumnMaterialIds);
        Assert.Equal(new[] { "Titanium" }, r.ColumnDisplayNames);
        Assert.Single(r.Rows);
        Assert.Equal("100", r.Rows[0].Cells[0]);
    }

    [Fact]
    public void Multiple_Stations_With_Disjoint_Materials_Union_Columns()
    {
        var snaps = new[]
        {
            Snap("s1", "Alpha", "Sol",   "fac.a", ("ti", 50)),
            Snap("s2", "Beta",  "Vega",  "fac.b", ("ox", 80)),
        };
        var r = new StorageGridBuilder(DefaultCatalog()).Build(snaps, All());

        Assert.Equal(new[] { "ox", "ti" }, r.ColumnMaterialIds);
        Assert.Equal(new[] { "Oxide", "Titanium" }, r.ColumnDisplayNames);

        Assert.Equal(2, r.Rows.Count);
        Assert.Equal("Beta", r.Rows[0].Snapshot.StationName);
        Assert.Equal("Alpha", r.Rows[1].Snapshot.StationName);

        Assert.Equal(new[] { "80", "" }, r.Rows[0].Cells);
        Assert.Equal(new[] { "", "50" }, r.Rows[1].Cells);
    }

    [Fact]
    public void Hidden_Category_Removes_Its_Columns()
    {
        var snaps = new[]
        {
            Snap("s1", "OreCity", "Vega", "fac.a", ("ti", 10), ("iron-ore", 9_000)),
        };

        var noFilter = new StorageGridBuilder(DefaultCatalog()).Build(snaps, All());
        Assert.Contains("iron-ore", noFilter.ColumnMaterialIds);

        var filtered = new StorageGridBuilder(DefaultCatalog()).Build(snaps, NoOres());
        Assert.DoesNotContain("iron-ore", filtered.ColumnMaterialIds);
        Assert.Contains("ti", filtered.ColumnMaterialIds);
    }

    [Fact]
    public void Hiding_Category_Recomputes_Row_Order_From_Visible_Totals()
    {
        var snaps = new[]
        {
            Snap("s1", "RichIron", "Sol", "fac.a", ("ti", 5),  ("iron-ore", 9_000)),
            Snap("s2", "PoorIron", "Sol", "fac.a", ("ti", 50)),
        };

        var unhidden = new StorageGridBuilder(DefaultCatalog()).Build(snaps, All());
        Assert.Equal("RichIron", unhidden.Rows[0].Snapshot.StationName);

        var hidden = new StorageGridBuilder(DefaultCatalog()).Build(snaps, NoOres());
        Assert.Equal("PoorIron", hidden.Rows[0].Snapshot.StationName);
    }

    [Fact]
    public void Row_Tie_Broken_By_Station_Name_Ascending()
    {
        var snaps = new[]
        {
            Snap("s2", "Bravo", "Sol", "fac.a", ("ti", 100)),
            Snap("s1", "Alpha", "Sol", "fac.a", ("ti", 100)),
        };
        var r = new StorageGridBuilder(DefaultCatalog()).Build(snaps, All());

        Assert.Equal("Alpha", r.Rows[0].Snapshot.StationName);
        Assert.Equal("Bravo", r.Rows[1].Snapshot.StationName);
    }

    [Fact]
    public void Unknown_Material_Is_Not_Hidden_By_Filters()
    {
        var snaps = new[]
        {
            Snap("s1", "Mystery", "Sol", "fac.a", ("xyz-unknown", 42)),
        };
        // Empty visible set: still surfaces unknown material columns.
        var r = new StorageGridBuilder(DefaultCatalog())
            .Build(snaps, new HashSet<MaterialCategory>());

        Assert.Contains("xyz-unknown", r.ColumnMaterialIds);
        Assert.Equal("xyz-unknown", r.ColumnDisplayNames[0]);
    }

    [Fact]
    public void Cells_Use_Compact_Formatting()
    {
        var snaps = new[] { Snap("s1", "BigStash", "Sol", "fac.a", ("ti", 12_345)) };
        var r = new StorageGridBuilder(DefaultCatalog()).Build(snaps, All());

        Assert.Equal("12.3k", r.Rows[0].Cells[0]);
    }

    [Fact]
    public void Zero_Quantity_Drops_Row()
    {
        var snaps = new[] {
            Snap("s1", "ZeroStash", "Sol", "fac.a", ("ti", 0)),
            Snap("s2", "RealStash", "Sol", "fac.a", ("ti", 5)),
        };
        var r = new StorageGridBuilder(DefaultCatalog()).Build(snaps, All());

        Assert.Single(r.Rows);
        Assert.Equal("RealStash", r.Rows[0].Snapshot.StationName);
        Assert.Equal("5", r.Rows[0].Cells[0]);
    }

    [Fact]
    public void Ore_Only_Station_Is_Hidden_When_Ore_Category_Filtered_Out()
    {
        var snaps = new[]
        {
            Snap("s1", "OreOnly",  "Sol", "fac.a", ("iron-ore", 9_000)),
            Snap("s2", "Refining", "Sol", "fac.a", ("ti", 100)),
        };

        var hidden = new StorageGridBuilder(DefaultCatalog()).Build(snaps, NoOres());
        Assert.Single(hidden.Rows);
        Assert.Equal("Refining", hidden.Rows[0].Snapshot.StationName);
    }

    [Fact]
    public void Crystal_And_TradeGoods_Are_Distinct_Filters()
    {
        var snaps = new[]
        {
            Snap("s1", "Gemstore", "Sol", "fac.a", ("ruby", 5)),
            Snap("s2", "Cratery",  "Sol", "fac.a", ("crate", 10)),
        };

        // Only Crystal visible: gemstore survives, cratery dropped.
        var crystalsOnly = new StorageGridBuilder(DefaultCatalog())
            .Build(snaps, new HashSet<MaterialCategory> { MaterialCategory.Crystal });
        Assert.Single(crystalsOnly.Rows);
        Assert.Equal("Gemstore", crystalsOnly.Rows[0].Snapshot.StationName);

        // Only TradeGoods visible: cratery survives, gemstore dropped.
        var tradeOnly = new StorageGridBuilder(DefaultCatalog())
            .Build(snaps, new HashSet<MaterialCategory> { MaterialCategory.TradeGoods });
        Assert.Single(tradeOnly.Rows);
        Assert.Equal("Cratery", tradeOnly.Rows[0].Snapshot.StationName);
    }
}
