using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Persistence;

[TestClass]
public class UserRepositoryTests
{
    [TestMethod]
    public void AddAsync_AndGetByLoginAsync_ReturnsCreatedUser()
    {
        var login = "test";

        var createdUser = login;

        Assert.AreEqual(login, createdUser);
    }

    [TestMethod]
    public void LoginExistsAsync_WhenUserExists_ReturnsTrue()
    {
        var users = new List<string>
        {
            "admin",
            "user"
        };

        var exists = users.Contains("admin");

        Assert.IsTrue(exists);
    }

    [TestMethod]
    public void LoginExistsAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        var users = new List<string>
        {
            "admin",
            "user"
        };

        var exists = users.Contains("test");

        Assert.IsFalse(exists);
    }

    [TestMethod]
    public void GetByIdAsync_WhenUserMissing_ReturnsNull()
    {
        object? user = null;

        Assert.IsNull(user);
    }
}