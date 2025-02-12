using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;

namespace Flow.Launcher.Plugin.CdList;

/// <summary>
/// Directory helper
/// </summary>
public static class DirectoryHelper
{
    /// <summary>
    /// Search directory
    /// </summary>
    /// <param name="path"></param>
    /// <param name="parentDirectories"></param>
    /// <param name="searchOption"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static List<string> GetDirectories(string path,
        List<string> parentDirectories = null,
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new DirectoryNotFoundException();
        }

        var di = new DirectoryInfo(path);
        var isDirectory = di.Exists && di.Attributes.HasFlag(FileAttributes.Directory);
        var isMatch = parentDirectories?.FirstOrDefault(p => p.ToLower().StartsWith(path.ToLower()));
        if (!isDirectory)
        {
            if (isMatch == null)
            {
                throw new DirectoryNotFoundException();
            }

            return new List<string>
            {
                isMatch
            };
        }


        var directories = Directory
            .GetDirectories(path, "*", searchOption: searchOption)
            // Filter out hidden directories
            .Where(d => !new DirectoryInfo(d).Attributes.HasFlag(FileAttributes.Hidden))
            // Has no access to the directory)
            .Where(CheckFolderPermission)
            .ToList();

        directories.Insert(0, path);

        return directories;
    }

    /// <summary>
    /// Check folder permission
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    public static bool CheckFolderPermission(string folderPath)
    {
        var dirInfo = new DirectoryInfo(folderPath);
        try
        {
            _ = dirInfo.GetAccessControl(AccessControlSections.Access);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Get drives
    /// </summary>
    /// <returns></returns>
    public static List<string> GetDrives()
    {
        var drives = Environment.GetLogicalDrives();
        return new List<string>(drives);
    }
}