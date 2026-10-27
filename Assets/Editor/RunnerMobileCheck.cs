#if UNITY_EDITOR
// Preserve the existing validation entry point for the combined mobile combat controls.
public static class RunnerMobileCheck
{
    public static void Start() => RunnerSmokeCheck.Start();
}
#endif
