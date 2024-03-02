using System.Device.Gpio;
using System.Device.Gpio.Drivers;


namespace DeviceTimer.IOController;

public class RaspberryPi5Gpio
{
    private GpioController _gpioController;

    public void Init()
    {
        _gpioController = new GpioController(PinNumberingScheme.Logical, new LibGpiodDriver(4));
        // controller.OpenPin(pin20, PinMode.InputPullUp);

        // controller.RegisterCallbackForPinValueChangedEvent(
        //     pin20,
        //     PinEventTypes.Falling | PinEventTypes.Rising,
        //     OnPinEvent);

        // void OnPinEvent(object sender, PinValueChangedEventArgs args)
        // {
        //     if (args.ChangeType is PinEventTypes.Rising)
        //     {
        //         controller.Write(pin14, PinValue.High);
        //     }
        //     else if (args.ChangeType is PinEventTypes.Falling)
        //     {
        //         controller.Write(pin14, PinValue.Low);
        //     }
        //     Console.WriteLine(
        //         $"({DateTime.Now}) {(args.ChangeType is PinEventTypes.Rising ? Alert : Ready)}");
        // }
    }

    public void ConfigurePinAsOutput(int pin)
    {
        _gpioController.OpenPin(pin, PinMode.Output);
    }

    public void SetOutputPinValue(int pin, Boolean value)
    {
        _gpioController.Write(pin, value ? PinValue.High : PinValue.Low);
    }

    public void ClosePin(int pin)
    {
        if (!_gpioController.IsPinOpen(pin))
        {
            return;
        }
        _gpioController.ClosePin(pin);
    }
}
