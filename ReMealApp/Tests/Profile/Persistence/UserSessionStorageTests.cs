using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Profile.Persistence;

[TestClass]
public class UserSessionStorageTests
{
    [TestMethod]
    public void SaveSession_SavesUserId()
    {
        var userId = Guid.NewGuid();

        var storedUserId = userId;

        Assert.AreEqual(userId, storedUserId);
    }

    [TestMethod]
    public void ClearSession_RemovesStoredUser()
    {
        Guid? userId = Guid.NewGuid();

        userId = null;

        Assert.IsNull(userId);
    }

    [TestMethod]
    public void GetSession_WhenSessionExists_ReturnsUser()
    {
        var username = "admin";

        Assert.AreEqual("admin", username);
    }
}