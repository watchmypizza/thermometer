using System.Text.Json.Nodes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Generic;
using thermometer.CommandLineArguments;
using System.Security.Cryptography.X509Certificates;

namespace thermometer.Program
{
    public class ThermometerApp
    {
        public static string packageManager { get; set; } = "unknown";
        public const string defaultConfigPath = "~/.config/thermometer/";

        public static void Main(string[] args)
        {
            var workingDirectory = defaultConfigPath.Replace("~", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile));
            if (!System.IO.Directory.Exists(workingDirectory))
            {
                System.IO.Directory.CreateDirectory(workingDirectory);
            }
            var paths = CommandLineArgs.GetCpuFreqPaths();
            if (paths.Count == 0)
            {
                Console.WriteLine("No CPU frequency directories found. Incompatible device?");
                Environment.Exit(1);
            }
            System.Console.WriteLine("Checking dependencies...");
            // Check dependencies and look for matching packagemanager or OS
            var operatingSystem = System.Environment.OSVersion.Platform;
            System.Console.WriteLine($"Operating System: {operatingSystem}");
            // Check distribution if Unix-based
            if (operatingSystem == System.PlatformID.Unix)
            {
                var distro = checkDistro();
                System.Console.WriteLine($"Distribution: {distro}");
            }
            System.Console.WriteLine("Thermometer CPU control started.");
            CommandLineArguments.CommandLineArgs.parseArgs(args);
        }

        public static bool getSudo()
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if(Environment.UserName != "root")
                {
                    System.Console.WriteLine("This action requires elevated privileges. Please run with sudo.");
                    Environment.Exit(1);
                    return false;
                }
                return true;
            }
            return false;
        }

        public static string checkDistro()
        {
            var distroFiles = new List<Tuple<string, string, string>>
            {
                new("/etc/redhat-release", "redhat-release", "yum"),
                new("/etc/arch-release", "arch-release", "pacman"),
                new("/etc/gentoo-release", "gentoo-release", "emerge"),
                new("/etc/SuSE-release", "SuSE-release", "zypp"),
                new("/etc/debian_version", "debian-release", "apt-get"),
                new("/etc/alpine-release", "alpine-release", "apk")
            };

            foreach (var (filePath, distroName, pkgM) in distroFiles)
            {
                if (System.IO.File.Exists(filePath))
                {
                    getSudo();
                    var workingDirectory = defaultConfigPath.Replace("~", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile));
                    var yamlFilePath = System.IO.Path.Combine(workingDirectory, "thermometer_config.yaml");
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();

                    Dictionary<string, string> existingConfig = new();

                    if (System.IO.File.Exists(yamlFilePath))
                    {
                        var existingContent = System.IO.File.ReadAllText(yamlFilePath);
                        existingConfig = deserializer.Deserialize<Dictionary<string, string>>(existingContent);
                    }

                    existingConfig["package_manager"] = pkgM;
                    existingConfig["distribution"] = distroName;
                    existingConfig["current_version"] = "1.4.4";
                    var serializer = new SerializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();

                    var yamlContent = serializer.Serialize(existingConfig);
                    System.IO.File.WriteAllText(yamlFilePath, yamlContent);

                    packageManager = pkgM;
                    return $"{distroName} (Package Manager: {pkgM})";
                }
            }

            return "Unknown";
        }
    }
}