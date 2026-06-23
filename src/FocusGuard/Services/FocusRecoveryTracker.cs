using System;

namespace FocusGuard.Services;

public readonly record struct FocusEnforcementDecision(
    bool CountFocused,
    bool CountDistracted,
    bool WarnAndRestore);

public sealed class FocusRecoveryTracker
{
    private nint _lastAllowedWindow;

    public void RecordAllowedWindow(nint windowHandle)
    {
        if (windowHandle != nint.Zero)
        {
            _lastAllowedWindow = windowHandle;
        }
    }

    public nint ResolveCandidate(Func<nint, bool> isValid, Func<nint> fallback)
    {
        ArgumentNullException.ThrowIfNull(isValid);
        ArgumentNullException.ThrowIfNull(fallback);

        if (_lastAllowedWindow != nint.Zero && isValid(_lastAllowedWindow))
        {
            return _lastAllowedWindow;
        }

        _lastAllowedWindow = fallback();
        return _lastAllowedWindow;
    }

    public static FocusEnforcementDecision Evaluate(bool isResting, bool isAllowed) =>
        isResting
            ? new(false, false, false)
            : isAllowed
                ? new(true, false, false)
                : new(false, true, true);
}
