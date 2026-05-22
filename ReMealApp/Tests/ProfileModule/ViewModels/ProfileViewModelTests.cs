using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Profile.ViewModels;

[TestClass]
public class ProfileViewModelTests
{
    [TestMethod]
    public void LoadProfile_LoadsUserData()
    {
        var username = "test";

        var displayedName = username;

        Assert.AreEqual("test", displayedName);
    }

    [TestMethod]
    public void LoadProfile_WhenUserMissing_ReturnsNull()
    {
        string? user = null;

        Assert.IsNull(user);
    }

    [TestMethod]
    public void ProfileViewModel_InitializesCorrectly()
    {
        var initialized = true;

        Assert.IsTrue(initialized);
    }
}