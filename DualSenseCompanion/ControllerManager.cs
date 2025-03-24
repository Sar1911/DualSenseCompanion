using HidSharp;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using DualSenseCompanion;
using System.Reactive.Linq;

class ControllerManager
{
    private static HidDevice _controller;
    private static HidStream _stream;
    private static bool _isBluetooth = false;
    private static bool dualsenseConnected;

    public static void StartListening()
    {
        _controller = GetPS5Controller();

        if (_controller == null)
        {
            Console.WriteLine("PS5 Controller not found. Exiting...");
            return;
        }

        Console.WriteLine("Starting input listener...");
        _stream = _controller.Open();
        _stream.ReadTimeout = Timeout.Infinite;

        byte[] inputReport = new byte[_controller.MaxInputReportLength];

        //Console.WriteLine("IRL1 "+_controller.MaxInputReportLength);
        //Console.WriteLine("IRL2 "+_controller.GetMaxInputReportLength());
        //Console.WriteLine("IRL3 "+_controller.GetMaxFeatureReportLength());

        while (true)
        {
            try
            {
                int bytesRead = _stream.Read(inputReport);
                //Console.WriteLine("Input Report Data: " + BitConverter.ToString(inputReport));

                if (bytesRead > 0)
                {
                    ProcessInput(inputReport);
                    //SendVibrationToPS5(255, 255);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("Controller disconnected.");
                Console.WriteLine($"Error: {ex.Message}");
                break;
            }
        }

    }

    private static void ProcessInput(byte[] report)
    {
        if (report.Length < 10)
        {
            Console.WriteLine("Invalid report length.");
            return;
        }

        byte reportId = report[0];

        if (reportId == 0x01)
        {
            ProcessStandardInput(report);
        }
        else if (reportId == 0x31)
        {
            ProcessExtendedInput(report);
        }
        else
        {
            Console.WriteLine($"Unknown report type");
        }
    }

    private static void ProcessStandardInput(byte[] report)
    {   
        //Offsets for BT/USB
        int dpadOffset = _isBluetooth ? 5 : 8;
        int buttonOffset = _isBluetooth ? 5 : 8;
        int shoulderOffset = _isBluetooth ? 6 : 9;
        int specialButtonOffset = _isBluetooth ? 7 : 10;
        int triggerOffset = _isBluetooth ? 8 : 5;

        //Sticks
        byte leftStickX = report[1];
        byte leftStickY = report[2];
        byte rightStickX = report[3];
        byte rightStickY = report[4];

        //Convert to Xbox stick ranges
        short xboxLeftStickX = (short)((leftStickX - 128) * 258);
        short xboxLeftStickY = (short)((127 - leftStickY) * 258);
        short xboxRightStickX = (short)((rightStickX - 128) * 258);
        short xboxRightStickY = (short)((127 - rightStickY) * 258);

        //Prevent stick values from flipping at the edges
        if (leftStickX == 0) xboxLeftStickX = -32768;
        if (leftStickX == 255) xboxLeftStickX = 32767;
        if (leftStickY == 0) xboxLeftStickY = 32767;
        if (leftStickY == 255) xboxLeftStickY = -32768;

        if (rightStickX == 0) xboxRightStickX = -32768;
        if (rightStickX == 255) xboxRightStickX = 32767;
        if (rightStickY == 0) xboxRightStickY = 32767;
        if (rightStickY == 255) xboxRightStickY = -32768;

        //Triggers
        byte lTrigger = report[triggerOffset];
        byte rTrigger = report[triggerOffset + 1];

        //D-Pad
        byte dpadState = (byte)(report[dpadOffset] & 0x0F);
        bool dpadUp = dpadState == 0 || dpadState == 1 || dpadState == 7;
        bool dpadRight = dpadState == 1 || dpadState == 2 || dpadState == 3;
        bool dpadDown = dpadState == 3 || dpadState == 4 || dpadState == 5;
        bool dpadLeft = dpadState == 5 || dpadState == 6 || dpadState == 7;

        //Buttons
        bool squarePressed = (report[buttonOffset] & 0x10) != 0;
        bool crossPressed = (report[buttonOffset] & 0x20) != 0;
        bool circlePressed = (report[buttonOffset] & 0x40) != 0;
        bool trianglePressed = (report[buttonOffset] & 0x80) != 0;

        bool l1Pressed = (report[shoulderOffset] & 0x01) != 0;
        bool r1Pressed = (report[shoulderOffset] & 0x02) != 0;
        bool l3Pressed = (report[shoulderOffset] & 0x40) != 0;
        bool r3Pressed = (report[shoulderOffset] & 0x80) != 0;
        bool createPressed = (report[shoulderOffset] & 0x10) != 0;
        bool optionsPressed = (report[shoulderOffset] & 0x20) != 0;
        bool psPressed = (report[specialButtonOffset] & 0x01) != 0;

        MapToXboxController(dpadUp, dpadDown, dpadLeft, dpadRight, squarePressed, crossPressed, circlePressed, trianglePressed,
                            l1Pressed, r1Pressed, l3Pressed, r3Pressed, createPressed, optionsPressed, psPressed,
                            lTrigger, rTrigger, xboxLeftStickX, xboxLeftStickY, xboxRightStickX, xboxRightStickY);
    }



    private static void ProcessExtendedInput(byte[] report)
    {
        //No need for offsets since we are currently using extended input report only in BT
        //Sticks
        byte leftStickX = report[2];
        byte leftStickY = report[3];
        byte rightStickX = report[4];
        byte rightStickY = report[5];

        //Convert to Xbox stick ranges
        short xboxLeftStickX = (short)((leftStickX - 128) * 258);
        short xboxLeftStickY = (short)((127 - leftStickY) * 258);
        short xboxRightStickX = (short)((rightStickX - 128) * 258);
        short xboxRightStickY = (short)((127 - rightStickY) * 258);

        //Prevent stick values from flipping at the edges
        if (leftStickX == 0) xboxLeftStickX = -32768;
        if (leftStickX == 255) xboxLeftStickX = 32767;
        if (leftStickY == 0) xboxLeftStickY = 32767;
        if (leftStickY == 255) xboxLeftStickY = -32768;

        if (rightStickX == 0) xboxRightStickX = -32768;
        if (rightStickX == 255) xboxRightStickX = 32767;
        if (rightStickY == 0) xboxRightStickY = 32767;
        if (rightStickY == 255) xboxRightStickY = -32768;

        //Triggers
        byte lTrigger = report[6];
        byte rTrigger = report[7];

        //D-Pad
        byte dpadState = (byte)(report[9] & 0x0F);
        bool dpadUp = dpadState == 0 || dpadState == 1 || dpadState == 7;
        bool dpadRight = dpadState == 1 || dpadState == 2 || dpadState == 3;
        bool dpadDown = dpadState == 3 || dpadState == 4 || dpadState == 5;
        bool dpadLeft = dpadState == 5 || dpadState == 6 || dpadState == 7;

        //Buttons
        bool squarePressed = (report[9] & (1 << 4)) != 0;
        bool crossPressed = (report[9] & (1 << 5)) != 0;
        bool circlePressed = (report[9] & (1 << 6)) != 0;
        bool trianglePressed = (report[9] & (1 << 7)) != 0;

        bool l1Pressed = (report[10] & (1 << 0)) != 0;
        bool r1Pressed = (report[10] & (1 << 1)) != 0;
        bool l3Pressed = (report[10] & (1 << 6)) != 0;
        bool r3Pressed = (report[10] & (1 << 7)) != 0;
        bool createPressed = (report[10] & (1 << 4)) != 0;
        bool optionsPressed = (report[10] & (1 << 5)) != 0;
        bool psPressed = (report[11] & (1 << 0)) != 0;

        //bool touchpadPressed = (report[11] & 0x02) != 0; //something for later :)

        MapToXboxController(dpadUp, dpadDown, dpadLeft, dpadRight, squarePressed, crossPressed, circlePressed, trianglePressed,
                            l1Pressed, r1Pressed, l3Pressed, r3Pressed, createPressed, optionsPressed, psPressed,
                            lTrigger, rTrigger, xboxLeftStickX, xboxLeftStickY, xboxRightStickX, xboxRightStickY);
    }

    private static void MapToXboxController(bool dpadUp, bool dpadDown, bool dpadLeft, bool dpadRight,
                                        bool squarePressed, bool crossPressed, bool circlePressed, bool trianglePressed,
                                        bool l1Pressed, bool r1Pressed, bool l3Pressed, bool r3Pressed,
                                        bool createPressed, bool optionsPressed, bool psPressed,
                                        byte lTrigger, byte rTrigger,
                                        short xboxLeftStickX, short xboxLeftStickY,
                                        short xboxRightStickX, short xboxRightStickY)
    {
        XboxEmulator.SetButtonState(Xbox360Button.A, crossPressed);
        XboxEmulator.SetButtonState(Xbox360Button.B, circlePressed);
        XboxEmulator.SetButtonState(Xbox360Button.X, squarePressed);
        XboxEmulator.SetButtonState(Xbox360Button.Y, trianglePressed);

        XboxEmulator.SetButtonState(Xbox360Button.LeftShoulder, l1Pressed);
        XboxEmulator.SetButtonState(Xbox360Button.RightShoulder, r1Pressed);
        XboxEmulator.SetButtonState(Xbox360Button.LeftThumb, l3Pressed);
        XboxEmulator.SetButtonState(Xbox360Button.RightThumb, r3Pressed);
        XboxEmulator.SetButtonState(Xbox360Button.Start, optionsPressed);
        XboxEmulator.SetButtonState(Xbox360Button.Back, createPressed);
        XboxEmulator.SetButtonState(Xbox360Button.Guide, psPressed);

        XboxEmulator.SetButtonState(Xbox360Button.Up, dpadUp);
        XboxEmulator.SetButtonState(Xbox360Button.Down, dpadDown);
        XboxEmulator.SetButtonState(Xbox360Button.Left, dpadLeft);
        XboxEmulator.SetButtonState(Xbox360Button.Right, dpadRight);

        XboxEmulator.SetTriggerState(Xbox360Slider.LeftTrigger, lTrigger);
        XboxEmulator.SetTriggerState(Xbox360Slider.RightTrigger, rTrigger);

        XboxEmulator.SetAxisState(Xbox360Axis.LeftThumbX, xboxLeftStickX);
        XboxEmulator.SetAxisState(Xbox360Axis.LeftThumbY, xboxLeftStickY);
        XboxEmulator.SetAxisState(Xbox360Axis.RightThumbX, xboxRightStickX);
        XboxEmulator.SetAxisState(Xbox360Axis.RightThumbY, xboxRightStickY);
    }


    //old function(only handles the basic report type)
    //private static void ProcessInput(byte[] report)
    //{
    //    if (report.Length < 10)
    //    {
    //        Console.WriteLine("Invalid report length.");
    //        return;
    //    }

    //    int dpadOffset = _isBluetooth ? 5 : 8;
    //    int buttonOffset = _isBluetooth ? 5 : 8;
    //    int shoulderOffset = _isBluetooth ? 6 : 9;
    //    int specialButtonOffset = _isBluetooth ? 7 : 10;
    //    int triggerOffset = _isBluetooth ? 8 : 5;

    //    //Same for USB & Bluetooth
    //    byte leftStickX = report[1];
    //    byte leftStickY = report[2];
    //    byte rightStickX = report[3];
    //    byte rightStickY = report[4];

    //    short xboxLeftStickX = (short)((leftStickX - 128) * 258);
    //    short xboxLeftStickY = (short)((127 - leftStickY) * 258);
    //    short xboxRightStickX = (short)((rightStickX - 128) * 258);
    //    short xboxRightStickY = (short)((127 - rightStickY) * 258);

    //    //Prevent stick values from flipping at the edges
    //    if (leftStickX == 0) xboxLeftStickX = -32768;
    //    if (leftStickX == 255) xboxLeftStickX = 32767;
    //    if (leftStickY == 0) xboxLeftStickY = 32767;
    //    if (leftStickY == 255) xboxLeftStickY = -32768;

    //    if (rightStickX == 0) xboxRightStickX = -32768;
    //    if (rightStickX == 255) xboxRightStickX = 32767;
    //    if (rightStickY == 0) xboxRightStickY = 32767;
    //    if (rightStickY == 255) xboxRightStickY = -32768;

    //    byte lTrigger = report[triggerOffset];
    //    byte rTrigger = report[triggerOffset + 1];

    //    byte dpadState = (byte)(report[dpadOffset] & 0x0F);
    //    bool dpadUp = dpadState == 0 || dpadState == 1 || dpadState == 7;
    //    bool dpadRight = dpadState == 1 || dpadState == 2 || dpadState == 3;
    //    bool dpadDown = dpadState == 3 || dpadState == 4 || dpadState == 5;
    //    bool dpadLeft = dpadState == 5 || dpadState == 6 || dpadState == 7;

    //    bool squarePressed = (report[buttonOffset] & 0x10) != 0;
    //    bool crossPressed = (report[buttonOffset] & 0x20) != 0;
    //    bool circlePressed = (report[buttonOffset] & 0x40) != 0;
    //    bool trianglePressed = (report[buttonOffset] & 0x80) != 0;

    //    bool l1Pressed = (report[shoulderOffset] & 0x01) != 0;
    //    bool r1Pressed = (report[shoulderOffset] & 0x02) != 0;
    //    bool l3Pressed = (report[shoulderOffset] & 0x40) != 0;
    //    bool r3Pressed = (report[shoulderOffset] & 0x80) != 0;
    //    bool createPressed = (report[shoulderOffset] & 0x10) != 0;
    //    bool optionsPressed = (report[shoulderOffset] & 0x20) != 0;

    //    bool psPressed = (report[specialButtonOffset] & 0x01) != 0;

    //    XboxEmulator.SetButtonState(Xbox360Button.A, crossPressed);
    //    XboxEmulator.SetButtonState(Xbox360Button.B, circlePressed);
    //    XboxEmulator.SetButtonState(Xbox360Button.X, squarePressed);
    //    XboxEmulator.SetButtonState(Xbox360Button.Y, trianglePressed);

    //    XboxEmulator.SetButtonState(Xbox360Button.LeftShoulder, l1Pressed);
    //    XboxEmulator.SetButtonState(Xbox360Button.RightShoulder, r1Pressed);
    //    XboxEmulator.SetButtonState(Xbox360Button.LeftThumb, l3Pressed);
    //    XboxEmulator.SetButtonState(Xbox360Button.RightThumb, r3Pressed);
    //    XboxEmulator.SetButtonState(Xbox360Button.Start, optionsPressed);
    //    XboxEmulator.SetButtonState(Xbox360Button.Back, createPressed);
    //    XboxEmulator.SetButtonState(Xbox360Button.Guide, psPressed);

    //    XboxEmulator.SetButtonState(Xbox360Button.Up, dpadUp);
    //    XboxEmulator.SetButtonState(Xbox360Button.Down, dpadDown);
    //    XboxEmulator.SetButtonState(Xbox360Button.Left, dpadLeft);
    //    XboxEmulator.SetButtonState(Xbox360Button.Right, dpadRight);

    //    XboxEmulator.SetTriggerState(Xbox360Slider.LeftTrigger, lTrigger);
    //    XboxEmulator.SetTriggerState(Xbox360Slider.RightTrigger, rTrigger);

    //    XboxEmulator.SetAxisState(Xbox360Axis.LeftThumbX, xboxLeftStickX);
    //    XboxEmulator.SetAxisState(Xbox360Axis.LeftThumbY, xboxLeftStickY);
    //    XboxEmulator.SetAxisState(Xbox360Axis.RightThumbX, xboxRightStickX);
    //    XboxEmulator.SetAxisState(Xbox360Axis.RightThumbY, xboxRightStickY);
    //}

    public static void InitializeVibration()
    {
        XboxEmulator.OnVibrationChanged += SendVibrationToPS5;
    }

    public static void SendVibrationToPS5(byte leftMotor, byte rightMotor)
    {
        if (_stream == null)
        {
            return;
        }

        int reportSize = _isBluetooth ? 78 : 48;
        byte[] vibrationReport = new byte[reportSize];

        if (_isBluetooth)
        {
            vibrationReport[0] = 0x31;//Report ID for Bluetooth
            vibrationReport[1] = 0x02;
            vibrationReport[2] = 0x0F;
            vibrationReport[3] = 0x55;
            vibrationReport[4] = rightMotor;
            vibrationReport[5] = leftMotor;
            vibrationReport[40] = 0x06;
            vibrationReport[43] = 0x02;
            vibrationReport[44] = 0x02;
            vibrationReport[48] = 0xFF;

            uint crcChecksum = NewCrc.ComputeCRC32(vibrationReport, 74);
            byte[] checksumBytes = BitConverter.GetBytes(crcChecksum);
            Array.Copy(checksumBytes, 0, vibrationReport, 74, 4);
        }
        else
        {
            vibrationReport[0] = 0x02;
            vibrationReport[1] = 0x01 | 0x02;
            vibrationReport[3] = rightMotor;
            vibrationReport[4] = leftMotor;
        }

        try
        {
            //Console.WriteLine($"Sending Vibration Report (Bluetooth: {_isBluetooth}): {BitConverter.ToString(vibrationReport)}");
            _stream.Write(vibrationReport, 0, vibrationReport.Length);
            //Console.WriteLine("Vibration command sent!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send vibration data: {ex.Message}");
        }
    }

    private static HidDevice GetPS5Controller()
    {
        var deviceList = DeviceList.Local;
        var device = deviceList.GetHidDevices().FirstOrDefault(d => d.VendorID == 0x054C && d.ProductID == 0x0CE6);

        if (device != null)
        {
            _isBluetooth = device.GetMaxFeatureReportLength() == 64 ? false : true;
            Console.WriteLine($"PS5 Controller Detected ({(_isBluetooth ? "Bluetooth" : "USB")})");
        }

        return device;
    }

    //alternate function which waits for user to connect controller first (better approach implemented in CloakManager)
    //private static HidDevice GetPS5Controller()
    //{
    //    var deviceList = DeviceList.Local;
    //    HidDevice controller = null;
    //    Console.WriteLine("Looking for controller...");

    //    while (!dualsenseConnected)
    //    {
    //        controller = deviceList.GetHidDevices().FirstOrDefault(d => d.VendorID == 0x054C && d.ProductID == 0x0CE6);

    //        if (controller != null)
    //        {
    //            _isBluetooth = controller.GetMaxFeatureReportLength() == 64 ? false : true;
    //            Console.WriteLine($"PS5 Controller Detected ({(_isBluetooth ? "Bluetooth" : "USB")})");
    //            dualsenseConnected = true;
    //        }
    //    }
    //    return controller;
    //}

}
