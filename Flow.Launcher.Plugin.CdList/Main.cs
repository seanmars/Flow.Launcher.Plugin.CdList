using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.CdList
{
    /// <summary>
    /// Plugin that cd into the selected folder
    /// </summary>
    public class CdList : IAsyncPlugin, IResultUpdated
    {
        private const string IconFolder = "folder.png";

        private PluginInitContext _context;

        private readonly SearchOption _searchOption = SearchOption.TopDirectoryOnly;

        /// <summary>
        /// Event that is triggered when the results are updated
        /// </summary>
        public event ResultUpdatedEventHandler ResultsUpdated;

        /// <summary>
        /// Method that is called when the user types in the Flow Launcher search bar
        /// </summary>
        /// <param name="query"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var results = new List<Result>();

            if (query.Search.Length == 0)
            {
                var drives = GetDrives()
                    .ConvertAll(d => new Result
                    {
                        Title = d,
                        IcoPath = IconFolder,
                        Action = Action(d),
                    });

                results.AddRange(drives);
            }
            else
            {
                var path = query.Search.Trim();

                var directories = GetDirectories(path)
                    .ConvertAll(d => new Result
                    {
                        Title = d,
                        IcoPath = IconFolder,
                        Action = Action(d),
                    });

                results.AddRange(directories);
            }

            ResultsUpdated?.Invoke(this, new ResultUpdatedEventArgs
            {
                Query = query,
                Results = results
            });

            return Task.FromResult(results);
        }

        /// <summary>
        /// Method that is called when the plugin is initialized
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task InitAsync(PluginInitContext context)
        {
            _context = context;
            return Task.CompletedTask;
        }

        private List<string> GetDirectories(string path)
        {
            var directories = Directory
                .GetDirectories(path, "*", searchOption: _searchOption)
                // Filter out hidden directories
                .Where(d => !new DirectoryInfo(d).Attributes.HasFlag(FileAttributes.Hidden))
                // Has no access to the directory)
                .Where(CheckFolderPermission)
                .ToList();

            if (directories.Any())
            {
                return directories;
            }

            var isDirectory = new DirectoryInfo(path).Attributes.HasFlag(FileAttributes.Directory);
            directories.Add(isDirectory
                ? path
                : "No directories found");

            return directories;
        }

        private bool CheckFolderPermission(string folderPath)
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

        private List<string> GetDrives()
        {
            var drives = Environment.GetLogicalDrives();
            return new List<string>(drives);
        }

        private static Func<ActionContext, bool> Action(string text)
        {
            return e =>
            {
                OpenFolder(text);
                return true;
            };
        }

        private static void OpenFolder(string path)
        {
            try
            {
                path = path.Replace('\\', '/');

                Process.Start(new ProcessStartInfo()
                {
                    FileName = path,
                    Verb = "open",
                    UseShellExecute = true,
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}