namespace AutoStartConfirm.Helpers
{
    public class UacService : IUacService
    {
        public bool IsProcessElevated => System.Environment.IsPrivilegedProcess;
    }
}
