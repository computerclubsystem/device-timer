using System.Device.Gpio;
using System.Device.Gpio.Drivers;

namespace DeviceTimer.IOController;

public class RaspberryPi5Gpio
{
    private GpioController _gpioController;

    public void Init()
    {
        // On Raspberry Pi 5 the driver will be autoselected to "new LibGpioDriver(4)"
        // int gpioChipNumber = 4;
        _gpioController = new GpioController(PinNumberingScheme.Logical); //, new LibGpiodDriver(gpioChipNumber));
        // _gpioController.GetPinMode(1);
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

    public GpioPin ConfigurePinAsOutput(int pin)
    {
        return _gpioController.OpenPin(pin, PinMode.Output);
    }

    public void SetOutputPinValue(int pin, Boolean value)
    {
        _gpioController.Write(pin, value ? PinValue.High : PinValue.Low);
    }

    public GpioPin ConfigurePinAsInput(int pin, PinMode pinMode)
    {
        return _gpioController.OpenPin(pin, pinMode);
    }

    public void RegisterCallbackForPinValueChangedEvent(int pin, PinEventTypes eventTypes, PinChangeEventHandler eventHandler)
    {
        _gpioController.RegisterCallbackForPinValueChangedEvent(
            pin,
            eventTypes,
            eventHandler
        );
    }

    public void UnregisterCallbackForPinValueChangedEvent(int pin, PinChangeEventHandler eventHandler)
    {
        _gpioController.UnregisterCallbackForPinValueChangedEvent(pin, eventHandler);
    }

    public void ClosePin(int pin)
    {
        if (!_gpioController.IsPinOpen(pin))
        {
            return;
        }
        _gpioController.ClosePin(pin);
    }

    public void Dispose()
    {
        _gpioController.Dispose();
    }
}
