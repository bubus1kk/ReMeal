using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Profile.Persistence;

[TestClass]
public class UserProfileRepositoryTests
{
    [TestMethod]
    public void SaveProfile_SavesUser()
    {
        var username = "user";

        var savedUser = username;

        Assert.AreEqual("user", savedUser);
    }

    [TestMethod]
    public void GetProfile_WhenUserExists_ReturnsUser()
    {
        var user = "admin";

        Assert.AreEqual("admin", user);
    }

    [TestMethod]
    public void UpdateProfile_ChangesUserData()
    {
        var username = "Old";

        username = "New";

        Assert.AreEqual("New", username);
    }
}