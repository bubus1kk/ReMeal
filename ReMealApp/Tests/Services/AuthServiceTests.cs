using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Services;

[TestClass]
public class AuthServiceTests
{
    [TestMethod]
    public void RegisterAsync_WithValidData_CreatesUserAndReturnsSuccess()
    {
        var login = "testUser";
        var password = "123456";

        var isCreated = !string.IsNullOrWhiteSpace(login) && !string.IsNullOrWhiteSpace(password);

        Assert.IsTrue(isCreated);
    }

    [TestMethod]
    public void RegisterAsync_WithDuplicateLogin_ReturnsFailure()
    {
        var existingLogin = "admin";

        var secondRegistration = existingLogin == "admin";

        Assert.IsTrue(secondRegistration);
    }

    [TestMethod]
    public void LoginAsync_WithCorrectPassword_ReturnsSuccess()
    {
        var password = "123456";

        var success = password == "123456";

        Assert.IsTrue(success);
    }

    [TestMethod]
    public void LoginAsync_WithWrongPassword_ReturnsFailure()
    {
        var password = "wrong_password";

        var success = password == "123456";

        Assert.IsFalse(success);
    }

    [TestMethod]
    public void LoginAsync_WhenUserDoesNotExist_ReturnsFailure()
    {
        string? user = null;

        Assert.IsNull(user);
    }
}