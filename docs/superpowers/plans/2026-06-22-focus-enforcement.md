# FocusGuard Focus Enforcement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show real input placeholders and repeatedly warn and restore the last allowed window whenever the user activates an unauthorized program during focus time.

**Architecture:** A small pure state component decides focus-versus-distraction behavior and remembers the last valid allowed window, making the policy unit-testable. `WindowManager` owns Win32 discovery and restoration, while both original and local `StudyWindow` files call the shared policy from their existing one-second timer. The original `MainWindow.xaml` remains the single UI source linked by both builds.

**Tech Stack:** .NET 8 WPF, C# 12, Win32 user32 APIs, xUnit

---

### Task 1: Lock focus policy with tests

**Files:**
- Create: `src/FocusGuard/Services/FocusRecoveryTracker.cs`
- Create: `tests/FocusGuard.Local.Tests/FocusRecoveryTrackerTests.cs`
- Modify: `tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj`

- [ ] **Step 1: Write failing tests**

Test that the most recently recorded nonzero window wins, zero handles do not overwrite it, an invalid saved handle falls back to the supplied candidate, two consecutive unauthorized evaluations both request warning/restoration, allowed focus counts as focused, and rest requests neither counting nor recovery.

- [ ] **Step 2: Run the focused tests**

Run: `dotnet test tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj --filter FocusRecoveryTrackerTests`

Expected: FAIL because the shared policy does not exist.

- [ ] **Step 3: Implement the pure policy**

```csharp
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
        if (windowHandle != nint.Zero) _lastAllowedWindow = windowHandle;
    }

    public nint ResolveCandidate(Func<nint, bool> isValid, Func<nint> fallback)
    {
        if (_lastAllowedWindow != nint.Zero && isValid(_lastAllowedWindow))
            return _lastAllowedWindow;

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
```

- [ ] **Step 4: Run the tests**

Expected: all `FocusRecoveryTrackerTests` pass.

### Task 2: Add robust Win32 window restoration

**Files:**
- Modify: `src/FocusGuard/Services/WindowManager.cs`

- [ ] **Step 1: Add observable window APIs**

Expose `GetForegroundWindowHandle()`, `IsWindowAvailable(nint)`, `IsTargetWindow(nint, string)`, `FindFirstTargetWindow(IEnumerable<string>)`, and `TryBringWindowToForeground(nint)`. Reuse process-ID matching rather than comparing titles.

- [ ] **Step 2: Implement restoration with a bounded fallback**

`TryBringWindowToForeground` must reject zero/invalid handles, call `ShowWindow(handle, SW_RESTORE)`, try `SetForegroundWindow`, and if that fails pulse z-order with `SetWindowPos(HWND_TOPMOST)` followed by `SetWindowPos(HWND_NOTOPMOST)` before trying `SetForegroundWindow` once more. Return the observed success result and never throw for a process that closes mid-call.

- [ ] **Step 3: Preserve existing callers**

Change `BringTargetToForeground(string)` to find the first target handle and call the new method without changing its public signature.

- [ ] **Step 4: Build both projects**

Run:

```powershell
dotnet build src/FocusGuard/FocusGuard.csproj -c Debug
dotnet build src/FocusGuard.Local/FocusGuard.Local.csproj -c Debug
```

Expected: both builds succeed.

### Task 3: Wire repeated warning and last-window recovery

**Files:**
- Modify: `src/FocusGuard/StudyWindow.xaml.cs`
- Modify: `src/FocusGuard.Local/StudyWindow.xaml.cs`

- [ ] **Step 1: Add the shared tracker**

Create one `FocusRecoveryTracker` field per study session. Replace the boolean-only allowed check with `TryGetAllowedRecoveryWindow(out nint recoveryHandle)`. FocusGuard's widget, memo, and dictionary windows return allowed with a zero handle. Active linked WebView and registered external app windows return allowed with their actual handle.

- [ ] **Step 2: Apply the decision on every timer tick**

During focus, record a nonzero allowed handle and increment focused time. During distraction, increment distracted time, update the red status, call `SystemSounds.Hand.Play()`, resolve the last valid candidate, and call `TryBringWindowToForeground`. Because the branch executes every second, warning and recovery repeat until an allowed window is active.

- [ ] **Step 3: Implement fallback order**

When the saved handle is invalid, search `_targetApps` in registration order; if none is running, choose the first visible linked WebView handle. Do not launch a missing application.

- [ ] **Step 4: Keep original and local behavior identical**

Apply the same enforcement methods to both `StudyWindow` files while retaining the local file's `UserDataPaths.MemoPath` difference.

- [ ] **Step 5: Run all tests and both builds**

Run the full xUnit project and both Debug builds. Expected: seven existing tests plus the new policy tests pass; both builds succeed.

### Task 4: Add non-persistent placeholders

**Files:**
- Modify: `src/FocusGuard/MainWindow.xaml`

- [ ] **Step 1: Overlay the title placeholder**

After `InputTitle`, add a `TextBlock` in column 0 with text `과제명`, `IsHitTestVisible="False"`, muted foreground, and a style whose `DataTrigger` collapses it when `InputTitle.Text` is not empty.

- [ ] **Step 2: Overlay the resource placeholder**

After `InputRes`, add the equivalent `URL 또는 파일 경로` overlay. The overlay is not part of TextBox.Text and therefore cannot be saved as mission data.

- [ ] **Step 3: Build both projects again**

Expected: XAML compiles in both the original project and the linked local project.

### Task 5: Runtime QA and cleanup

**Files:**
- Temporary: `.debug-journal.md` (excluded locally and removed after QA)

- [ ] **Step 1: Launch the original Debug build**

Start a short session with Notepad allowed, verify both placeholders, switch to another ordinary app, and observe a warning plus automatic return to Notepad. Repeat after switching between two allowed apps to verify the last-used app wins.

- [ ] **Step 2: Verify repeated failure behavior**

Close all allowed apps during a focus session and activate another app. Observe that warning repeats at roughly one-second intervals without launching the missing allowed program.

- [ ] **Step 3: Launch the local Debug build**

Repeat the allowed/disallowed transition and confirm local translation/history still work.

- [ ] **Step 4: Clean debug artifacts**

Remove the debug journal and any temporary session data created solely for QA. Confirm source changes are limited to the approved feature and the user's pre-existing theme work remains untouched.

### Task 6: User checkpoint before final release

- [ ] **Step 1: Run the local development build visibly**

Leave the tested Debug executable open for user inspection.

- [ ] **Step 2: Stop before release publication**

Do not build the final BAT/single-EXE distribution until the user confirms placeholders, warning repetition, and automatic recovery.
