namespace DeviceTimer.DeviceTimerApp;

public partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        await Start(args);
        return 0;
    }

    private static async Task Start(string[] args) {
        var app = new App(args);
        await app.Start();
    }
}
