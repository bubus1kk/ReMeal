namespace Tests.TestSupport;

public class FakePasswordHasher
{
    public string Hash(string password)
    {
        return $"HASH_{password}";
    }

    public bool Verify(
        string password,
        string hash)
    {
        return hash == $"HASH_{password}";
    }
}