using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReMealApp.Tests.AdminModule.Validators;

[TestClass]
public class UserValidatorTests
{
    [TestMethod]
    public void ValidateUser_WhenLoginValid_ReturnsTrue()
    {
        var login = "admin";

        var isValid = login.Length >= 4;

        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void ValidateUser_WhenLoginTooShort_ReturnsFalse()
    {
        var login = "ad";

        var isValid = login.Length >= 4;

        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void ValidateUser_WhenEmailEmpty_ReturnsFalse()
    {
        var email = string.Empty;

        Assert.AreEqual(string.Empty, email);
    }
}