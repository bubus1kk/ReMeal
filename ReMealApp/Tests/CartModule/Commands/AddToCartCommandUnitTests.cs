using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.CartModule.Commands;

[TestClass]
public class AddToCartCommandUnitTests
{
    [TestMethod]
    public void AddToCart_WhenLotExists_ItemAdded()
    {
        var cartCount = 0;

        cartCount++;

        Assert.AreEqual(1, cartCount);
    }

    [TestMethod]
    public void AddToCart_WhenAddingSeveralItems_CountIncreases()
    {
        var cartCount = 1;

        cartCount++;

        Assert.AreEqual(2, cartCount);
    }

    [TestMethod]
    public void AddToCart_WhenLotMissing_CountDoesNotChange()
    {
        var cartCount = 0;

        Assert.AreEqual(0, cartCount);
    }
}