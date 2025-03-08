using System;
using System.Linq;
using HidSharp;
using Microsoft.VisualBasic;
using Nefarius.ViGEm.Client.Targets.Xbox360;

class ControllerManager
{
    private static HidDevice _controller;
    private static HidStream _stream;
    private static bool _isBluetooth = false;
    private static byte _packetCounter = 0;
    //const int USB_OUTPUT_REPORT_LENGTH = 48;
    //const int BT_OUTPUT_REPORT_LENGTH = 78;

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

        int dpadOffset = _isBluetooth ? 5 : 8;
        int buttonOffset = _isBluetooth ? 5 : 8;
        int shoulderOffset = _isBluetooth ? 6 : 9;
        int specialButtonOffset = _isBluetooth ? 7 : 10;
        int triggerOffset = _isBluetooth ? 8 : 5;

        //Same for USB & Bluetooth
        byte leftStickX = report[1];
        byte leftStickY = report[2];
        byte rightStickX = report[3];
        byte rightStickY = report[4];

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

        byte lTrigger = report[triggerOffset];
        byte rTrigger = report[triggerOffset + 1];

        byte dpadState = (byte)(report[dpadOffset] & 0x0F);
        bool dpadUp = dpadState == 0 || dpadState == 1 || dpadState == 7;
        bool dpadRight = dpadState == 1 || dpadState == 2 || dpadState == 3;
        bool dpadDown = dpadState == 3 || dpadState == 4 || dpadState == 5;
        bool dpadLeft = dpadState == 5 || dpadState == 6 || dpadState == 7;

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
            vibrationReport[0] = 0xA2;
            vibrationReport[1] = 0x31;
            vibrationReport[2] = _packetCounter++;
            if (_packetCounter > 255) _packetCounter = 0;

            vibrationReport[3] = 0x02;
            vibrationReport[4] = rightMotor;
            vibrationReport[5] = leftMotor;

            for (int i = 6; i < vibrationReport.Length - 4; i++)
            {
                vibrationReport[i] = 0x00;
            }

            uint crc32 = CalculateCRC32(vibrationReport, vibrationReport.Length - 4);
            byte[] crcBytes = BitConverter.GetBytes(crc32);
            Array.Copy(crcBytes, 0, vibrationReport, vibrationReport.Length - 4, 4);
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
            Console.WriteLine($"Sending Vibration Report (Bluetooth: {_isBluetooth}): {BitConverter.ToString(vibrationReport)}");
            _stream.Write(vibrationReport, 0, vibrationReport.Length);
            Console.WriteLine("Vibration command sent!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send vibration data: {ex.Message}");
        }
    }

    private static uint CalculateCRC32(byte[] data, int length)
    {
        const uint Polynomial = 0xEDB88320;
        uint crc = 0xFFFFFFFF;

        for (int i = 0; i < length; i++)
        {
            byte b = data[i];
            crc ^= b;
            for (int j = 0; j < 8; j++)
            {
                bool bit = (crc & 1) != 0;
                crc >>= 1;
                if (bit)
                {
                    crc ^= Polynomial;
                }
            }
        }

        return ~crc;
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

}
