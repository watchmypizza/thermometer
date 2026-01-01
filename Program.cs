using System.Text.Json.Nodes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Generic;
using thermometer.CommandLineArguments;

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
            System.Console.WriteLine("Checking dependencies...");
            // Check dependencies and look for matching packagemanager or OS
            var operatingSystem = System.Environment.OSVersion.Platform;
            System.Console.WriteLine($"Operating System: {operatingSystem}");
            // Check distribution if Unix-based
            if (operatingSystem == System.PlatformID.Unix)
            {
                var distro = checkDistro();
                System.Console.WriteLine($"Distribution: {distro}");
                bool successInstall = checkDependencies("cpupower, lm_sensors");
            }
            System.Console.WriteLine("Thermometer CPU control started.");
            CommandLineArguments.CommandLineArgs.ParseArgs(args);
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
                    existingConfig["current_version"] = "1.2";

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

        public static bool checkDependencies(string dependencies)
        {
            if (packageManager == "unknown")
            {
                return false;
            }

            var depsList = dependencies.Split(',');
            foreach (var dep in depsList)
            {
                var trimmedDep = dep.Trim();
                var command = packageManager switch
                {
                    "apt-get" => "dpkg -s ",
                    "yum" => "rpm -q ",
                    "pacman" => "pacman -Qi ",
                    "emerge" => "equery list ",
                    "zypp" => "zypper se -i ",
                    "apk" => "apk info | grep ",
                    _ => ""
                };
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sudo",
                        Arguments = command + trimmedDep,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (string.IsNullOrWhiteSpace(result))
                {
                    System.Console.WriteLine($"Dependency missing: {trimmedDep}");
                    System.Console.WriteLine($"Do you want to install it now? (y/n): ");
                    var input = System.Console.ReadLine();

                    if (input != null && input.ToLower() == "y")
                    {
                        var installCommand = packageManager switch
                        {
                            "apt-get" => $"sudo apt-get install -y {trimmedDep}",
                            "yum" => $"sudo yum install -y {trimmedDep}",
                            "pacman" => $"sudo pacman -S --noconfirm {trimmedDep}",
                            "emerge" => $"sudo emerge {trimmedDep}",
                            "zypp" => $"sudo zypper install -y {trimmedDep}",
                            "apk" => $"sudo apk add {trimmedDep}",
                            _ => ""
                        };
                        var installProcess = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "/bin/bash",
                                Arguments = $"-c \"{installCommand}\"",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        installProcess.Start();
                        string installResult = installProcess.StandardOutput.ReadToEnd();
                        installProcess.WaitForExit();
                        System.Console.WriteLine(installResult);
                    }
                    else
                    {
                        System.Console.WriteLine("Installation skipped. Exiting.");
                        return false;
                    }
                }
            }
            return true;
        }
    }
}