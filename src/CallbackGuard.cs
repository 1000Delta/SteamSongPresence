using System;
using System.Collections.Generic;

namespace SteamSongPresence;

// All callers run on Unity's main thread. A logging failure must not escape either.
internal sealed class CallbackGuard
{
    private readonly Action<string, Exception> report;
    private readonly HashSet<string> failedOperations = new HashSet<string>();

    public CallbackGuard(Action<string, Exception> report) { this.report = report; }

    public bool Run(string operation, Action action)
    {
        try
        {
            action();
            failedOperations.Remove(operation);
            return true;
        }
        catch (Exception error)
        {
            try
            {
                if (failedOperations.Add(operation)) report(operation, error);
            }
            catch (Exception) { /* Error reporting must not interrupt the game. */ }
            return false;
        }
    }
}
