namespace DeviceTimer.DeviceTimerApp;

public partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        await Start();
        return 0;
    }

    private static async Task Start() {
        var app = new App();
        await app.Start();
    }
}
