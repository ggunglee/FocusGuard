using FocusGuard.Services;
using Xunit;

namespace FocusGuard.Local.Tests;

public sealed class FocusRecoveryTrackerTests
{
    [Fact]
    public void RecordAllowedWindow_RemembersMostRecentNonZeroHandle()
    {
        var tracker = new FocusRecoveryTracker();
        tracker.RecordAllowedWindow((nint)11);
        tracker.RecordAllowedWindow((nint)22);
        tracker.RecordAllowedWindow(nint.Zero);

        nint result = tracker.ResolveCandidate(_ => true, () => (nint)99);

        Assert.Equal((nint)22, result);
    }

    [Fact]
    public void ResolveCandidate_UsesFallbackWhenSavedWindowIsInvalid()
    {
        var tracker = new FocusRecoveryTracker();
        tracker.RecordAllowedWindow((nint)11);

        nint result = tracker.ResolveCandidate(handle => handle != (nint)11, () => (nint)44);

        Assert.Equal((nint)44, result);
        Assert.Equal((nint)44, tracker.ResolveCandidate(_ => true, () => (nint)55));
    }

    [Fact]
    public void Evaluate_RequestsWarningAndRestoreOnEveryDistractedTick()
    {
        FocusEnforcementDecision first = FocusRecoveryTracker.Evaluate(isResting: false, isAllowed: false);
        FocusEnforcementDecision second = FocusRecoveryTracker.Evaluate(isResting: false, isAllowed: false);

        Assert.True(first.CountDistracted);
        Assert.True(first.WarnAndRestore);
        Assert.True(second.CountDistracted);
        Assert.True(second.WarnAndRestore);
    }

    [Fact]
    public void Evaluate_CountsFocusedTickWithoutWarning()
    {
        FocusEnforcementDecision decision = FocusRecoveryTracker.Evaluate(isResting: false, isAllowed: true);

        Assert.True(decision.CountFocused);
        Assert.False(decision.CountDistracted);
        Assert.False(decision.WarnAndRestore);
    }

    [Fact]
    public void Evaluate_DoesNothingDuringRest()
    {
        FocusEnforcementDecision decision = FocusRecoveryTracker.Evaluate(isResting: true, isAllowed: false);

        Assert.False(decision.CountFocused);
        Assert.False(decision.CountDistracted);
        Assert.False(decision.WarnAndRestore);
    }
}
