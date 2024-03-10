using System.Device.Gpio;
using DeviceTimer.IOController;

namespace DeviceTimerApp;

public class PlayStationPower
{
    private readonly RaspberryPi5Gpio gpio;
    private readonly GpioPin outputPin;
    private readonly GpioPin inputPin;
    private CancellationTokenSource powerOnCancellationTokenSource;
    private Task powerOnTask;

    public PlayStationPower(RaspberryPi5Gpio gpio, GpioPin inputPin, GpioPin outputPin)
    {
        this.gpio = gpio;
        this.outputPin = outputPin;
        this.inputPin = inputPin;
    }

    public void StartMonitoring()
    {
        gpio.RegisterCallbackForPinValueChangedEvent(
            inputPin.PinNumber,
            PinEventTypes.Rising | PinEventTypes.Falling,
            OnPlayStationPowerInputPinValueChanged
        );
        powerOnCancellationTokenSource = new CancellationTokenSource();
        powerOnTask = Task.Factory.StartNew(PowerOnAction, powerOnCancellationTokenSource.Token);
    }

    public void StopMonitoring()
    {
        powerOnCancellationTokenSource.Cancel();
        if (powerOnTask != null)
        {
            Task.WaitAll([powerOnTask]);
        }
        gpio.UnregisterCallbackForPinValueChangedEvent(inputPin.PinNumber, OnPlayStationPowerInputPinValueChanged);
    }

    public void Dispose()
    {
        // TODO: Stop the threads, unsubscribe etc.
        StopMonitoring();
    }

    private async void PowerOnAction()
    {
        while (!powerOnCancellationTokenSource.IsCancellationRequested)
        {
            if (!powerOnCancellationTokenSource.IsCancellationRequested)
            {
                outputPin.Write(PinValue.Low);
                await Task.Delay(TimeSpan.FromSeconds(5), powerOnCancellationTokenSource.Token);
            }
            if (!powerOnCancellationTokenSource.IsCancellationRequested)
            {
                outputPin.Write(PinValue.High);
                await Task.Delay(TimeSpan.FromSeconds(1), powerOnCancellationTokenSource.Token);
            }
        }
        outputPin.Write(PinValue.Low);
    }

    private void OnPlayStationPowerInputPinValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
    {
        Console.WriteLine("{0} : PlayStationPowerInputPin value {1}", DateTime.Now.ToString("s"), pinValueChangedEventArgs.ChangeType);
    }
}
