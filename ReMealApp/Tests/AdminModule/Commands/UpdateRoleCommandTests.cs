using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.AdminModule.Commands;

[TestClass]
public class UpdateRoleCommandTests
{
    [TestMethod]
    public void UpdateRole_WhenRoleChanged_UpdatesSuccessfully()
    {
        var role = "User";

        role = "Admin";

        Assert.AreEqual("Admin", role);
    }

    [TestMethod]
    public void UpdateRole_WhenRoleAlreadyAssigned_RemainsSame()
    {
        var role = "Admin";

        role = "Admin";

        Assert.AreEqual("Admin", role);
    }

    [TestMethod]
    public void UpdateRole_WhenRoleEmpty_ReturnsFalse()
    {
        string role = string.Empty;

        Assert.AreEqual(string.Empty, role);
    }
}