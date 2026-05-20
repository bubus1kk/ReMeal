namespace Tests.Profile.TestSupport;

public class FakeCurrentUser
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;
}