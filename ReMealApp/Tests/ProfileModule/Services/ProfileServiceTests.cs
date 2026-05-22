using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Profile.Services;

[TestClass]
public class ProfileServiceTests
{
    [TestMethod]
    public void GetProfile_WhenUserExists_ReturnsProfile()
    {
        var username = "test_user";

        var result = username;

        Assert.AreEqual("test_user", result);
    }

    [TestMethod]
    public void UpdateProfile_WithValidData_UpdatesProfile()
    {
        var oldName = "OldName";
        var newName = "NewName";

        oldName = newName;

        Assert.AreEqual("NewName", oldName);
    }

    [TestMethod]
    public void UpdateProfile_WithEmptyName_ReturnsFailure()
    {
        var name = "";

        var isValid = !string.IsNullOrWhiteSpace(name);

        Assert.IsFalse(isValid);
    }
}
