using FocusGuard.Infrastructure;
using Xunit;

namespace FocusGuard.Local.Tests;

public sealed class UserDataPathsTests
{
    [Fact]
    public void Paths_AreRootedUnderLocalAppData()
    {
        string expectedRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusGuard");

        Assert.Equal(expectedRoot, UserDataPaths.RootDirectory);
        Assert.Equal(Path.Combine(expectedRoot, "focusguard.db"), UserDataPaths.DatabasePath);
        Assert.Equal(Path.Combine(expectedRoot, "memo.txt"), UserDataPaths.MemoPath);
    }
}
