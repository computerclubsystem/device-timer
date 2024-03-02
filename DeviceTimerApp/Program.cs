// using System.Device.Gpio;
// using System.Device.Gpio.Drivers;

// var _gpioController = new GpioController(PinNumberingScheme.Logical, new LibGpiodDriver(4));

// _gpioController.OpenPin(23, PinMode.Output);
// _gpioController.Write(23, PinValue.Low);

// See https://aka.ms/new-console-template for more information
using DeviceTimer.IOController;
using DeviceTimerApp;

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
