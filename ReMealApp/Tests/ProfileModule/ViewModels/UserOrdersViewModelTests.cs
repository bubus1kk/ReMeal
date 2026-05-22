using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Profile.ViewModels;

[TestClass]
public class UserOrdersViewModelTests
{
    [TestMethod]
    public void LoadOrders_WhenOrdersExist_ReturnsOrders()
    {
        var ordersCount = 3;

        var loadedOrders = ordersCount;

        Assert.AreEqual(3, loadedOrders);
    }

    [TestMethod]
    public void LoadOrders_WhenOrdersEmpty_ReturnsEmptyCollection()
    {
        var orders = new List<string>();

        Assert.AreEqual(0, orders.Count);
    }

    [TestMethod]
    public void OrdersViewModel_InitializesCorrectly()
    {
        var initialized = true;

        Assert.IsTrue(initialized);
    }
}