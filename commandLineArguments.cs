using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Generic;

namespace thermometer.CommandLineArguments
{
    public class CommandLineArgs
    {
        public static bool verbose { get; set; } = false;
        public static string GHz { get; set; } = "2.5GHz";

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

        public static void ParseArgs(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg == "--verbose")
                {
                    verbose = true;
                    System.Console.WriteLine("Verbose mode enabled.");
                }

                if (arg.StartsWith("--max-ghz="))
                {
                    GHz = arg.Split('=')[1];
                    System.Console.WriteLine($"Setting CPU max frequency to: {GHz}");
                    bool validation = validateGHz(GHz);

                    if(!validation)
                    {
                        System.Console.WriteLine("Invalid GHz format. Please use the format like '2.5GHz'.");
                        continue;
                    }

                    if (verbose)
                    {
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "sudo",
                                Arguments = $"cpupower frequency-set -u {GHz}",
                                RedirectStandardOutput = false,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                    } else
                    {
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "sudo",
                                Arguments = $"cpupower frequency-set -u {GHz}",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                    }

                    var workingDirectory = System.IO.Directory.GetCurrentDirectory();
                    var yamlFilePath = System.IO.Path.Combine(workingDirectory, "thermometer_config.yaml");

                    var yamlObject = ReadConfig(yamlFilePath);
                    yamlObject["cpu_max_frequency"] = GHz;
                    WriteConfig(yamlFilePath, yamlObject);

                    System.Console.WriteLine("CPU max frequency set successfully.");
                }

                if (arg.StartsWith("--min-ghz="))
                {
                    GHz = arg.Split('=')[1];
                    System.Console.WriteLine($"Setting CPU min frequency to: {GHz}");
                    bool validation = validateGHz(GHz);

                    if(!validation)
                    {
                        System.Console.WriteLine("Invalid GHz format. Please use the format like '1GHz'.");
                        continue;
                    }

                    if (verbose)
                    {
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "sudo",
                                Arguments = $"cpupower frequency-set -d {GHz}",
                                RedirectStandardOutput = false,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                    } else
                    {
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "sudo",
                                Arguments = $"cpupower frequency-set -d {GHz}",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                    }

                    var workingDirectory = System.IO.Directory.GetCurrentDirectory();
                    var yamlFilePath = System.IO.Path.Combine(workingDirectory, "thermometer_config.yaml");
                    var yamlObject = ReadConfig(yamlFilePath);
                    yamlObject["cpu_min_frequency"] = GHz;
                    WriteConfig(yamlFilePath, yamlObject);

                    System.Console.WriteLine("CPU min frequency set successfully.");
                }

                if (arg.StartsWith("--version"))
                {
                    System.Console.WriteLine("Thermometer version 1.1");
                }

                if (arg.StartsWith("--status"))
                {
                    var workingDirectory = System.IO.Directory.GetCurrentDirectory();
                    var yamlFilePath = System.IO.Path.Combine(workingDirectory, "thermometer_config.yaml");
                    if (System.IO.File.Exists(yamlFilePath))
                    {
                        var yamlContent = System.IO.File.ReadAllText(yamlFilePath);
                        System.Console.WriteLine("Current Configuration:");
                        System.Console.WriteLine(yamlContent);
                    }
                    else
                    {
                        System.Console.WriteLine("Configuration file not found.");
                    }
                }

                if (arg.StartsWith("--help"))
                {
                    System.Console.WriteLine("Available command line arguments:");
                    System.Console.WriteLine("--verbose : Enable verbose output.");
                    System.Console.WriteLine("--max-ghz=<value> : Set the CPU max frequency (e.g., --max-ghz=2.5GHz).");
                    System.Console.WriteLine("--min-ghz=<value> : Set the CPU min frequency (e.g., --min-ghz=1.0GHz).");
                    System.Console.WriteLine("--help : Display this help message.");
                    System.Console.WriteLine("--version : Display the current version of the application.");
                    System.Console.WriteLine("--status : Display the current configuration.");
                }
            }
        }

        private static bool validateGHz(string ghz)
        {
            // Simple validation to check if the format is correct (e.g., "2.5GHz")
            return System.Text.RegularExpressions.Regex.IsMatch(ghz, @"^\d+(\.\d+)?GHz$");
        }
    }
}