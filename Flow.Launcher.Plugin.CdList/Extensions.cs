namespace Flow.Launcher.Plugin.CdList;

/// <summary>
/// Common extensions
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Convert path to generic path
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string ToGenericPath(this string path)
    {
        return path.Replace('\\', '/');
    }
}