using System.Device.Gpio;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DeviceTimer.IOController;

namespace DeviceTimer.DeviceTimerApp;

public class PlayStationPower
{
    private readonly RaspberryPi5Gpio gpio;
    private Pins pins;
    private Subjects subjects;
    private CancellationTokenSource tryPowerOnCancellationTokenSource;
    private Task powerOnTask;

    private readonly object lockObject = new();

    private IDisposable inputPinValueChangesSubscription;
    private readonly TimeSpan inputPinThrottleTimeSpan = TimeSpan.FromSeconds(1);

    public PlayStationPower(RaspberryPi5Gpio gpio, GpioPin powerOnInputPin, GpioPin powerOnOutputPin)
    {
        this.gpio = gpio;
        pins = new Pins()
        {
            PowerOnInputPin = powerOnInputPin,
            PowerOnOutputPin = powerOnOutputPin,
        };
        subjects = new Subjects()
        {
            PowerOnInputPinValueChangesSubject = new(),
            PowerOnInputPinValueChangeDetectedSubject = new(),
        };
        subjects.PowerOnInputPinValueChangesObservable = subjects.PowerOnInputPinValueChangesSubject.AsObservable();
    }

    public IObservable<PinEventTypes> GetPowerButtonChangeDetectedObservable()
    {
        return subjects.PowerOnInputPinValueChangeDetectedSubject.AsObservable();
    }

    public void StartMonitoring()
    {
        inputPinValueChangesSubscription = subjects.PowerOnInputPinValueChangesObservable
            .Throttle(inputPinThrottleTimeSpan)
            .Subscribe(OnPowerInputPinChangeDetected);
        gpio.RegisterCallbackForPinValueChangedEvent(
            pins.PowerOnInputPin.PinNumber,
            PinEventTypes.Rising | PinEventTypes.Falling,
            OnPlayStationPowerInputPinValueChanged
        );
        tryPowerOnCancellationTokenSource = new CancellationTokenSource();
        powerOnTask = Task.Factory.StartNew(TryPowerOnAction, tryPowerOnCancellationTokenSource.Token);
    }

    private void OnPowerInputPinChangeDetected(PinValueChangedEventArgs args)
    {
        Console.WriteLine("Power input pin {0}", args.ChangeType);
        subjects.PowerOnInputPinValueChangeDetectedSubject.OnNext(args.ChangeType);
        if (args.ChangeType == PinEventTypes.Rising)
        {
            SetTryPowerOnOutputPinValue(PinValue.Low);
        }
    }

    private void SetTryPowerOnOutputPinValue(PinValue pinValue)
    {
        LockAction(() => pins.PowerOnOutputPin.Write(pinValue));
    }

    private void LockAction(Action action)
    {
        lock (lockObject)
        {
            action();
        }
    }

    public void StopMonitoring()
    {
        gpio.UnregisterCallbackForPinValueChangedEvent(pins.PowerOnInputPin.PinNumber, OnPlayStationPowerInputPinValueChanged);
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
        var value = pins.PowerOnInputPin.Read();
        return value == PinValue.Low;
    }

    private void OnPlayStationPowerInputPinValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
    {
        subjects.PowerOnInputPinValueChangesSubject.OnNext(pinValueChangedEventArgs);
    }

    private class Pins
    {
        public GpioPin PowerOnInputPin { get; set; }
        public GpioPin PowerOnOutputPin { get; set; }
    }

    private class Subjects
    {
        public Subject<PinValueChangedEventArgs> PowerOnInputPinValueChangesSubject { get; set; }
        public IObservable<PinValueChangedEventArgs> PowerOnInputPinValueChangesObservable { get; set; }
        public Subject<PinEventTypes> PowerOnInputPinValueChangeDetectedSubject { get; set; }
    }
}
