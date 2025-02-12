using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Flow.Launcher.Plugin.CdList;

/// <summary>
/// 
/// </summary>
public static class TerminalHelper
{
    /// <summary>
    /// Get default terminal
    /// </summary>
    /// <returns></returns>
    public static TerminalKind GetDefaultTerminal()
    {
        if (IsWindowsTerminalInstalled())
        {
            return TerminalKind.WindowsTerminal;
        }

        if (IsPwshInstalled())
        {
            return TerminalKind.PowerShellCore;
        }

        if (IsPowerShellInstalled())
        {
            return TerminalKind.PowerShell;
        }

        if (IsCmdInstalled())
        {
            return TerminalKind.Cmd;
        }

        return TerminalKind.Cmd;
    }

    private static bool IsWindowsTerminalInstalled()
    {
        // 檢查 Windows Terminal 是否安裝
        try
        {
            // 檢查 Windows Terminal 的安裝路徑
            string[] possiblePaths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft",
                    "WindowsApps", "wt.exe")
            };

            Console.WriteLine(JsonSerializer.Serialize(possiblePaths));

            return possiblePaths.Any(path =>
                File.Exists(path) || Directory.GetFiles(Path.GetDirectoryName(path) ?? string.Empty, "wt.exe",
                    SearchOption.AllDirectories).Length != 0);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPwshInstalled()
    {
        try
        {
            // 檢查 PowerShell Core (pwsh) 是否安裝
            using var process = new Process();
            process.StartInfo.FileName = "pwsh.exe";
            process.StartInfo.Arguments = "-NoProfile -Command \"exit\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPowerShellInstalled()
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = "-NoProfile -Command \"exit\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCmdInstalled()
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c exit";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            return true;
        }
        catch
        {
            return false;
        }
    }
}