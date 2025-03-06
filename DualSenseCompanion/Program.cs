class Program
{
    static void Main(string[] args)
    {
        XboxEmulator.Initialize();
        ControllerManager.StartListening();
        ControllerManager.InitializeVibration();
    }
}
