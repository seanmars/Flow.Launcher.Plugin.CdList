using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.CdList
{
    /// <summary>
    /// Plugin that cd into the selected folder
    /// </summary>
    public class CdList : IAsyncPlugin, IResultUpdated, IContextMenu
    {
        private const string NotExistMessage = "Path does not exist";

        private const string IconFolder = "folder.png";
        private const string IconFolderFail = "folder_fail.png";

        private const string IconTerminalWindowsTerminal = "wt.png";
        private const string IconTerminalPowershell = "powershell.png";
        private const string IconTerminalCmd = "cmd.png";
        private const string IconTerminalPowershellCore = "pwsh.png";

        private PluginInitContext _context;

        private TerminalKind _terminalKind = TerminalKind.WindowsTerminal;

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
            var results = GetResults(query.Search);

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
            _terminalKind = TerminalHelper.GetDefaultTerminal();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Method that is called when the user selects a result
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public List<Result> LoadContextMenus(Result result)
        {
            var menuOptions = new List<Result>();
            var folderPath = result.ContextData.ToString();

            menuOptions.Add(new Result
            {
                Title = "Open in terminal",
                SubTitle = folderPath,
                IcoPath = UseTerminalIcon(),
                Action = _ =>
                {
                    try
                    {
                        OpenFolderInTerminal(folderPath, _terminalKind);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        return false;
                    }
                }
            });


            return menuOptions;
        }

        private string UseTerminalIcon()
        {
            return _terminalKind switch
            {
                TerminalKind.WindowsTerminal => IconTerminalWindowsTerminal,
                TerminalKind.PowerShell => IconTerminalPowershell,
                TerminalKind.Cmd => IconTerminalCmd,
                TerminalKind.PowerShellCore => IconTerminalPowershellCore,
                _ => IconTerminalCmd
            };
        }

        private List<Result> GetDrivesResult()
        {
            var drives = DirectoryHelper.GetDrives()
                .ConvertAll(p => new Result
                {
                    Title = p,
                    ContextData = p,
                    IcoPath = IconFolder,
                    Action = Action(p),
                });

            return drives;
        }

        private List<Result> GetDirectoryResult(string path)
        {
            path = path.ToGenericPath();
            var parentDirectories = new List<string>();
            if (path.Contains('/'))
            {
                var splitPath = path.Split('/').ToList();
                if (splitPath.Count > 1)
                {
                    splitPath.RemoveAt(splitPath.Count - 1);
                    splitPath[0] += "/";
                    var parentPath = Path.Combine(splitPath.ToArray()).ToGenericPath();
                    parentDirectories = DirectoryHelper.GetDirectories(parentPath);
                }
            }

            var directories = DirectoryHelper.GetDirectories(path, parentDirectories)
                .ConvertAll(p =>
                {
                    p = p.ToGenericPath();
                    return new Result
                    {
                        Title = p,
                        ContextData = p,
                        IcoPath = IconFolder,
                        Action = Action(p),
                    };
                });

            return directories;
        }

        private List<Result> GetResults(string path)
        {
            var results = new List<Result>();

            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    results.AddRange(GetDrivesResult());
                }
                else
                {
                    results.AddRange(GetDirectoryResult(path));
                }

                foreach (var r in results)
                {
                    var fullPath = r.ContextData.ToString();
                    r.SubTitle = new DirectoryInfo(fullPath!).Name;
                }

                return results;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                results.Add(new Result
                {
                    Title = NotExistMessage,
                    IcoPath = IconFolderFail,
                    Action = Action(path),
                });

                return results;
            }
        }

        private static Func<ActionContext, bool> Action(string text)
        {
            return actionContext =>
            {
                try
                {
                    OpenFolder(text);
                    return true;
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    return false;
                }
            };
        }

        private static void OpenFolderInTerminal(string path, TerminalKind kind)
        {
            switch (kind)
            {
                case TerminalKind.WindowsTerminal:
                    OpenFolderInWindowsTerminal(path);
                    break;
                case TerminalKind.PowerShell:
                    OpenFolderInPowershell(path);
                    break;
                case TerminalKind.Cmd:
                    OpenFolderInCmd(path);
                    break;
                case TerminalKind.PowerShellCore:
                    OpenFolderInPowershellCore(path);
                    break;
                default:
                    OpenFolder(path);
                    break;
            }
        }

        private static void OpenFolderInWindowsTerminal(string path)
        {
            try
            {
                path = path.Replace('\\', '/');

                if (!Directory.Exists(path))
                {
                    return;
                }

                Process.Start(new ProcessStartInfo()
                {
                    FileName = "wt",
                    Arguments = $"-d \"{path}\"",
                    UseShellExecute = true,
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void OpenFolderInCmd(string path)
        {
            try
            {
                path = path.Replace('\\', '/');

                if (!Directory.Exists(path))
                {
                    return;
                }

                Process.Start(new ProcessStartInfo()
                {
                    FileName = "cmd",
                    Arguments = $"/K cd \"{path}\"",
                    UseShellExecute = true,
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void OpenFolderInPowershell(string path)
        {
            try
            {
                path = path.Replace('\\', '/');

                if (!Directory.Exists(path))
                {
                    return;
                }

                Process.Start(new ProcessStartInfo()
                {
                    FileName = "powershell",
                    Arguments = $"-NoExit -Command \"cd '{path}'\"",
                    UseShellExecute = true,
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void OpenFolderInPowershellCore(string path)
        {
            try
            {
                path = path.Replace('\\', '/');

                if (!Directory.Exists(path))
                {
                    return;
                }

                Process.Start(new ProcessStartInfo()
                {
                    FileName = "pwsh",
                    Arguments = $"-NoExit -Command \"cd '{path}'\"",
                    UseShellExecute = true,
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void OpenFolder(string path)
        {
            try
            {
                path = path.Replace('\\', '/');

                if (!Directory.Exists(path))
                {
                    return;
                }

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