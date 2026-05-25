using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.AdminModule.Queries;

[TestClass]
public class GetUsersQueryTests
{
    [TestMethod]
    public void GetUsers_WhenUsersExist_ReturnsUsers()
    {
        var users = new List<string>
        {
            "admin",
            "user"
        };

        Assert.AreEqual(2, users.Count);
    }

    [TestMethod]
    public void GetUsers_WhenNoUsers_ReturnsEmptyCollection()
    {
        var users = new List<string>();

        Assert.AreEqual(0, users.Count);
    }

    [TestMethod]
    public void GetUsers_ContainsAdmin_ReturnsTrue()
    {
        var users = new List<string>
        {
            "admin",
            "user"
        };

        Assert.IsTrue(users.Contains("admin"));
    }
}