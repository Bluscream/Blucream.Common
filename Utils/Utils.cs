using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using Bluscream.MoreChatNotifications;
using MelonLoader;
using OverwolfPatcher.Classes;

namespace Bluscream;

public static class Utils
{
    public enum ShowWindowCommand
    {
        SW_HIDE = 0,
        SW_SHOW = 5,
    }

    public enum FocusAssistState
    {
        OFF = 0,
        PRIORITY_ONLY = 1,
        ALARMS_ONLY = 2,
    }

    const int defaultMinWidth = 80;
    const int defaultPadding = 10;
    public static bool _consoleEnabled = false;

    [DllImport("kernel32.dll", SetLastError = true)]
    public extern bool AllocConsole();

    [DllImport("User32.dll")]
    public static extern Int32 SetForegroundWindow(int hWnd);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool GetFocusAssistState(out int state);

    public static List<int> GetPadding(string input, int minWidth = 80, int padding = 10)
    {
        int totalWidth = minWidth + padding * 2;
        int leftPadding = (totalWidth - input.Length) / 2;
        int rightPadding = totalWidth - input.Length - leftPadding;
        return new List<int> { leftPadding, rightPadding, totalWidth };
    }

    public static string Pad(string input, string outer = "||", int minWidth = 80, int padding = 10)
    {
        var padded = GetPadding(input, minWidth, padding);
        return $"{outer}{new string(' ', padded[index: 0])}{input}{new string(' ', padded[1])}{outer}";
    }

    public static string Log(string text, int length = 73)
    {
        text = "|| " + text;
        for (int i = 0; text.Length < length; i++)
        {
            text += " ";
        }
        text = text + " ||";
        Console.WriteLine(text);
        return text;
    }

    public List<string> removeFromToRow(string from, string where, string to, string insert = "")
    {
        List<string> list;
        if (where.Contains("\r\n"))
            list = where.Split(new[] { "\r\n" }, StringSplitOptions.None).ToList();
        else
            list = where.Split(new[] { "\n" }, StringSplitOptions.None).ToList();
        return removeFromToRow(from, list, to, insert);
    }

    public List<string> removeFromToRow(
        string from,
        List<string> where,
        string to,
        string insert = ""
    )
    {
        int start = -1;
        int end = -1;
        for (int i = 0; i < where.Count; i++)
        {
            if (where[i] == from)
            {
                start = i;
            }
            if (start != -1 && where[i] == to)
            {
                end = i;
                break;
            }
        }
        if (start != -1 && end != -1)
        {
            where.RemoveRange(start, end - start + 1);
        }
        if (insert != "")
        {
            where.Insert(start, insert);
        }
        return where;
    }

    public static void Exit(int exitCode = 0)
    {
        Environment.Exit(exitCode);
        var currentP = Process.GetCurrentProcess();
        currentP.Kill();
    }

    public static void RestartAsAdmin(string[] arguments)
    {
        if (IsAdmin())
            return;
        ProcessStartInfo proc = new ProcessStartInfo();
        proc.UseShellExecute = true;
        proc.WorkingDirectory = Environment.CurrentDirectory;
        proc.FileName = Assembly.GetEntryAssembly().CodeBase;
        proc.Arguments += arguments.ToString();
        proc.Verb = "runas";
        try
        {
            Process.Start(proc);
            Exit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to restart as admin automatically: {ex.Message}");
            Console.WriteLine(
                "This app has to run with elevated permissions (Administrator) to be able to modify files in the Overwolf folder!"
            );
            Console.ReadKey();
            Exit();
        }
    }

    public static bool IsAdmin()
    {
        bool isAdmin;
        try
        {
            WindowsIdentity user = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(user);
            isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (UnauthorizedAccessException)
        {
            isAdmin = false;
        }
        catch (Exception)
        {
            isAdmin = false;
        }
        return isAdmin;
    }

    public static List<int> GetPadding(
        string input,
        int minWidth = defaultMinWidth,
        int padding = defaultPadding
    )
    {
        int totalWidth = minWidth + padding * 2;
        int leftPadding = (totalWidth - input.Length) / 2;
        int rightPadding = totalWidth - input.Length - leftPadding;
        return new List<int> { leftPadding, rightPadding, totalWidth };
    }

    public static string Pad(
        string input,
        string outer = "||",
        int minWidth = defaultMinWidth,
        int padding = defaultPadding
    )
    {
        var padded = GetPadding(input, minWidth, padding);
        return $"{outer}{new string(' ', Math.Max(padded[index: 0], 0))}{input}{new string(' ', Math.Max(padded[1], 0))}{outer}";
    }

    public static string Fill(char c, int width = defaultMinWidth, int padding = defaultPadding)
    {
        return new string(c, width + padding * 2 + 4);
    }







    public static void ErrorAndExit(string message, bool reinstall_overwolf = false)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        if (reinstall_overwolf)
            Overwolf.DownloadUrl.OpenInDefaultBrowser();
        Exit(1);
    }

    public static void CreateConsole()
    {
        AllocConsole();
        _consoleEnabled = true;
    }

    public static void SetConsoleTitle(string title)
    {
        Console.Title = title;
    }

    public static void SetConsoleEnabled(bool enabled)
    {
        _consoleEnabled = enabled;
    }

    public static void Log(object message, params object[] args)
    {
        if (!_consoleEnabled)
            return;
        var msg = message?.ToString() ?? string.Empty;
        Console.WriteLine(args != null && args.Length > 0 ? string.Format(msg, args) : msg);
    }

    public static string GetOwnPath()
    {
        var possiblePaths = new List<string?>
        {
            Process.GetCurrentProcess().MainModule?.FileName,
            AppContext.BaseDirectory,
            Environment.GetCommandLineArgs().FirstOrDefault(),
            Assembly.GetEntryAssembly()?.Location,
            ".",
        };
        foreach (var path in possiblePaths.Where(p => !string.IsNullOrEmpty(p)))
        {
            if (System.IO.File.Exists(path!))
            {
                return System.IO.Path.GetFullPath(path!);
            }
        }
        return string.Empty;
    }

    public static bool IsRunAsAdmin()
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public static void RelaunchAsAdmin(string[] args)
    {
        var exeName = GetOwnPath();
        var startInfo = new ProcessStartInfo(exeName)
        {
            UseShellExecute = true,
            Verb = "runas",
            Arguments = args != null ? string.Join(" ", args) : string.Empty,
        };
        Process.Start(startInfo);
    }









    public static void BringSelfToFront()
    {
        var window = Program.mainWindow;
        if (window.WindowState == FormWindowState.Minimized)
            window.WindowState = FormWindowState.Normal;
        else
        {
            window.TopMost = true;
            window.Focus();
            window.BringToFront();
            window.TopMost = false;
        }
        Program.mainWindow.Activate();
        Program.mainWindow.Focus();
        SetForegroundWindow(SafeHandle.ToInt32());
    }

    public static bool IsAlreadyRunning(string appName)
    {
        System.Threading.Mutex m = new System.Threading.Mutex(false, appName);
        if (m.WaitOne(1, false) == false)
        {
            return true;
        }
        return false;
    }

    public static void Exit()
    {
        Application.Exit();
        var currentP = Process.GetCurrentProcess();
        currentP.Kill();
    }



    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }

    public static FileInfo DownloadFile(
        string url,
        DirectoryInfo destinationPath,
        string fileName = null
    )
    {
        if (fileName == null)
            fileName = url.Split('/').Last();
        // Main.webClient.DownloadFile(url, Path.Combine(destinationPath.FullName, fileName));
        return new FileInfo(Path.Combine(destinationPath.FullName, fileName));
    }

    public static FileInfo pickFile(
        string title = null,
        string initialDirectory = null,
        string filter = null
    )
    {
        using (var fileDialog = new OpenFileDialog())
        {
            if (title != null)
                fileDialog.Title = title;
            fileDialog.InitialDirectory =
                initialDirectory ?? "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
            if (filter != null)
                fileDialog.Filter = filter;
            fileDialog.Multiselect = false;
            var result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var file = new FileInfo(fileDialog.FileName);
                if (file.Exists)
                    return file;
            }
            return null;
        }
    }

    public static FileInfo saveFile(
        string title = null,
        string initialDirectory = null,
        string filter = null,
        string fileName = null,
        string content = null
    )
    {
        using (var fileDialog = new SaveFileDialog())
        {
            if (title != null)
                fileDialog.Title = title;
            fileDialog.InitialDirectory =
                initialDirectory ?? "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
            if (filter != null)
                fileDialog.Filter = filter;
            fileDialog.FileName = fileName ?? null;
            var result = fileDialog.ShowDialog();
            if (result != DialogResult.OK || fileDialog.FileName.IsNullOrWhiteSpace())
                return null;
            if (content != null)
            {
                using (var fileStream = fileDialog.OpenFile())
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(content);
                    fileStream.Write(info, 0, info.Length);
                }
            }
            return new FileInfo(fileDialog.FileName);
        }
    }

    public static DirectoryInfo pickFolder(string title = null, string initialDirectory = null)
    {
        Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog dialog =
            new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
        if (title != null)
            dialog.Title = title;
        dialog.IsFolderPicker = true;
        dialog.DefaultDirectory = initialDirectory ?? "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
        if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
        {
            var dir = new DirectoryInfo(dialog.FileName);
            if (dir.Exists)
                return dir;
        }
        return null;
    }

    public static Process StartProcess(FileInfo file, params string[] args) =>
        StartProcess(file.FullName, file.DirectoryName, args);

    public static Process StartProcess(string file, string workDir = null, params string[] args)
    {
        ProcessStartInfo proc = new ProcessStartInfo();
        proc.FileName = file;
        proc.Arguments = string.Join(" ", args);
        Logger.Debug("Starting Process: {0} {1}", proc.FileName, proc.Arguments);
        if (workDir != null)
        {
            proc.WorkingDirectory = workDir;
            Logger.Debug("Working Directory: {0}", proc.WorkingDirectory);
        }
        return Process.Start(proc);
    }

    public static IPEndPoint ParseIPEndPoint(string endPoint)
    {
        string[] ep = endPoint.Split(':');
        if (ep.Length < 2)
            return null;
        IPAddress ip;
        if (ep.Length > 2)
        {
            if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
            {
                return null;
            }
        }
        else
        {
            if (!IPAddress.TryParse(ep[0], out ip))
            {
                return null;
            }
        }
        int port;
        if (
            !int.TryParse(
                ep[ep.Length - 1],
                NumberStyles.None,
                NumberFormatInfo.CurrentInfo,
                out port
            )
        )
        {
            return null;
        }
        return new IPEndPoint(ip, port);
    }

    public static MelonLogger.Instance Logger = new(
        HTTPServer.Properties.AssemblyInfoParams.Name,
        color: System.Drawing.Color.DarkCyan
    );

    public static void Debug(object message, params object[] parms)
    {
        if (!PluginConfig.EnableLogging.Value)
            return;
    }

    public static void Log(object message, params object[] parms)
    {
        if (!PluginConfig.EnableLogging.Value)
            return;
        Logger.Msg(message.ToString(), parms);
    }

    public static void Error(object message, params object[] parms)
    {
        if (!PluginConfig.EnableLogging.Value)
            return;
        Logger.Error(message.ToString(), parms);
    }

    public static void BigError(object message)
    {
        if (!PluginConfig.EnableLogging.Value)
            return;
        Logger.BigError(message.ToString());
    }

    public static void Warn(object message, params object[] parms) => Warn(message, parms);

    public static void Warning(object message, params object[] parms)
    {
        if (!PluginConfig.EnableLogging.Value)
            return;
        Logger.Warning(message.ToString(), parms);
    }






    public static void HUDNotify(
        string header = null,
        string subtext = null,
        string cat = null,
        float? time = null
    )
    {
        if (!ModConfig.EnableMod.Value || !ModConfig.EnableHUDNotifications.Value)
            return;
        cat ??= $"(Local) {MoreChatNotifications.Properties.AssemblyInfoParams.Name}";
        if (time != null)
        {
            ViewManager.Instance.NotifyUser(cat, subtext, time.Value);
        }
        else
        {
            ViewManager.Instance.NotifyUserAlert(cat, header, subtext);
        }
    }

    public static void SendChatNotification(
        object text,
        bool sendSoundNotification = false,
        bool displayInHistory = false
    )
    {
        if (!ModConfig.EnableMod.Value || !ModConfig.EnableChatNotifications.Value)
            return;
        Kafe.ChatBox.API.SendMessage(
            text.ToString(),
            sendSoundNotification: sendSoundNotification,
            displayInChatBox: true,
            displayInHistory: displayInHistory
        );
    }

    public static Color GetColor(List<ushort> _c) => Color.FromArgb(_c[0], _c[1], _c[2], _c[3]);

    public static string GetPlayerNameById(string playerId)
    {
        return IsLocalPlayer(playerId)
            ? "You"
            : "\"" + CVRPlayerManager.Instance.TryGetPlayerName(playerId) + "\"";
    }

    public static bool IsLocalPlayer(string playerId)
    {
        return playerId == MetaPort.Instance.ownerId;
    }








    public static bool PropsAllowed()
    {
        if (!ModConfig.EnableMod.Value)
            return false;
        if (!CVRSyncHelper.IsConnectedToGameNetwork())
        {
            HUDNotify("Cannot spawn prop", "Not connected to an online Instance");
            return false;
        }
        else if (!MetaPort.Instance.worldAllowProps)
        {
            HUDNotify("Cannot spawn prop", "Props are not allowed in this world");
            return false;
        }
        else if (!MetaPort.Instance.settings.GetSettingsBool("ContentFilterPropsEnabled", false))
        {
            HUDNotify("Cannot spawn prop", "Props are disabled in content filter");
            return false;
        }
        return true;
    }










    public static void ShowFileInExplorer(FileInfo file)
    {
        StartProcess("explorer.exe", null, "/select, " + file.FullName.Quote());
    }

    public static void OpenFolderInExplorer(DirectoryInfo dir)
    {
        StartProcess("explorer.exe", null, dir.FullName.Quote());
    }

        StartProcess(file.FullName, file.DirectoryName, args);














        StartProcess(file.FullName, file.DirectoryName, args);














        StartProcess(file.FullName, file.DirectoryName, args);


    public static void HideConsoleWindow()
    {
        try
        {
            var handle = GetConsoleWindow();
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, (int)ShowWindowCommand.SW_HIDE);
            }
            var process = Process.GetCurrentProcess();
            if (process != null && process.MainWindowHandle != IntPtr.Zero)
            {
                ShowWindow(process.MainWindowHandle, (int)ShowWindowCommand.SW_HIDE);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex}");
        }
    }


    public static bool IsDoNotDisturbActiveRegistry()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings"
            );
            if (key?.GetValue("NOC_GLOBAL_SETTING_TOASTS_ENABLED") is object value)
            {
                return value.ToString() == "0"; // Toasts disabled = Do Not Disturb
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Utils] IsDoNotDisturbActiveRegistry failed: {ex.Message}");
        }
        return false;
    }

    public static bool IsDoNotDisturbActiveFocusAssist()
    {
        try
        {
            if (GetFocusAssistState(out int state))
            {
                return state == (int)FocusAssistState.PRIORITY_ONLY
                    || state == (int)FocusAssistState.ALARMS_ONLY;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Utils] IsDoNotDisturbActiveFocusAssist failed: {ex.Message}");
        }
        return false;
    }

    public static bool IsDoNotDisturbActiveFocusAssistCim()
    {
        try
        {
            var scope = new System.Management.ManagementScope(@"\\.\root\cimv2\mdm\dmmap");
            var query = new System.Management.ObjectQuery(
                "SELECT QuietHoursState FROM MDM_Policy_Config_QuietHours"
            );
            using (var searcher = new System.Management.ManagementObjectSearcher(scope, query))
            using (var results = searcher.Get())
            {
                foreach (System.Management.ManagementObject obj in results)
                {
                    var stateObj = obj["QuietHoursState"];
                    if (stateObj != null && int.TryParse(stateObj.ToString(), out int state))
                    {
                        return state == (int)FocusAssistState.PRIORITY_ONLY
                            || state == (int)FocusAssistState.ALARMS_ONLY;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Utils] IsDoNotDisturbActiveFocusAssistCim failed: {ex.Message}");
        }
        return false;
    }

    public static bool IsDoNotDisturbActive()
    {
        return IsDoNotDisturbActiveRegistry()
            || IsDoNotDisturbActiveFocusAssist()
            || IsDoNotDisturbActiveFocusAssistCim();
    }

    public static void TryExitApplication()
    {
        try
        {
            System.Windows.Forms.Application.Exit();
            System.Threading.Thread.Sleep(500);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Application.Exit() failed: {ex}");
        }
        try
        {
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Environment.Exit() failed: {ex}");
        }
        try
        {
            var process = Process.GetCurrentProcess();
            process.Kill();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Process.Kill() failed: {ex}");
        }
    }

    public static (Color, double) ParseColorAndOpacity(
        string? colorString,
        Color defaultColor,
        double defaultOpacity
    )
    {
        if (!string.IsNullOrWhiteSpace(colorString))
        {
            try
            {
                var colorStr = colorString.TrimStart('#');
                if (colorStr.Length == 8)
                {
                    var a = Convert.ToByte(colorStr.Substring(0, 2), 16);
                    var r = Convert.ToByte(colorStr.Substring(2, 2), 16);
                    var g = Convert.ToByte(colorStr.Substring(4, 2), 16);
                    var b = Convert.ToByte(colorStr.Substring(6, 2), 16);
                    return (Color.FromArgb(r, g, b), a / 255.0);
                }
                else if (colorStr.Length == 6)
                {
                    var r = Convert.ToByte(colorStr.Substring(0, 2), 16);
                    var g = Convert.ToByte(colorStr.Substring(2, 2), 16);
                    var b = Convert.ToByte(colorStr.Substring(4, 2), 16);
                    return (Color.FromArgb(r, g, b), defaultOpacity);
                }
            }
            catch { }
        }
        return (defaultColor, defaultOpacity);
    }

    public static Bitmap CreateDefaultIcon()
    {
        var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), 0, 0, 32, 32);
            using (var pen = new Pen(Color.Orange, 2))
            {
                var points = new[]
                {
                    new System.Drawing.Point(10, 8),
                    new System.Drawing.Point(22, 16),
                    new System.Drawing.Point(10, 24),
                };
                g.DrawLines(pen, points);
            }
        }
        return bitmap;
    }

    public static bool IsVirtualDesktopConnected() =>
        Process.GetProcessesByName("VirtualDesktop.Server").Any();

    public static bool IsSteamVRRunning() =>
        Process.GetProcessesByName("vrmonitor").Any()
        && Process.GetProcessesByName("vrcompositor").Any();

    public static bool IsVrchatRunning() => Process.GetProcessesByName("VRChat").Any();

    public static Uri BuildJoinLink(string worldId, string instanceId)
    {
        return Program.gameUri.AddQuery("id", $"{worldId}:{instanceId}", encode: false);
    }

    public static bool HandleJoin(
        string worldId,
        string instanceId,
        List<string> additionalArgs = null
    )
    {
        if (string.IsNullOrWhiteSpace(worldId) || string.IsNullOrWhiteSpace(instanceId))
        {
            Console.WriteLine("Invalid world or instance ID.");
            return false;
        }
        try
        {
            switch (Program.cfg.App.LaunchMode)
            {
                case Configuration.LaunchMode.Uri:
                    RunAdditionalApps(Program.cfg.App.RunAdditional);
                    var joinLink = BuildJoinLink(worldId, instanceId);
                    Console.WriteLine(
                        $"Joining world {worldId} instance {instanceId} with link: {joinLink}"
                    );
                    StartGame(joinLink, additionalArgs);
                    return true;
                case Configuration.LaunchMode.Launcher:
                    throw new NotImplementedException();
                case Configuration.LaunchMode.Steam:
                    throw new NotImplementedException("Steam launch mode is not implemented yet.");
                case Configuration.LaunchMode.SelfInvite:
                    if (!IsVrchatRunning())
                        Console.WriteLine(
                            $"Using self-invite launch mode but VRChat is not running"
                        );
                    Program.client.InviteSelf(worldId, instanceId).GetAwaiter().GetResult();
                    break;
                case Configuration.LaunchMode.Unknown:
                default:
                    Console.WriteLine(
                        $"Unknown launch mode ({Enum.GetName(typeof(Configuration.LaunchMode), Program.cfg.App.LaunchMode)}). Please check your configuration."
                    );
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting game: {ex.Message}");
        }
        return false;
    }

    public static Process StartGame(Uri joinLink, List<string> additionalArgs = null)
    {
        // $"{Program.cfg.App.GameArguments}{string.Join(" ", Program.args)}"
        var args = JoinArgs(Program.cfg.App.GameArguments.Split(' '), additionalArgs);
        if (Program.useVR)
        {
            args += "--vrmode OpenVR";
        }
        else
        {
            args += " --no-vr -vrmode None";
        }
        var p = Process.Start(
            new ProcessStartInfo(joinLink.ToString()) { UseShellExecute = true, Arguments = args }
        );
        //Console.WriteLine($"Started game as process #{p.Id} with args \"{p.StartInfo.Arguments}\"");
        var commandLine = p.GetCommandLine();
        Console.WriteLine($"{commandLine}");
        return p;
    }

    public static string JoinArgs(params IEnumerable<IEnumerable<string>> args) =>
        string.Join(" ", JoinListsUnique(args));

    public static IEnumerable<string> JoinListsUnique(
        params IEnumerable<IEnumerable<string>> arglists
    )
    {
        var unique = new HashSet<string>();
        foreach (var args in arglists)
        {
            if (args is null)
                continue;
            foreach (var a in args)
            {
                if (!string.IsNullOrWhiteSpace(a))
                {
                    unique.Add(a.Trim());
                }
            }
        }
        return unique;
    }

    public static void RunAdditionalApps(List<List<string>> apps)
    {
        if (apps == null || apps.Count == 0)
            return;
        foreach (var app in apps)
        {
            if (app == null || app.Count == 0 || string.IsNullOrWhiteSpace(app[0]))
                continue;
            var binary = app[0];
            var args =
                app.Count > 1
                    ? string.Join(" ", app.Skip(1).Select(a => a.Contains(' ') ? $"\"{a}\"" : a))
                    : string.Empty;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = binary,
                    Arguments = args,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                };
                Process.Start(psi);
                Console.WriteLine($"Launched additional app: {binary} {args}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Failed to launch additional app: {binary} {args}\n{ex.Message}"
                );
            }
        }
    }
}
