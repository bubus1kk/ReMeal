namespace Tests.Profile.TestSupport;

public class FakeRememberedUserStore
{
    private Guid? _userId;

    public void Save(Guid userId)
    {
        _userId = userId;
    }

    public Guid? Get()
    {
        return _userId;
    }

    public void Clear()
    {
        _userId = null;
    }
}