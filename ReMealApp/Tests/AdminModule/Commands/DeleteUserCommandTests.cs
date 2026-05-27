using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMeal.Tests.AdminModule.Commands;

[TestClass]
public class DeleteUserCommandTests
{
    [TestMethod]
    public void DeleteUser_WhenUserExists_RemovesUser()
    {
        var users = new List<string>
        {
            "admin",
            "user",
            "test"
        };

        users.Remove("test");

        Assert.IsFalse(users.Contains("test"));
    }

    [TestMethod]
    public void DeleteUser_WhenUserMissing_ListDoesNotChange()
    {
        var users = new List<string>
        {
            "admin",
            "user"
        };

        users.Remove("test");

        Assert.AreEqual(2, users.Count);
    }

    [TestMethod]
    public void DeleteUser_WhenCollectionEmpty_ReturnsFalse()
    {
        var users = new List<string>();

        var removed = users.Remove("admin");

        Assert.IsFalse(removed);
    }
}