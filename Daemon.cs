using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using thermometer.CommandLineArguments;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace thermometer
{
    public class Daemon
    {
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

        public static void run()
        {
            var workingDirectory = Program.ThermometerApp.defaultConfigPath.Replace("~", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile));
            var yamlFilePath = System.IO.Path.Combine(workingDirectory, "thermometer_config.yaml");
            var config = ReadConfig(yamlFilePath);
            var maxGhz = config.ContainsKey("setMaxFreq") ? config["setMaxFreq"] : "2500000";
            var minGhz = config.ContainsKey("setMinFreq") ? config["setMinFreq"] : "1000000";

            var paths = CommandLineArgs.GetCpuFreqPaths();

            foreach (var path in paths) {
                var cpuMaxFreq = Path.Combine(path, "scaling_max_freq");
                var cpuMinFreq = Path.Combine(path, "scaling_min_freq");

                Regex ptrn = new Regex(@"\d+");
                var match = ptrn.Match(path.ToString());

                Console.Write($"Applying settings for core {match}...");

                File.WriteAllText(cpuMaxFreq, maxGhz);
                File.WriteAllText(cpuMinFreq, minGhz);

                Console.WriteLine("Done.");
            }
        }

        public static bool installDaemon()
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"curl -fsSL https://raw.githubusercontent.com/watchmypizza/thermometer/refs/heads/main/thermometer.service | sudo tee /etc/systemd/system/thermometer.service\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.WaitForExit();

            Console.WriteLine("Daemon Installed. Do you want to enable it so it starts on boot? (y/N)");
            string input = Console.ReadLine() ?? "n";

            if (input.ToLower() == "y")
            {
                var enableProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = "-c \"sudo systemctl enable thermometer.service\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                enableProcess.Start();
                enableProcess.WaitForExit();

                Console.WriteLine("Daemon enabled to start on boot.");
                return true;
            }

            return false;
        }
    }
}