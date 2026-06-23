using FocusGuard.Services;
using Xunit;

namespace FocusGuard.Local.Tests;

public sealed class SessionExitCoordinatorTests
{
    [Fact]
    public void Complete_AuthorizesBeforeCleanup_AndClosesAfterCleanupFailure()
    {
        var events = new List<string>();

        IReadOnlyList<Exception> errors = SessionExitCoordinator.Complete(
            authorize: () => events.Add("authorized"),
            close: () => events.Add("closed"),
            cleanupSteps: new Action[]
            {
                () =>
                {
                    events.Add("save");
                    throw new InvalidOperationException("database unavailable");
                },
                () => events.Add("web windows closed")
            });

        Assert.Equal(
            new[] { "authorized", "save", "web windows closed", "closed" },
            events);
        Assert.Single(errors);
        Assert.IsType<InvalidOperationException>(errors[0]);
    }
}
