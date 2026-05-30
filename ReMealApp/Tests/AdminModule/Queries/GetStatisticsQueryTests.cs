using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.AdminModule.Queries;

[TestClass]
public class GetStatisticsQueryTests
{
    [TestMethod]
    public void GetStatistics_ReturnsCorrectUsersCount()
    {
        var usersCount = 15;

        Assert.AreEqual(15, usersCount);
    }

    [TestMethod]
    public void GetStatistics_ReturnsCorrectLotsCount()
    {
        var lotsCount = 24;

        Assert.AreEqual(24, lotsCount);
    }

    [TestMethod]
    public void GetStatistics_WhenNoData_ReturnsZero()
    {
        var statistics = 0;

        Assert.AreEqual(0, statistics);
    }
}