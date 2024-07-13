namespace AutoStartConfirm.Helpers
{
    public interface IUacService
    {
        bool IsProcessElevated { get; }
    }
}