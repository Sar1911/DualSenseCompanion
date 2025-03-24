using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

class XboxEmulator
{
    private static ViGEmClient? _client;
    private static IXbox360Controller? _controller;

    public static event Action<byte, byte> OnVibrationChanged;

    public static void Initialize()
    {
        _client = new ViGEmClient();
        _controller = _client.CreateXbox360Controller();
        _controller.Connect();

        _controller.FeedbackReceived -= OnFeedbackReceived;
        _controller.FeedbackReceived += OnFeedbackReceived;

        if (_controller != null)
        {
            Console.WriteLine("Virtual Xbox 360 Controller Created!");
            Console.WriteLine("Close this window to disconnect");
        }
        
        else
            Console.WriteLine("Failed to create virtual Xbox controller.");
    }

    private static void OnFeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs e)
    {
        ControllerManager.SendVibrationToPS5(e.LargeMotor, e.SmallMotor);
    }

    public static void SetButtonState(Xbox360Button button, bool pressed)
    {
        if (_controller != null)
            _controller.SetButtonState(button, pressed);
    }

    public static void SetTriggerState(Xbox360Slider trigger, byte value)
    {
        if (_controller != null)
            _controller.SetSliderValue(trigger, value);
    }

    public static void SetAxisState(Xbox360Axis axis, short value)
    {
        _controller?.SetAxisValue(axis, value);
    }
}
