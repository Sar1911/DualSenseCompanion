
class Program
{
    static void Main(string[] args)
    {
        DependencyManager.CheckAndInstallDependencies();
        Console.WriteLine("All dependencies verified. Starting DualSenseCompanion...");
        XboxEmulator.Initialize();
        CloakManager.HidePS5Controller();
        ControllerManager.StartListening();
        ControllerManager.InitializeVibration();
    }


}

