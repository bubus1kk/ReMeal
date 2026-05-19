namespace Application.Interfaces
{
    public interface IRememberedUserStore
    {
        Guid? GetRememberedUserId();

        void RememberUser(Guid userId);

        void ForgetUser();
    }
}
