using System.Device.Gpio;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DeviceTimer.IOController;

namespace DeviceTimer.DeviceTimerApp;

public class PlayStationPower
{
    private readonly RaspberryPi5Gpio gpio;
    private readonly GpioPin outputPin;
    private readonly GpioPin inputPin;
    private CancellationTokenSource tryPowerOnCancellationTokenSource;
    private Task powerOnTask;
    private readonly Subject<PinValueChangedEventArgs> inputPinValueChangesSubject;
    private readonly IObservable<PinValueChangedEventArgs> inputPinValueChangesObservable;
    private readonly Subject<PinEventTypes> inputPinValueChangeDetectedSubject;
    private readonly object lockObject = new();

    private IDisposable inputPinValueChangesSubscription;
    private readonly TimeSpan inputPinThrottleTimeSpan = TimeSpan.FromSeconds(1);

    public PlayStationPower(RaspberryPi5Gpio gpio, GpioPin inputPin, GpioPin outputPin)
    {
        this.gpio = gpio;
        this.outputPin = outputPin;
        this.inputPin = inputPin;
        inputPinValueChangesSubject = new();
        inputPinValueChangesObservable = inputPinValueChangesSubject.AsObservable();
        inputPinValueChangeDetectedSubject = new();
    }

    public IObservable<PinEventTypes> GetPowerButtonChangeDetectedObservable()
    {
        return inputPinValueChangeDetectedSubject.AsObservable();
    }

    public void StartMonitoring()
    {
        inputPinValueChangesSubscription = inputPinValueChangesObservable
            .Throttle(inputPinThrottleTimeSpan)
            .Subscribe(OnPowerInputPinChangeDetected);
        gpio.RegisterCallbackForPinValueChangedEvent(
            inputPin.PinNumber,
            PinEventTypes.Rising | PinEventTypes.Falling,
            OnPlayStationPowerInputPinValueChanged
        );
        tryPowerOnCancellationTokenSource = new CancellationTokenSource();
        powerOnTask = Task.Factory.StartNew(TryPowerOnAction, tryPowerOnCancellationTokenSource.Token);
    }

    private void OnPowerInputPinChangeDetected(PinValueChangedEventArgs args)
    {
        inputPinValueChangeDetectedSubject.OnNext(args.ChangeType);
        if (args.ChangeType == PinEventTypes.Rising)
        {
            SetTryPowerOnOutputPinValue(PinValue.Low);
        }
        Console.WriteLine("PS power button value changed {0}", args.ChangeType);
    }

    private void SetTryPowerOnOutputPinValue(PinValue pinValue)
    {
        lock (lockObject)
        {
            outputPin.Write(pinValue);
        }
    }

    public void StopMonitoring()
    {
        gpio.UnregisterCallbackForPinValueChangedEvent(inputPin.PinNumber, OnPlayStationPowerInputPinValueChanged);
        SetTryPowerOnOutputPinValue(PinValue.Low);
    }

    public void Dispose()
    {
        // Stop the threads, unsubscribe etc.
        inputPinValueChangesSubscription?.Dispose();
        try
        {
            tryPowerOnCancellationTokenSource.Cancel();
        }
        catch { }
        StopMonitoring();
    }

    private async void TryPowerOnAction()
    {
        while (!tryPowerOnCancellationTokenSource.IsCancellationRequested)
        {
            if (!tryPowerOnCancellationTokenSource.IsCancellationRequested)
            {
                SetTryPowerOnOutputPinValue(PinValue.Low);
                await Task.Delay(TimeSpan.FromSeconds(5), tryPowerOnCancellationTokenSource.Token);
            }
            if (!tryPowerOnCancellationTokenSource.IsCancellationRequested)
            {
                if (IsPlayStationPowerOff())
                {
                    SetTryPowerOnOutputPinValue(PinValue.High);
                    await Task.Delay(TimeSpan.FromSeconds(1), tryPowerOnCancellationTokenSource.Token);
                }
            }
        }
        // The thread was cancelled - turn off the power on output pin
        SetTryPowerOnOutputPinValue(PinValue.Low);
    }

    private bool IsPlayStationPowerOff()
    {
        var value = inputPin.Read();
        return value == PinValue.Low;
    }

    private void OnPlayStationPowerInputPinValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
    {
        inputPinValueChangesSubject.OnNext(pinValueChangedEventArgs);
    }
}
