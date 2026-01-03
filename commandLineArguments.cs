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
using System.Xml.Serialization;
using System.Security;
using System.Data.SqlTypes;
using System.Reflection;
using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Collections.Specialized;
using System.Globalization;

namespace thermometer.CommandLineArguments
{
    public class CommandLineArgs
    {
        public static bool verbose { get; set; } = false;
        public static string GHz { get; set; } = "2.5GHz";
        public static int safeMinKhz { get; set; } = 0600000;
        public static int safeMaxKhz { get; set; } = 2500000;
        public static string cpuDirs { get; } = "/sys/devices/system/cpu/cpu";
        public static string tempDirs { get; } = "/sys/class/hwmon/";
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

        public static List<string> GetTemperaturePaths()
        {
            var paths = new List<string>();

            for(int i = 0; i < 256; i++)
            {
                var path = Path.Combine(tempDirs, "hwmon" + i.ToString());
                if (System.IO.Directory.Exists(path))
                {
                    paths.Add(path);
                }
            }
            return paths;
        }

        public static List<double> getTemperatures(string path)
        {
            List<double> temperatureResult = [];
            for(int i = 1; i < 256; i++)
            {
                if(verbose)
                {
                    Console.WriteLine($"VERBOSE: Attempting to read temp{i}_input");
                }
                var tempPath = Path.Combine(path, $"temp{i}_input");
                if(!File.Exists(tempPath))
                {
                    if (verbose) {
                        Console.WriteLine($"VERBOSE: Stopping and returning, temp{i}_input does not exist");
                    }
                    break;
                }

                if(verbose)
                {
                    Console.WriteLine($"VERBOSE: Reading temp{i}_input");
                }
                var tempInput = File.ReadAllText(tempPath) ?? "UNKNOWN TEMPERATURE";
                double.TryParse(tempInput.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double result);
                temperatureResult.Add(result);
            }
            return temperatureResult;
        }

        public static bool? checkDirectory(string directory)
        {
            try { return Directory.Exists(directory); }
            catch (Exception) { return null; }
        }

        private static void WriteConfig(string yamlFilePath, Dictionary<string, string> config)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yamlContent = serializer.Serialize(config);
            System.IO.File.WriteAllText(yamlFilePath, yamlContent);
        }

        private static bool verifyValue(double value)
        {
            List<string> paths = GetCpuFreqPaths();
            var cpuMaxFreq = Path.Combine(paths[0], "cpuinfo_max_freq");
            var cpuMinFreq = Path.Combine(paths[0], "cpuinfo_min_freq");

            if (verbose)
            {
                Console.WriteLine($"VERBOSE: Reading {cpuMaxFreq}");
            }
            var cpuMaxFreqContent = File.ReadAllText(cpuMaxFreq).Trim();
            if(verbose)
            {
                Console.WriteLine($"VERBOSE: Reading {cpuMinFreq}");
            }
            var cpuMinFreqContent = File.ReadAllText(cpuMinFreq).Trim();
            double.TryParse(cpuMaxFreqContent, out double cpuMax);
            double.TryParse(cpuMinFreqContent, out double cpuMin);

            if(value <= 0)
            {
                return true;
            }

            if(cpuMax < value || value < cpuMin)
            {
                if(verbose)
                {
                    Console.WriteLine($"VERBOSE: cpuMin: {cpuMin}, cpuMax: {cpuMax}, value (kHz): {value}");
                }
                Console.WriteLine($"Invalid Selection!\nValid range is from {Math.Round(cpuMin / 1000000, 2)}GHz - {Math.Round(cpuMax / 1000000, 2)}GHz");
                Environment.Exit(1);
            }
            
            return true;
        }

        private static void SetCpu(string targetFile, (string mode, double freq) data, Dictionary<string, string> config, string configKey, string yamlPath)
        {
            var (mode, freq) = data;

            string valueToWrite = freq> 0 ? ((long)freq).ToString() : mode;

            double.TryParse(valueToWrite, out double parsedVal);
            verifyValue(parsedVal);

            var paths = GetCpuFreqPaths();

            foreach (var path in paths)
            {
                if (!(checkDirectory(path) ?? false))
                {
                    Console.WriteLine($"CPU Core at {path} is offline, skipping.");
                    continue;
                }

                if (verbose)
                {
                    var match = Regex.Match(path, @"\d+");
                    Console.WriteLine($"Writing {valueToWrite} to {targetFile} on core {match}...");
                }

                File.WriteAllText(Path.Combine(path, targetFile), valueToWrite);
                
                if (verbose) Console.WriteLine("Done.");
            }

            if (!string.IsNullOrEmpty(configKey))
            {
                config[configKey] = valueToWrite;
                WriteConfig(yamlPath, config);
            }
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
                            string raw = args[++i].ToLower().Replace("ghz", "");
                            if (double.TryParse(raw, out double val))
                            {
                                double khz = val * 1000000;

                                SetCpu("scaling_max_freq", ("", khz), existingConfig, "setMaxFreq", yamlFilePath);
                                Console.WriteLine($"Max frequency set to {val} GHz.");
                            }
                            else { Console.WriteLine("Invalid frequency."); }
                        }
                        break;
                    case "--set-min-freq":
                    case "-smnf":
                        if (i + 1 < args.Length)
                        {
                            string raw = args[++i].ToLower().Replace("ghz", "");
                            if (double.TryParse(raw, out double val))
                            {
                                double khz = val * 1000000;

                                SetCpu("scaling_min_freq", ("", khz), existingConfig, "setMinFreq", yamlFilePath);
                                Console.WriteLine($"Min frequency set to {val} GHz.");
                            }
                            else { Console.WriteLine("Invalid frequency."); }
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
                        System.Console.WriteLine("--mode | -m : Set a CPU mode like performance.");
                        System.Console.WriteLine("--list-modes | -lm : See all supported CPU modes");
                        System.Console.WriteLine("--temperature | -t : View temperatures of different devices.");
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
                            string selectedMode = args[++i];
                            string availableGovPath = "/sys/devices/system/cpu/cpu0/cpufreq/scaling_available_governors";

                            if (!File.Exists(availableGovPath))
                            {
                                Console.WriteLine("Error: CPU frequency scaling is not supported on this system.");
                                break; 
                            }

                            var modes = File.ReadAllText(availableGovPath).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                            if (!modes.Contains(selectedMode))
                            {
                                Console.WriteLine($"Mode '{selectedMode}' doesn't exist.");
                                Console.WriteLine($"Available Modes: {string.Join(", ", modes)}");
                                Environment.Exit(1);
                            }

                            var paths = GetCpuFreqPaths();
                            SetCpu("scaling_governor", (selectedMode, 0), existingConfig, "selectedMode", yamlFilePath);

                            Console.WriteLine($"Applied mode {selectedMode} to the CPU.");
                        }
                        else
                        {
                            Console.WriteLine("Error: No mode specified.");
                        }
                        break;
                    case "--list-modes":
                    case "-lm":
                        string availableGovernors = "/sys/devices/system/cpu/cpu0/cpufreq/scaling_available_governors";

                        if(!File.Exists(availableGovernors))
                        {
                            Console.WriteLine("Error: CPU frequency scaling is not supported on this system.");
                            Environment.Exit(1);
                        }
                        if(verbose)
                        {
                            Console.WriteLine("Reading /sys/devices/system/cpu/cpu0/cpufreq/scaling_available_governors");
                        }
                        var availModes = File.ReadAllText(availableGovernors).Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);

                        Console.WriteLine($"Available Modes: {string.Join(", ", availModes)}");
                        break;
                    case "--temperature":
                    case "-t":
                        List<string> temperatures = GetTemperaturePaths();
                        if(temperatures.Count == 0)
                        {
                            Console.WriteLine("ERROR: No HWMON device found.");
                            Environment.Exit(1);
                        }
                        foreach(var path in temperatures)
                        {
                            var filePath = Path.Combine(path, "temp1_input");
                            if (!File.Exists(filePath))
                            {
                                continue;
                            }
                            if (verbose)
                            {
                                Console.WriteLine($"VERBOSE: Reading {filePath}");
                            }
                            var namePath = Path.Combine(path, "name");
                            if (verbose)
                            {
                                Console.WriteLine($"VERBOSE: Reading {namePath}");
                            }
                            var deviceName = File.ReadAllText(namePath).Trim() ?? "UNKNOWN DEVICE";

                            List<double> temps = getTemperatures(path);

                            Console.WriteLine("------------------------------");
                            Console.WriteLine($"Device Name: {deviceName}");
                            foreach(var temp in temps)
                            {
                                Console.WriteLine($"Temperature: {temp / 1000}°C");
                            }
                        }
                        break;
                    case "--status":
                    case "-s":
                        string cpu0 = "/sys/devices/system/cpu/cpu0/cpufreq/";
                        List<string> AvailPaths = ["scaling_max_freq", "scaling_min_freq", "cpuinfo_max_freq", "cpuinfo_min_freq", "scaling_available_governors", "scaling_governor", "cpuinfo_transition_latency", "cpuinfo_avg_freq"];
                        List<string> infoToPrint = ["Current max frequency: ", "Current min frequency: ", "Max supported frequency: ", "Min supported frequency: ", "Available modes: ", "Current mode: ", "Transition latency: ", "Average frequency: "];
                        int curIdx = 0;
                        foreach(string path in AvailPaths)
                        {
                            string newP = cpu0 + path;

                            if(!File.Exists(newP))
                            {
                                if(verbose)
                                {
                                    Console.WriteLine($"VERBOSE: File {newP} does not exist, moving on.");
                                }
                                continue;
                            }
                            string entry = infoToPrint[curIdx];
                            curIdx++;
                            string text = File.ReadAllText(newP).Trim().Replace(" ", ", ");
                            double.TryParse(text, out var result);

                            if(result != 0)
                            {
                                Console.WriteLine(entry + Math.Round(result / 1000000, 2) + "GHz");
                                continue;
                            }

                            Console.WriteLine(entry + text);
                        }
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