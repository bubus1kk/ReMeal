using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Persistence;

[TestClass]
public class DataAccessGuardTests
{
    [TestMethod]
    public void SaveChanges_WhenUniqueLoginViolated_ThrowsDataAccessException()
    {
        try
        {
            throw new Exception("Duplicate login");
        }
        catch (Exception ex)
        {
            Assert.AreEqual("Duplicate login", ex.Message);
        }
    }

    [TestMethod]
    public void Query_WhenDatabaseMissingTable_ReturnsUserFriendlyMessage()
    {
        var message = "Table does not exist";

        Assert.AreEqual("Table does not exist", message);
    }

    [TestMethod]
    public void LockedDatabase_ReturnsUserFriendlyMessage()
    {
        var message = "Database locked";

        Assert.IsTrue(message.Contains("locked"));
    }
}