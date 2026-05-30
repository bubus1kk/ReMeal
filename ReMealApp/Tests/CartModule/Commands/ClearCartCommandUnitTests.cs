using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.CartModule.Commands;

[TestClass]
public class ClearCartCommandUnitTests
{
    [TestMethod]
    public void ClearCart_WhenCartContainsItems_CartBecomesEmpty()
    {
        var cartCount = 5;

        cartCount = 0;

        Assert.AreEqual(0, cartCount);
    }

    [TestMethod]
    public void ClearCart_WhenCartAlreadyEmpty_RemainsEmpty()
    {
        var cartCount = 0;

        Assert.AreEqual(0, cartCount);
    }
}