using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using thermometer.Program;

namespace thermometer.CommandLineArguments
{
    public class CommandLineArgs
    {
        public static bool verbose { get; set; } = false;
        public static string GHz { get; set; } = "2.5GHz";
        public static int safeMinKhz { get; set; } = 0600000;
        public static int safeMaxKhz { get; set; } = 2500000;
        public static string cpuFreqDirectory { get; } = "/sys/devices/system/cpu/cpu0/cpufreq/";

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

            var max_freq_info = System.IO.Path.Combine(cpuFreqDirectory, "cpuinfo_max_freq");
            var min_freq_info = System.IO.Path.Combine(cpuFreqDirectory, "cpuinfo_min_freq");

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
                                if (verbose)
                                {
                                    System.Console.WriteLine($"VERBOSE: Writing YAML config for max freq: {KHzValue}");
                                    WriteConfig(yamlFilePath, existingConfig);
                                    System.Console.WriteLine($"Setting CPU frequency to {KHzValue/1000000} GHz.");
                                } else
                                {
                                    System.Console.WriteLine($"CPU frequency set to {GHzValue} GHz ({KHzValue} KHz).");
                                }

                                File.WriteAllText(System.IO.Path.Combine(cpuFreqDirectory, "scaling_max_freq"), KHzValue.ToString());
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
                                if (verbose)
                                {
                                    System.Console.WriteLine($"VERBOSE: Writing YAML config for min freq: {KHzValue}");
                                    WriteConfig(yamlFilePath, existingConfig);
                                    System.Console.WriteLine($"Setting CPU minimum frequency to {KHzValue/1000000} GHz.");
                                } else
                                {
                                    System.Console.WriteLine($"CPU minimum frequency set to {GHzValue} GHz ({KHzValue} KHz).");
                                }


                                File.WriteAllText(System.IO.Path.Combine(cpuFreqDirectory, "scaling_min_freq"), KHzValue.ToString());
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
                        break;
                    case "--daemon":
                    case "-d":
                        thermometer.DaemonMode.Daemon.run();
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