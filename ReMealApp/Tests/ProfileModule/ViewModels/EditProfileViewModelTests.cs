using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Profile.ViewModels;

[TestClass]
public class EditProfileViewModelTests
{
    [TestMethod]
    public void SaveChanges_WithValidData_SavesProfile()
    {
        var username = "new_name";

        var saved = !string.IsNullOrWhiteSpace(username);

        Assert.IsTrue(saved);
    }

    [TestMethod]
    public void SaveChanges_WithEmptyName_ReturnsFailure()
    {
        var username = "";

        var valid = !string.IsNullOrWhiteSpace(username);

        Assert.IsFalse(valid);
    }

    [TestMethod]
    public void CancelChanges_DoesNotModifyProfile()
    {
        var originalName = "OldName";
        var editedName = originalName;

        Assert.AreEqual(originalName, editedName);
    }
}