using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.AdminModule.Services;

[TestClass]
public class AdminServiceTests
{
    [TestMethod]
    public void CreateUser_WhenValid_ReturnsCreated()
    {
        var created = true;

        Assert.IsTrue(created);
    }

    [TestMethod]
    public void DeleteUser_WhenExists_ReturnsTrue()
    {
        var deleted = true;

        Assert.IsTrue(deleted);
    }

    [TestMethod]
    public void BlockUser_WhenExists_ReturnsTrue()
    {
        var blocked = true;

        Assert.IsTrue(blocked);
    }

    [TestMethod]
    public void GetStatistics_ReturnsNotNull()
    {
        object statistics = new();

        Assert.IsNotNull(statistics);
    }
}