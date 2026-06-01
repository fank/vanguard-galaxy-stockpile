using System.Linq;
using VGStockpile.Data;
using VGStockpile.Tests.Data;
using VGStockpile.UI.Refinery;
using Xunit;

namespace VGStockpile.Tests.UI;

public class RefineryJobsBuilderTests
{
    private static RefineryJobSnapshot Job(
        string station, string matId, float eta, float progress = 0f, int maxJobs = 1)
        => new(
            StationId:       $"st-{station}",
            StationName:     station,
            SystemGuid:      "sys",
            SystemName:      "System",
            FactionId:       "fac",
            MaterialId:      matId,
            ProgressFraction: progress,
            RemainingAmount: 1,
            InitialAmount:   1,
            EtaSeconds:      eta,
            MaxJobs:         maxJobs);

    private static FakeMaterialCatalog Catalog() =>
        new FakeMaterialCatalog()
            .Add("iron-ore", "Iron Ore", MaterialCategory.Ore)
            .Add("scrap",    "Scrap",    MaterialCategory.Salvage);

    [Fact]
    public void Groups_Jobs_By_Station()
    {
        var groups = new RefineryJobsBuilder(Catalog()).Build(new[]
        {
            Job("Helios", "iron-ore", eta: 10f),
            Job("Helios", "scrap",    eta: 90f),
            Job("Aurora", "scrap",    eta: 30f),
        });

        Assert.Equal(2, groups.Count);
        Assert.Equal(new[] { "Aurora", "Helios" }, groups.Select(g => g.StationName).ToArray());
        Assert.Equal(2, groups.Single(g => g.StationName == "Helios").Jobs.Count);
    }

    [Fact]
    public void Stations_Ordered_By_Name_Not_By_Eta()
    {
        // ETA must NOT drive order (it ticks down and would reshuffle).
        var groups = new RefineryJobsBuilder(Catalog()).Build(new[]
        {
            Job("Zeta",  "iron-ore", eta: 10f),
            Job("Alpha", "scrap",    eta: 90f),
        });

        Assert.Equal(new[] { "Alpha", "Zeta" }, groups.Select(g => g.StationName).ToArray());
    }

    [Fact]
    public void Jobs_Within_Station_Ordered_By_Material_Name()
    {
        var groups = new RefineryJobsBuilder(Catalog()).Build(new[]
        {
            Job("Helios", "scrap",    eta: 10f),   // "Scrap"
            Job("Helios", "iron-ore", eta: 90f),   // "Iron Ore"
        });

        Assert.Equal(new[] { "Iron Ore", "Scrap" },
            groups.Single().Jobs.Select(j => j.MaterialName).ToArray());
    }

    [Fact]
    public void Resolves_Localized_Material_Name()
    {
        var groups = new RefineryJobsBuilder(Catalog()).Build(new[] { Job("Helios", "scrap", 10f) });
        Assert.Equal("Scrap", groups.Single().Jobs.Single().MaterialName);
    }

    [Fact]
    public void Unknown_Material_Falls_Back_To_Id()
    {
        var groups = new RefineryJobsBuilder(Catalog()).Build(new[] { Job("Helios", "mystery", 10f) });
        Assert.Equal("mystery", groups.Single().Jobs.Single().MaterialName);
    }

    [Fact]
    public void Group_Carries_Station_MaxJobs()
    {
        var groups = new RefineryJobsBuilder(Catalog())
            .Build(new[] { Job("Helios", "scrap", 10f, maxJobs: 5) });
        Assert.Equal(5, groups.Single().MaxJobs);
    }

    [Fact]
    public void Empty_Input_Yields_No_Groups()
    {
        Assert.Empty(new RefineryJobsBuilder(Catalog()).Build(System.Array.Empty<RefineryJobSnapshot>()));
    }
}
