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
            .Add("ti", "Titanium", MaterialCategory.Refined)
            .Add("ox", "Oxide",    MaterialCategory.Refined)
            .Add("si", "Silicon",  MaterialCategory.Refined)
            .Add("iron-ore", "Iron Ore", MaterialCategory.Ore);

    [Fact]
    public void Empty_Snapshot_List_Yields_Empty_Grid()
    {
        var r = new StorageGridBuilder(DefaultCatalog())
            .Build(new List<StationStorageSnapshot>(), hideOres: false);

        Assert.Empty(r.ColumnMaterialIds);
        Assert.Empty(r.ColumnDisplayNames);
        Assert.Empty(r.Rows);
    }

    [Fact]
    public void Single_Station_Single_Material()
    {
        var snaps = new[] { Snap("s1", "Helios", "Sol", "fac.coalition", ("ti", 100)) };
        var r = new StorageGridBuilder(DefaultCatalog()).Build(snaps, hideOres: false);

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
        var r = new StorageGridBuilder(DefaultCatalog()).Build(snaps, hideOres: false);

        Assert.Equal(new[] { "ox", "ti" }, r.ColumnMaterialIds);
        Assert.Equal(new[] { "Oxide", "Titanium" }, r.ColumnDisplayNames);

        // Beta:80 > Alpha:50, so Beta first.
        Assert.Equal(2, r.Rows.Count);
        Assert.Equal("Beta", r.Rows[0].Snapshot.StationName);
        Assert.Equal("Alpha", r.Rows[1].Snapshot.StationName);

        Assert.Equal(new[] { "80", "" }, r.Rows[0].Cells);
        Assert.Equal(new[] { "", "50" }, r.Rows[1].Cells);
    }

    [Fact]
    public void Hide_Ores_Removes_Ore_Columns()
    {
        var snaps = new[]
        {
            Snap("s1", "OreCity", "Vega", "fac.a", ("ti", 10), ("iron-ore", 9_000)),
        };

        var noFilter = new StorageGridBuilder(DefaultCatalog()).Build(snaps, hideOres: false);
        Assert.Contains("iron-ore", noFilter.ColumnMaterialIds);

        var filtered = new StorageGridBuilder(DefaultCatalog()).Build(snaps, hideOres: true);
        Assert.DoesNotContain("iron-ore", filtered.ColumnMaterialIds);
        Assert.Contains("ti", filtered.ColumnMaterialIds);
    }

    [Fact]
    public void Hide_Ores_Recomputes_Row_Order_From_Visible_Totals()
    {
        var snaps = new[]
        {
            Snap("s1", "RichIron", "Sol", "fac.a", ("ti", 5),  ("iron-ore", 9_000)),
            Snap("s2", "PoorIron", "Sol", "fac.a", ("ti", 50)),
        };

        var unhidden = new StorageGridBuilder(DefaultCatalog()).Build(snaps, hideOres: false);
        Assert.Equal("RichIron", unhidden.Rows[0].Snapshot.StationName);

        var hidden = new StorageGridBuilder(DefaultCatalog()).Build(snaps, hideOres: true);
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
        var r = new StorageGridBuilder(DefaultCatalog()).Build(snaps, hideOres: false);

        Assert.Equal("Alpha", r.Rows[0].Snapshot.StationName);
        Assert.Equal("Bravo", r.Rows[1].Snapshot.StationName);
    }

    [Fact]
    public void Unknown_Material_Is_Not_Hidden_By_Ore_Filter()
    {
        var snaps = new[]
        {
            Snap("s1", "Mystery", "Sol", "fac.a", ("xyz-unknown", 42)),
        };
        var r = new StorageGridBuilder(DefaultCatalog()).Build(snaps, hideOres: true);

        Assert.Contains("xyz-unknown", r.ColumnMaterialIds);
        Assert.Equal("xyz-unknown", r.ColumnDisplayNames[0]);
    }

    [Fact]
    public void Cells_Use_Compact_Formatting()
    {
        var snaps = new[] { Snap("s1", "BigStash", "Sol", "fac.a", ("ti", 12_345)) };
        var r = new StorageGridBuilder(DefaultCatalog()).Build(snaps, hideOres: false);

        Assert.Equal("12.3k", r.Rows[0].Cells[0]);
    }

    [Fact]
    public void Zero_Quantity_Is_Treated_As_Missing()
    {
        var snaps = new[] { Snap("s1", "Stash", "Sol", "fac.a", ("ti", 0)) };
        var r = new StorageGridBuilder(DefaultCatalog()).Build(snaps, hideOres: false);

        Assert.Equal("", r.Rows[0].Cells[0]);
    }
}
