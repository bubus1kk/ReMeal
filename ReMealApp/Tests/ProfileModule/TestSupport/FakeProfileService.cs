namespace Tests.Profile.TestSupport;

public class FakeProfileService
{
    public string GetProfileName()
    {
        return "TestUser";
    }

    public bool UpdateProfile(string username)
    {
        return !string.IsNullOrWhiteSpace(username);
    }
}