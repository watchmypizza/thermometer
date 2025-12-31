namespace thermometer.CommandLineArguments
{
    public class CommandLineArgs
    {
        public static bool verbose { get; set; } = false;
        public static string GHz { get; set; } = "2.5GHz";

        public static void ParseArgs(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg == "--verbose")
                {
                    verbose = true;
                }

                if (arg.StartsWith("--ghz="))
                {
                    GHz = arg.Split('=')[1];
                    System.Console.WriteLine($"Setting CPU max frequency to: {GHz}");
                    bool validation = validateGHz(GHz);

                    if(!validation)
                    {
                        System.Console.WriteLine("Invalid GHz format. Please use the format like '2.5GHz'.");
                        continue;
                    }

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
                    System.Console.WriteLine("CPU max frequency set successfully.");
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