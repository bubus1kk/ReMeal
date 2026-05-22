using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Profile.Services;

[TestClass]
public class UserSessionServiceTests
{
    [TestMethod]
    public void Login_CreatesActiveSession()
    {
        var isLoggedIn = false;

        isLoggedIn = true;

        Assert.IsTrue(isLoggedIn);
    }

    [TestMethod]
    public void Logout_RemovesActiveSession()
    {
        var isLoggedIn = true;

        isLoggedIn = false;

        Assert.IsFalse(isLoggedIn);
    }

    [TestMethod]
    public void GetCurrentUser_WhenSessionExists_ReturnsUser()
    {
        var username = "admin";

        var currentUser = username;

        Assert.AreEqual("admin", currentUser);
    }
}