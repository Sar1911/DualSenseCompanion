using System.Diagnostics;
using System.Net;
using System.ServiceProcess;
using System.Windows.Forms;
using Nefarius.Drivers.HidHide;

public static class DependencyManager
{
    public static void CheckAndInstallDependencies()
    {
        bool vigemInstalled = IsViGEmInstalled();
        bool hidHideInstalled = IsHidHideInstalled();
        bool hidHideServiceRunning = IsHidHideServiceRunning();
        bool hidHideWasInstalled = false;

        if (!vigemInstalled)
        {
            Console.WriteLine("ViGEm Bus Driver not found. Downloading...");
            if (DownloadAndInstallViGEm())
            {
                Console.WriteLine("ViGEm Bus Driver installed successfully.");
            }
            else
            {
                Console.WriteLine("Failed to install ViGEm Bus Driver.");

                Console.WriteLine("\nPress any key to exit the process...");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        if (!hidHideInstalled)
        {
            Console.WriteLine("HidHide not found. Downloading...");
            if (DownloadAndInstallHidHide())
            {
                Console.WriteLine("HidHide installed successfully.");
                hidHideWasInstalled = true;
            }
            else
            {
                Console.WriteLine("Failed to install HidHide.");

                Console.WriteLine("\nPress any key to exit the process...");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        if (!hidHideServiceRunning && hidHideWasInstalled)
        {
            DialogResult result = MessageBox.Show(
                "HidHide has been installed. A restart is required for changes to take effect.\nRestart now?",
                "Restart Required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                RestartSystem();
            }
        }
    }

    private static bool IsViGEmInstalled()
    {
        //ViGEmBus.sys driver file check
        string driverPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "ViGEmBus.sys");
        bool driverExists = File.Exists(driverPath);

        //ViGEmBus service installed and running check
        try
        {
            using (ServiceController service = new ServiceController("ViGEmBus"))
            {
                bool serviceExists = service.Status != ServiceControllerStatus.Stopped;
                return driverExists && serviceExists;
            }
        }
        catch
        {
            return false;
        }
    }

    private static bool DownloadAndInstallViGEm()
    {
        string url = "https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe";
        string installerPath = Path.Combine(Path.GetTempPath(), "ViGEmBus_1.22.0_x64_x86_arm64.exe");

        try
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, installerPath);
            }

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true
                }
            };

            process.Start();
            process.WaitForExit();

            return IsViGEmInstalled();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error installing ViGEm Bus Driver: {ex.Message}");
            return false;
        }
    }

    private static HidHideControlService _hidHide = new HidHideControlService();
    private static bool IsHidHideInstalled()
    {
        return _hidHide.IsInstalled;
    }

    private static bool IsHidHideServiceRunning()
    {
        try
        {
            ServiceController service = new ServiceController("HidHide");
            return service.Status == ServiceControllerStatus.Running;
        }
        catch
        {
            return false;
        }
    }

    private static bool DownloadAndInstallHidHide()
    {
        string url = "https://github.com/nefarius/HidHide/releases/download/v1.5.230.0/HidHide_1.5.230_x64.exe";
        string installerPath = Path.Combine(Path.GetTempPath(), "HidHide_1.5.230_x64.exe");

        try
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, installerPath);
            }

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true
                }
            };

            process.Start();
            process.WaitForExit();

            return IsHidHideInstalled();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error installing HidHide: {ex.Message}");
            return false;
        }
    }

    private static void RestartSystem()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown",
            Arguments = "/r /t 5",
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }
}
