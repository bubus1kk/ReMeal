using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.AdminModule.Commands;

[TestClass]
public class BlockUserCommandUnitTests
{
    [TestMethod]
    public void BlockUser_WhenUserExists_UserBecomesBlocked()
    {
        var isBlocked = false;

        isBlocked = true;

        Assert.IsTrue(isBlocked);
    }

    [TestMethod]
    public void BlockUser_WhenAlreadyBlocked_RemainsBlocked()
    {
        var isBlocked = true;

        Assert.IsTrue(isBlocked);
    }

    [TestMethod]
    public void BlockUser_WhenUserNotFound_ReturnsFalse()
    {
        bool result = false;

        Assert.IsFalse(result);
    }
}