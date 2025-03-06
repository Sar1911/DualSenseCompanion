using System;
using System.Linq;
using HidSharp;
using Microsoft.VisualBasic;
using Nefarius.ViGEm.Client.Targets.Xbox360;

class ControllerManager
{
    private static HidDevice _controller;
    private static HidStream _stream;

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
        if (report.Length < 13)
        {
            Console.WriteLine("Invalid report length.");
            return;
        }

        byte leftStickX = report[1];
        byte leftStickY = report[2];
        byte rightStickX = report[3];
        byte rightStickY = report[4];

        short xboxLeftStickX = (short)((leftStickX - 128) * 258);
        short xboxLeftStickY = (short)((127 - leftStickY) * 258);
        short xboxRightStickX = (short)((rightStickX - 128) * 258);
        short xboxRightStickY = (short)((127 - rightStickY) * 258);

        // Prevent stick values flipping at the edges
        if (leftStickX == 0) xboxLeftStickX = -32768;
        if (leftStickX == 255) xboxLeftStickX = 32767;
        if (leftStickY == 0) xboxLeftStickY = 32767;
        if (leftStickY == 255) xboxLeftStickY = -32768;

        if (rightStickX == 0) xboxRightStickX = -32768;
        if (rightStickX == 255) xboxRightStickX = 32767;
        if (rightStickY == 0) xboxRightStickY = 32767;
        if (rightStickY == 255) xboxRightStickY = -32768;

        byte lTrigger = report[5];
        byte rTrigger = report[6];

        byte dpadState = (byte)(report[8] & 0x0F);
        bool dpadUp = dpadState == 0 || dpadState == 1 || dpadState == 7;
        bool dpadRight = dpadState == 1 || dpadState == 2 || dpadState == 3;
        bool dpadDown = dpadState == 3 || dpadState == 4 || dpadState == 5;
        bool dpadLeft = dpadState == 5 || dpadState == 6 || dpadState == 7;

        bool squarePressed = (report[8] & 0x10) != 0;
        bool crossPressed = (report[8] & 0x20) != 0;
        bool circlePressed = (report[8] & 0x40) != 0;
        bool trianglePressed = (report[8] & 0x80) != 0;

        bool l1Pressed = (report[9] & 0x01) != 0;
        bool r1Pressed = (report[9] & 0x02) != 0;
        bool l3Pressed = (report[9] & 0x40) != 0;
        bool r3Pressed = (report[9] & 0x80) != 0;
        bool createPressed = (report[9] & 0x10) != 0;
        bool optionsPressed = (report[9] & 0x20) != 0;

        bool psPressed = (report[10] & 0x01) != 0;

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
            return;

        byte[] vibrationReport = new byte[48];

        vibrationReport[0] = 0x02;
        vibrationReport[1] = 0x01 | 0x02;
        vibrationReport[3] = rightMotor;
        vibrationReport[4] = leftMotor;

        // Remaining bytes zeroed out
        for (int i = 5; i < vibrationReport.Length; i++)
        {
            vibrationReport[i] = 0x00;
        }

        try
        {
            //Console.WriteLine($"Sending Vibration Report: {BitConverter.ToString(vibrationReport)}");
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
        return deviceList.GetHidDevices().FirstOrDefault(d => d.VendorID == 0x054C && d.ProductID == 0x0CE6);
    }

}
