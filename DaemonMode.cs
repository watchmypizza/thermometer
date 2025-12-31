using System.Drawing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace thermometer.DaemonMode
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
            var workingDirectory = System.IO.Directory.GetCurrentDirectory();
            var yamlFilePath = System.IO.Path.Combine(workingDirectory, "thermometer_config.yaml");
            var config = ReadConfig(yamlFilePath);
            var maxGhz = config.ContainsKey("cpu_max_frequency") ? config["cpu_max_frequency"] : "2.5GHz";
            var minGhz = config.ContainsKey("cpu_min_frequency") ? config["cpu_min_frequency"] : "1.0GHz";

            Console.Write("Applying settings... ");

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "cpupower frequency-set -u " + maxGhz + " -d " + minGhz,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.WaitForExit();
        }
    }
}