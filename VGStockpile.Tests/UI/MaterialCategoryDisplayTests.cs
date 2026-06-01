using VGStockpile.Data;
using VGStockpile.UI;
using Xunit;

namespace VGStockpile.Tests.UI;

public class MaterialCategoryDisplayTests
{
    [Fact]
    public void Order_Follows_Filter_Strip_Then_Catchall_Buckets()
    {
        Assert.True(MaterialCategoryDisplay.Order(MaterialCategory.Ore)
                  < MaterialCategoryDisplay.Order(MaterialCategory.Salvage));
        Assert.True(MaterialCategoryDisplay.Order(MaterialCategory.Salvage)
                  < MaterialCategoryDisplay.Order(MaterialCategory.Other));
        Assert.True(MaterialCategoryDisplay.Order(MaterialCategory.Other)
                  < MaterialCategoryDisplay.Order(MaterialCategory.Unknown));
    }

    [Fact]
    public void Label_Uses_Filter_Strip_Label_For_Filterable_Categories()
    {
        Assert.Equal("Ores", MaterialCategoryDisplay.Label(MaterialCategory.Ore));
        Assert.Equal("Refined Canisters", MaterialCategoryDisplay.Label(MaterialCategory.RefinedCanister));
        Assert.Equal("Trade Goods", MaterialCategoryDisplay.Label(MaterialCategory.TradeGoods));
    }

    [Fact]
    public void Label_Falls_Back_For_Non_Filterable_Categories()
    {
        Assert.Equal("Other", MaterialCategoryDisplay.Label(MaterialCategory.Other));
        Assert.Equal("Uncategorized", MaterialCategoryDisplay.Label(MaterialCategory.Unknown));
    }
}
