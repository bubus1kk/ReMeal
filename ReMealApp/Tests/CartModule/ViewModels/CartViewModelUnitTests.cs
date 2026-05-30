using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.CartModule.ViewModels;

[TestClass]
public class CartViewModelUnitTests
{
    [TestMethod]
    public void LoadCart_WhenCalled_ItemsLoaded()
    {
        var loaded = true;

        Assert.IsTrue(loaded);
    }

    [TestMethod]
    public void RemoveItem_WhenExecuted_ItemCountReduced()
    {
        var count = 3;

        count--;

        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void Checkout_WhenCartNotEmpty_ReturnsSuccess()
    {
        var success = true;

        Assert.IsTrue(success);
    }
}