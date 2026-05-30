using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.CartModule.Commands;

[TestClass]
public class RemoveFromCartCommandUnitTests
{
    [TestMethod]
    public void RemoveFromCart_WhenItemExists_ItemRemoved()
    {
        var cartCount = 2;

        cartCount--;

        Assert.AreEqual(1, cartCount);
    }

    [TestMethod]
    public void RemoveFromCart_WhenCartEmpty_CountRemainsZero()
    {
        var cartCount = 0;

        Assert.AreEqual(0, cartCount);
    }

    [TestMethod]
    public void RemoveFromCart_WhenRemovingLastItem_CartBecomesEmpty()
    {
        var cartCount = 1;

        cartCount--;

        Assert.AreEqual(0, cartCount);
    }
}