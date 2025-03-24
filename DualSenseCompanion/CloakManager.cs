using System.Diagnostics;
using Nefarius.Drivers.HidHide;
using System.Management;

class CloakManager
{
    private static HidHideControlService _hidHide = new HidHideControlService();
    private static string _ps5ControllerInstanceId;
    private static string _exePath = Process.GetCurrentProcess().MainModule.FileName;
    private static bool controllerConnected;

    public static void HidePS5Controller()
    {
        if (!_hidHide.IsInstalled)
        {
            Console.WriteLine("HidHide is not installed. Skipping controller hiding.");
            return;
        }

        _ps5ControllerInstanceId = FindPS5ControllerInstanceId();

        if (string.IsNullOrEmpty(_ps5ControllerInstanceId))
        {
            Console.WriteLine("No DualSense controller found.");
            return;
        }

        Console.WriteLine($"Attempting to block controller: {_ps5ControllerInstanceId}");

        WhitelistCurrentApp();

        if (!_hidHide.BlockedInstanceIds.Contains(_ps5ControllerInstanceId))
        {
            _hidHide.AddBlockedInstanceId(_ps5ControllerInstanceId);
            Console.WriteLine($"Added PS5 Controller ({_ps5ControllerInstanceId}) to HidHide block list.");
        }
        else
        {
            Console.WriteLine("PS5 Controller is already in the block list.");
        }

        _hidHide.IsActive = true;
        Console.WriteLine("PS5 Controller is now hidden!");

        // Unhides controller and removes app whitlist from HidHide on exit
        AppDomain.CurrentDomain.ProcessExit += (s, e) => UnhidePS5Controller();

    }

    public static void UnhidePS5Controller()
    {
        if (!_hidHide.IsInstalled)
        {
            Console.WriteLine("HidHide is not installed. Skipping controller unhiding.");
            return;
        }

        if (string.IsNullOrEmpty(_ps5ControllerInstanceId))
        {
            Console.WriteLine("No DualSense controller found.");
            return;
        }

        if (_hidHide.BlockedInstanceIds.Contains(_ps5ControllerInstanceId))
        {
            _hidHide.RemoveBlockedInstanceId(_ps5ControllerInstanceId);
            Console.WriteLine("PS5 Controller removed from HidHide block list.");
        }

        _hidHide.IsActive = false;
        Console.WriteLine("PS5 Controller is now visible!");

        if (_hidHide.ApplicationPaths.Contains(_exePath))
        {
            _hidHide.RemoveApplicationPath(_exePath);
            Console.WriteLine("Removed application from HidHide whitelist.");
        }
    }

    //This version of the function is more optimized and actually waits for the user to plug in the controller
    public static string FindPS5ControllerInstanceId()
    {

        Console.WriteLine("Looking for controller...");
        List<string> devices = new List<string>();

        while (true)
        {

            devices.Clear();
            devices.AddRange(GetAllHIDDevices());
           
            foreach (var device in devices)
            {

                if (device.Contains("VID_054C&PID_0CE6"))
                {
                    Console.WriteLine("Found DualSense Controller (USB)");
                    controllerConnected = true;
                    return device;
                }

                if (device.Contains("_VID&0002054C_PID&0CE6"))
                {
                    Console.WriteLine("Found DualSense Controller (Bluetooth)");
                    controllerConnected = true;
                    return device;
                }
            }

            Thread.Sleep(1000); //1 second delay before the next check
        }

    }

    //public static string FindPS5ControllerInstanceId()
    //{
    //    var devices = GetAllHIDDevices();

    //    foreach (var device in devices)
    //    {
    //        Console.WriteLine($"Checking Device: {device}");

    //        // USB DualSense
    //        if (device.Contains("VID_054C&PID_0CE6"))
    //        {
    //            Console.WriteLine("Found DualSense Controller (USB)");
    //            return device;
    //        }

    //        // Bluetooth DualSense
    //        if (device.Contains("_VID&0002054C_PID&0CE6"))
    //        {
    //            Console.WriteLine("Found DualSense Controller (Bluetooth)");
    //            return device;
    //        }
    //    }

    //    Console.WriteLine("No DualSense controller found.");
    //    return null;
    //}

    public static List<string> GetAllHIDDevices()
    {
        List<string> deviceList = new List<string>();

        try
        {
            using (var searcher = new ManagementObjectSearcher(@"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'HID%'"))
            {
                foreach (ManagementObject device in searcher.Get())
                {
                    string deviceId = device["DeviceID"]?.ToString();
                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        deviceList.Add(deviceId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving HID devices: {ex.Message}");
        }

        return deviceList;
    }
    private static void WhitelistCurrentApp()
    {
        if (!_hidHide.ApplicationPaths.Contains(_exePath))
        {
            _hidHide.AddApplicationPath(_exePath);
            Console.WriteLine($"Added {System.IO.Path.GetFileName(_exePath)} to HidHide whitelist.");
        }
        else
        {
            Console.WriteLine("Application is already whitelisted.");
        }
    }
}