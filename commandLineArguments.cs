using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using thermometer.Program;
using System.Security.AccessControl;
using System.ComponentModel;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Timers;
using System.Diagnostics;

namespace thermometer.CommandLineArguments
{
    public class CommandLineArgs
    {
        public static bool verbose { get; set; } = false;
        public static string GHz { get; set; } = "2.5GHz";
        public static int safeMinKhz { get; set; } = 0600000;
        public static int safeMaxKhz { get; set; } = 2500000;
        public static string cpuDirs { get; } = "/sys/devices/system/cpu/cpu";
        private static Dictionary<string, string> ReadConfig(string yamlFilePath)
        {
            if (!System.IO.File.Exists(yamlFilePath))
                return new Dictionary<string, string>();

            var yamlContent = System.IO.File.ReadAllText(yamlFilePath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<Dictionary<string, string>>(yamlContent) ?? new Dictionary<string, string>();
        }

        public static List<string> GetCpuFreqPaths()
        {
            var paths = new List<string>();
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                var path = System.IO.Path.Combine(cpuDirs + i.ToString(), "cpufreq");
                if (System.IO.Directory.Exists(path))
                {
                    paths.Add(path);
                }
            }
            return paths;
        }

        private static void WriteConfig(string yamlFilePath, Dictionary<string, string> config)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yamlContent = serializer.Serialize(config);
            System.IO.File.WriteAllText(yamlFilePath, yamlContent);
        }

        private static double getKhz(double GHzValue)
        {
            double KHzValue = GHzValue * 1000000;

            var paths = GetCpuFreqPaths();

            foreach (var path in paths) {

                var max_freq_info = System.IO.Path.Combine(path, "cpuinfo_max_freq");
                var min_freq_info = System.IO.Path.Combine(path, "cpuinfo_min_freq");

                var max_freq = double.Parse(System.IO.File.ReadAllText(max_freq_info));
                var min_freq = double.Parse(System.IO.File.ReadAllText(min_freq_info));

                if (KHzValue > max_freq || KHzValue < min_freq)
                {
                    Console.WriteLine("Invalid amount. QUITTING.");
                    Console.WriteLine($"Allowed range: {min_freq / 1000000} GHz - {max_freq / 1000000} GHz");
                    Environment.Exit(1);
                }

                return KHzValue;
            }
            return -1;
        }

        public static void parseArgs(string[] args)
        {
            var workingDirectory = ThermometerApp.defaultConfigPath.Replace("~", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile));
            var yamlFilePath = System.IO.Path.Combine(workingDirectory, "thermometer_config.yaml");

            var existingConfig = ReadConfig(yamlFilePath);

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--verbose":
                    case "-v":
                        verbose = true;
                        System.Console.WriteLine("Verbose mode enabled.");
                        break;

                    case "--set-max-freq":
                    case "-smf":
                        if (i + 1 < args.Length)
                        {
                            if (verbose) {
                                Console.WriteLine($"VERBOSE: {args[i + 1]}");
                            }
                            GHz = args[i + 1];
                            if(GHz.ToLower().Contains("ghz"))
                            {
                                GHz = GHz.ToLower().Replace("ghz", "");
                            }
                            if (double.TryParse(GHz, out double GHzValue))
                            {
                                double KHzValue = getKhz((double)GHzValue);
                                existingConfig["setMaxFreq"] = KHzValue.ToString();

                                var paths = GetCpuFreqPaths();

                                foreach(var path in paths) {
                                    if (verbose)
                                    {
                                        Regex ptrn = new Regex(@"\d+");
                                        var match = ptrn.Match(path.ToString());

                                        Console.WriteLine($"Writing core {match}...");
                                    }
                                    File.WriteAllText(System.IO.Path.Combine(path, "scaling_max_freq"), KHzValue.ToString());
                                    if (verbose)
                                    {
                                        Console.WriteLine("Done.");
                                    }
                                }

                                if (verbose)
                                {
                                    System.Console.WriteLine($"VERBOSE: Writing YAML config for max freq: {KHzValue}");
                                    WriteConfig(yamlFilePath, existingConfig);
                                    System.Console.WriteLine($"CPU frequency set to {GHzValue} GHz ({KHzValue} KHz).");
                                } else
                                {
                                    System.Console.WriteLine($"Setting CPU frequency to {KHzValue/1000000} GHz.");
                                }

                                i++;
                            }
                            else
                            {
                                System.Console.WriteLine("Invalid frequency value provided.");
                            }
                        }
                        else
                        {
                            System.Console.WriteLine("No frequency value provided.");
                        }
                        break;
                    case "--set-min-freq":
                    case "-smnf":
                        if (i + 1 < args.Length)
                        {
                            if (verbose) {
                                Console.WriteLine($"VERBOSE: {args[i + 1]}");
                            }
                            GHz = args[i + 1];
                            if(GHz.ToLower().Contains("ghz"))
                            {
                                GHz = GHz.ToLower().Replace("ghz", "");
                            }
                            if (double.TryParse(GHz, out double GHzValue))
                            {
                                double KHzValue = getKhz((double)GHzValue);
                                existingConfig["setMinFreq"] = KHzValue.ToString();

                                var paths = GetCpuFreqPaths();

                                foreach(var path in paths)
                                {    
                                    if (verbose)
                                    {
                                        Regex ptrn = new Regex(@"\d+");
                                        var match = ptrn.Match(path.ToString());

                                        Console.WriteLine($"Writing core {match}...");
                                    }
                                    File.WriteAllText(System.IO.Path.Combine(path, "scaling_max_freq"), KHzValue.ToString());
                                    if (verbose)
                                    {
                                        Console.WriteLine("Done.");
                                    }
                                }

                                if (verbose)
                                {
                                    System.Console.WriteLine($"VERBOSE: Writing YAML config for min freq: {KHzValue}");
                                    WriteConfig(yamlFilePath, existingConfig);
                                    System.Console.WriteLine($"CPU minimum frequency set to {GHzValue} GHz ({KHzValue} KHz).");
                                } else
                                {
                                    System.Console.WriteLine($"Setting CPU minimum frequency to {KHzValue/1000000} GHz.");
                                }
                                
                                i++;
                            }
                            else
                            {
                                System.Console.WriteLine("Invalid frequency value provided.");
                            }
                        }
                        else
                        {
                            System.Console.WriteLine("No frequency value provided.");
                        }
                        break;
                    case "--help":
                    case "-h":
                        System.Console.WriteLine("Thermometer CPU Control Help:");
                        System.Console.WriteLine("--verbose | -v : Enable verbose output.");
                        System.Console.WriteLine("--set-max-freq | -smf [value in GHz] : Set the maximum CPU frequency.");
                        System.Console.WriteLine("--set-min-freq | -smnf [value in GHz] : Set the minimum CPU frequency.");
                        System.Console.WriteLine("--help | -h : Display this help message.");
                        System.Console.WriteLine("--version : Display the current version of Thermometer.");
                        System.Console.WriteLine("--install-daemon | -id : Install and optionally enable the Thermometer daemon.");
                        break;
                    case "--daemon":
                    case "-d":
                        thermometer.Daemon.run();
                        break;
                    case "--version":
                        var version = existingConfig.ContainsKey("current_version") ? existingConfig["current_version"] : "unknown";
                        System.Console.WriteLine($"Thermometer Version: {version}");
                        break;
                    case "--install-daemon":
                    case "-id":
                        bool install = thermometer.Daemon.installDaemon();
                        if(!install)
                        {
                            System.Console.WriteLine("Daemon installation failed.");
                        } else
                        {
                            System.Console.WriteLine("Daemon installation succeeded.");
                        }
                        break;
                    case "--mode":
                    case "-m":
                        if (i + 1 < args.Length)
                        {
                            if (verbose)
                            {
                                Console.WriteLine($"VERBOSE: {args[i + 1]}");
                            }

                            var mode = args[i + 1];
                            var modeDir = "/sys/devices/system/cpu/cpu0/cpufreq/scaling_available_governors";
                            var paths = GetCpuFreqPaths();
                            if (!File.Exists(modeDir))
                            {
                                Console.WriteLine("The available_governors file does either not exist, or your CPU doesn't support it.");
                            }
                            var fileText = File.ReadAllText(modeDir).Trim().Split(" ");
                            List<string> modes = [.. fileText];
                            if(!modes.Contains(mode))
                            {
                                Console.WriteLine($"Mode doesn't exist: {mode}");
                                Console.WriteLine($"Available Modes: {string.Join(", ", modes)}");
                                Environment.Exit(1);
                            }
                            foreach(var path in paths)
                            {
                                if (verbose)
                                {
                                    Regex ptrn = new Regex(@"\d+");
                                    var core = ptrn.Match(path.ToString());
                                    Console.WriteLine($"Applying governor for cpu core {core}...");
                                }
                                var finalPath = Path.Combine(path, "scaling_governor");
                                File.WriteAllText(finalPath, mode);
                                if (verbose)
                                {
                                    Console.WriteLine("Done.");
                                }
                            }

                            Console.WriteLine($"Applied mode {mode} to the CPU.");
                        }
                        i++;
                        break;

                    default:
                        System.Console.WriteLine($"Unknown argument: {args[i]}");
                        break;
                }
            }

            WriteConfig(yamlFilePath, existingConfig);
        }
    }
}