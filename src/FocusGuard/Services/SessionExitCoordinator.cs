using System;
using System.Collections.Generic;

namespace FocusGuard.Services;

public static class SessionExitCoordinator
{
    public static IReadOnlyList<Exception> Complete(
        Action authorize,
        Action close,
        IEnumerable<Action> cleanupSteps)
    {
        ArgumentNullException.ThrowIfNull(authorize);
        ArgumentNullException.ThrowIfNull(close);
        ArgumentNullException.ThrowIfNull(cleanupSteps);

        var errors = new List<Exception>();
        authorize();

        foreach (Action cleanupStep in cleanupSteps)
        {
            try
            {
                cleanupStep();
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }

        try
        {
            close();
        }
        catch (Exception ex)
        {
            errors.Add(ex);
        }

        return errors;
    }
}
