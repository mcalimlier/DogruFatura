namespace DogruFatura.Wpf;

public static class StartupProgressProfile
{
    public const int MinimumVisibleDurationMs = 5500;
    public const int CruiseDurationMs = 5000;
    public const int TailDurationMs = 5000;
    public const int CompletionDurationMs = 450;
    public const int FrameDelayMs = 33;

    public const int CruiseTargetPercent = 94;
    public const int TailTargetPercent = 98;

    public static int CalculateContinuousPercent(long elapsedMs)
    {
        if (elapsedMs <= 0) return 0;

        if (elapsedMs <= CruiseDurationMs)
        {
            var progress = elapsedMs / (double)CruiseDurationMs;
            return (int)Math.Floor(CruiseTargetPercent * Math.Clamp(progress, 0.0, 1.0));
        }

        var tailElapsed = Math.Min(elapsedMs - CruiseDurationMs, TailDurationMs);
        var tailProgress = tailElapsed / (double)TailDurationMs;
        var percent = CruiseTargetPercent
                      + (TailTargetPercent - CruiseTargetPercent) * Math.Clamp(tailProgress, 0.0, 1.0);
        return Math.Min(TailTargetPercent, (int)Math.Floor(percent));
    }
}
