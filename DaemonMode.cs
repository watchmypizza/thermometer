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
            var workingDirectory = Program.ThermometerApp.defaultConfigPath.Replace("~", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile));
            var yamlFilePath = System.IO.Path.Combine(workingDirectory, "thermometer_config.yaml");
            var config = ReadConfig(yamlFilePath);
            var maxGhz = config.ContainsKey("setMaxFreq") ? config["setMaxFreq"] : "2.5GHz";
            var minGhz = config.ContainsKey("setMinFreq") ? config["setMinFreq"] : "1.0GHz";
            var cpuMaxFreq = CommandLineArguments.CommandLineArgs.cpuFreqDirectory + "scaling_max_freq";
            var cpuMinFreq = CommandLineArguments.CommandLineArgs.cpuFreqDirectory + "scaling_min_freq";

            Console.Write("Applying settings... ");

            File.WriteAllText(cpuMaxFreq, maxGhz);
            File.WriteAllText(cpuMinFreq, minGhz);
        }
    }
}