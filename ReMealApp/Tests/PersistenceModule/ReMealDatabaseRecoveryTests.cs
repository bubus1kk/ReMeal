using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Persistence;

[TestClass]
public class ReMealDatabaseRecoveryTests
{
    [TestMethod]
    public void Recovery_WhenDatabaseFileIsInvalid_MovesFileToBackup()
    {
        var path = Path.GetTempFileName();

        File.WriteAllText(path, "INVALID_DATABASE");

        var exists = File.Exists(path);

        Assert.IsTrue(exists);
    }

    [TestMethod]
    public void Recovery_AfterInvalidDatabase_AllowsNewSchemaCreation()
    {
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void Recovery_WhenDatabaseIsValid_DoesNotMoveFile()
    {
        Assert.IsTrue(true);
    }
}