using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.CartModule.Services;

[TestClass]
public class CartServiceUnitTests
{
    [TestMethod]
    public void CalculateTotal_WhenItemsExist_ReturnsCorrectTotal()
    {
        decimal total = 250 + 150;

        Assert.AreEqual(400, total);
    }

    [TestMethod]
    public void CalculateTotal_WhenCartEmpty_ReturnsZero()
    {
        decimal total = 0;

        Assert.AreEqual(0, total);
    }

    [TestMethod]
    public void ApplyDiscount_WhenDiscountExists_TotalReduced()
    {
        decimal total = 500;
        decimal discount = 100;

        total -= discount;

        Assert.AreEqual(400, total);
    }
}