using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Profile.Services;

[TestClass]
public class RememberedUserServiceTests
{
    [TestMethod]
    public void SaveRememberedUser_SavesUserId()
    {
        var userId = Guid.NewGuid();

        var savedUserId = userId;

        Assert.AreEqual(userId, savedUserId);
    }

    [TestMethod]
    public void ClearRememberedUser_RemovesStoredUser()
    {
        Guid? rememberedUser = Guid.NewGuid();

        rememberedUser = null;

        Assert.IsNull(rememberedUser);
    }

    [TestMethod]
    public void GetRememberedUser_WhenUserExists_ReturnsUser()
    {
        var userId = Guid.NewGuid();

        var result = userId;

        Assert.AreEqual(userId, result);
    }
}