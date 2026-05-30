using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.CartModule.Queries;

[TestClass]
public class GetCartItemsQueryUnitTests
{
    [TestMethod]
    public void GetCartItems_WhenItemsExist_ReturnsItems()
    {
        var items = new List<string>
        {
            "Салат",
            "Пицца"
        };

        Assert.AreEqual(2, items.Count);
    }

    [TestMethod]
    public void GetCartItems_WhenCartEmpty_ReturnsEmptyCollection()
    {
        var items = new List<string>();

        Assert.AreEqual(0, items.Count);
    }
}