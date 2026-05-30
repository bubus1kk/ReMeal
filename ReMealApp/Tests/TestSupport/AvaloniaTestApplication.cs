using Avalonia;
using ReMealApp;

namespace Tests.TestSupport;

internal static class AvaloniaTestApplication
{
    private static readonly object SyncRoot = new();

    private static bool _isInitialized;

    public static void EnsureInitialized()
    {
        if (_isInitialized)
            return;

        lock (SyncRoot)
        {
            if (_isInitialized)
                return;

            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .SetupWithoutStarting();

            _isInitialized = true;
        }
    }
}
