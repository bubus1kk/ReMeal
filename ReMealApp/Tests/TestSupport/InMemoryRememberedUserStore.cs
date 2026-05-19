namespace Tests.TestSupport;

public class InMemoryRememberedUserStore
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