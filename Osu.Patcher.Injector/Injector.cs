using System;
using System.IO;
using System.Management;
using System.Runtime.Versioning;
using HoLLy.ManagedInjector;

namespace Osu.Patcher.Injector;

[SupportedOSPlatform("windows")]
internal static class Injector
{
    public static void Main()
    {
        try
        {
            using var proc = new InjectableProcess(GetOsuPid());
            var dllPath = Path.GetFullPath(typeof(Injector).Assembly.Location + @"\..\osu!.hook.dll");

            proc.Inject(dllPath, "Osu.Patcher.Hook.Hook", "Initialize");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);

            Console.WriteLine("\nPress any key to continue...");
            Console.Write("\a"); // Bell sound
            Console.ReadKey();
        }
    }

    /// <summary>
    ///     Find a <c>osu!.exe</c> process that has a <c>devserver</c> in the cli arguments. (Not connected to Bancho)
    /// </summary>
    /// <returns>The process id of the first matching process.</returns>
    /// <exception cref="Exception">If found invalid osu! process or no process at all.</exception>
    private static uint GetOsuPid()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ps",
            Arguments = "-e -o pid,command",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(startInfo))
        {
            using (var reader = process.StandardOutput)
            {
                string output = reader.ReadToEnd();
                foreach (var line in output.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;
                    
                    var pid = uint.Parse(parts[0]);
                    var command = parts[1];

                    if (command == "osu!" || command.Contains("osu!.exe"))
                    {
                        if (line.Contains("-devserver") && !line.contains("ppy.sh"))
                        {
                            return pid;
                        }
                        else
                        {
                            throw new Exception("Will not inject into osu! connected to Bancho!");
                        }
                    }
                }
            }
        }

        throw new Exception("Cannot find a running osu! process!");
    }
}
