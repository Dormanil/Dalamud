using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Dalamud.Interface;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Dalamud.Injector
{
    /// <summary>
    /// Entrypoint to the program.
    /// </summary>
    public sealed class EntryPoint
    {
        /// <summary>
        /// Start the Dalamud injector.
        /// </summary>
        /// <param name="args">String arguments.</param>
        public static void Main(string[] args)
        {
            InitUnhandledException();
            InitLogging();

            var process = GetProcess(args.ElementAtOrDefault(1));
            var startInfo = GetStartInfo(args.ElementAtOrDefault(2), process);

            startInfo.WorkingDirectory = Directory.GetCurrentDirectory();

            // This seems to help with the STATUS_INTERNAL_ERROR condition
            Thread.Sleep(1000);

            Inject(process, startInfo);

            Thread.Sleep(1000);
        }

        private static void InitUnhandledException()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                if (Log.Logger == null)
                {
                    Console.WriteLine($"A fatal error has occurred: {eventArgs.ExceptionObject}");
                }
                else
                {
                    var exObj = eventArgs.ExceptionObject;
                    if (exObj is Exception ex)
                    {
                        Log.Error(ex, "A fatal error has occurred.");
                    }
                    else
                    {
                        Log.Error($"A fatal error has occurred: {eventArgs.ExceptionObject}");
                    }
                }

#if DEBUG
                MessageBox.Show(
                    $"Couldn't inject.\nMake sure that Dalamud was not injected into your target process " +
                    $"as a release build before and that the target process can be accessed with VM_WRITE permissions.\n\n" +
                    $"{eventArgs.ExceptionObject}",
                    "Debug Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
#else
                MessageBox.Show(
                    "Failed to inject the XIVLauncher in-game addon.\nPlease try restarting your game and your PC.\n" +
                    "If this keeps happening, please report this error.",
                    "XIVLauncher Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
#endif
                Environment.Exit(0);
            };
        }

        private static void InitLogging()
        {
            var baseDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);
#if DEBUG
            var logPath = Path.Combine(baseDirectory, "injector.log");
#else
            var logPath = Path.Combine(baseDirectory, "..", "..", "..", "dalamud.injector.log");
#endif

            var levelSwitch = new LoggingLevelSwitch();

#if DEBUG
            levelSwitch.MinimumLevel = LogEventLevel.Verbose;
#else
            levelSwitch.MinimumLevel = LogEventLevel.Information;
#endif

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Async(a => a.File(logPath))
                .WriteTo.Sink(SerilogEventSink.Instance)
                .MinimumLevel.ControlledBy(levelSwitch)
                .CreateLogger();
        }

        private static Process GetProcess(string arg)
        {
            Process process;

            var pid = -1;
            if (arg != default)
            {
                pid = int.Parse(arg);
            }

            switch (pid)
            {
                case -1:
                    process = Process.GetProcessesByName("ffxiv_dx11").FirstOrDefault();

                    if (process == default)
                    {
                        throw new Exception("Could not find process");
                    }

                    break;
                case -2:
                    var exePath = "C:\\Program Files (x86)\\SquareEnix\\FINAL FANTASY XIV - A Realm Reborn\\game\\ffxiv_dx11.exe";
                    var exeArgs = new StringBuilder()
                        .Append("DEV.TestSID=0 DEV.UseSqPack=1 DEV.DataPathType=1 ")
                        .Append("DEV.LobbyHost01=127.0.0.1 DEV.LobbyPort01=54994 ")
                        .Append("DEV.LobbyHost02=127.0.0.1 DEV.LobbyPort02=54994 ")
                        .Append("DEV.LobbyHost03=127.0.0.1 DEV.LobbyPort03=54994 ")
                        .Append("DEV.LobbyHost04=127.0.0.1 DEV.LobbyPort04=54994 ")
                        .Append("DEV.LobbyHost05=127.0.0.1 DEV.LobbyPort05=54994 ")
                        .Append("DEV.LobbyHost06=127.0.0.1 DEV.LobbyPort06=54994 ")
                        .Append("DEV.LobbyHost07=127.0.0.1 DEV.LobbyPort07=54994 ")
                        .Append("DEV.LobbyHost08=127.0.0.1 DEV.LobbyPort08=54994 ")
                        .Append("SYS.Region=0 language=1 version=1.0.0.0 ")
                        .Append("DEV.MaxEntitledExpansionID=2 DEV.GMServerHost=127.0.0.1 DEV.GameQuitMessageBox=0").ToString();
                    process = Process.Start(exePath, exeArgs);
                    Thread.Sleep(1000);
                    break;
                default:
                    process = Process.GetProcessById(pid);
                    break;
            }

            return process;
        }

        private static DalamudStartInfo GetStartInfo(string arg, Process process)
        {
            DalamudStartInfo startInfo;

            if (arg != default)
            {
                startInfo = JsonConvert.DeserializeObject<DalamudStartInfo>(Encoding.UTF8.GetString(Convert.FromBase64String(arg)));
            }
            else
            {
                var ffxivDir = Path.GetDirectoryName(process.MainModule.FileName);
                var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                startInfo = new DalamudStartInfo
                {
                    WorkingDirectory = null,
                    ConfigurationPath = Path.Combine(appDataDir, "XIVLauncher-net5", "dalamudConfig.json"),
                    PluginDirectory = Path.Combine(appDataDir, "XIVLauncher-net5", "installedPlugins"),
                    DefaultPluginDirectory = Path.Combine(appDataDir, "XIVLauncher-net5", "devPlugins"),
                    AssetDirectory = Path.Combine(appDataDir, "XIVLauncher-net5", "dalamudAssets"),
                    GameVersion = File.ReadAllText(Path.Combine(ffxivDir, "ffxivgame.ver")),
                    Language = ClientLanguage.English,
                    OptOutMbCollection = false,
                };

                Log.Debug(
                    "Creating a new StartInfo with:\n" +
                    $"    WorkingDirectory: {startInfo.WorkingDirectory}\n" +
                    $"    ConfigurationPath: {startInfo.ConfigurationPath}\n" +
                    $"    PluginDirectory: {startInfo.PluginDirectory}\n" +
                    $"    DefaultPluginDirectory: {startInfo.DefaultPluginDirectory}\n" +
                    $"    AssetDirectory: {startInfo.AssetDirectory}\n" +
                    $"    GameVersion: {startInfo.GameVersion}\n" +
                    $"    Language: {startInfo.Language}\n" +
                    $"    OptOutMbCollection: {startInfo.OptOutMbCollection}");

                Log.Information("A Dalamud start info was not found in the program arguments. One has been generated for you.");
                Log.Information("Copy the following contents into the program arguments:");

                var startInfoJson = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(startInfo)));
                Log.Information(startInfoJson);
            }

            return startInfo;
        }

        private static void Inject(Process process, DalamudStartInfo startInfo)
        {
            Log.Information($"Injecting into {process.Id}");

            var nethostName = "nethost.dll";
            var bootName = "Dalamud.Boot.dll";

            var nethostPath = Path.GetFullPath(nethostName);
            var bootPath = Path.GetFullPath(bootName);

            using var injector = new Reloaded.Injector.Injector(process);

            Inject(injector, nethostPath);
            Inject(injector, bootPath);

            var infoStr = JsonConvert.SerializeObject(startInfo);
            using var initParam = new SafeString(process, infoStr);
            var initResult = injector.CallFunction(bootPath, "Initialize", initParam.Address.ToInt64());
            if (initResult != 0)
            {
                Log.Error($"Dalamud.Boot::Initialize returned {initResult:X}");
                return;
            }

            Log.Information("Done");
        }

        private static long Inject(Reloaded.Injector.Injector injector, string path)
        {
            var moduleAddr = injector.Inject(path);
            if (moduleAddr == 0)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                throw new Exception($"Injection failed: {name}");
            }

            return moduleAddr;
        }
    }
}
