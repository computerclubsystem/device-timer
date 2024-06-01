using System.Device.Gpio;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DeviceTimer.IOController;
using Iot.Device.Button;

class RemainingTimeChangedEventArgs : EventArgs
{
    public int RemainingSeconds { get; set; }
    public bool IsTimerOn { get; set; }
}

class TimerManager
{
    public event EventHandler<RemainingTimeChangedEventArgs> RemainingTimeChanged;

    private TimerManagerState _state;
    private IDisposable coinInputPinValueChangesSubscription;
    private Subjects subjects;
    private readonly TimeSpan coinInputPinThrottleTimeSpan = TimeSpan.FromSeconds(1);

    private readonly object _lockObject = new object();
    private Task timerTask;

    public TimerManager(RaspberryPi5Gpio gpio, int inputPinNumber, int outputPinNumber)
    {
        _state = new TimerManagerState()
        {
            GpioController = gpio,
            InputPinNumber = inputPinNumber,
            OutputPinNumber = outputPinNumber,
            CoinTimeSeconds = 10,
            RemainingSeconds = 0,
            TimerTaskEnabled = true,
        };
        subjects = new Subjects()
        {
            CoinInputPinValueChangesSubject = new(),
            CoinInputPinValueChangeDetectedSubject = new(),
        };
        subjects.CoinInputPinValueChangesObservable = subjects.CoinInputPinValueChangesSubject.AsObservable();
    }

    public void StartMonitoring()
    {
        _state.GpioController.ConfigurePinAsInput(_state.InputPinNumber, PinMode.InputPullDown);
        _state.GpioController.ConfigurePinAsOutput(_state.OutputPinNumber);
        coinInputPinValueChangesSubscription = subjects.CoinInputPinValueChangesObservable
            .Throttle(coinInputPinThrottleTimeSpan)
            .Subscribe(OnCoinInputPinChangeDetected);
        _state.GpioController.RegisterCallbackForPinValueChangedEvent(
            _state.InputPinNumber,
            PinEventTypes.Rising | PinEventTypes.Falling,
            OnCoinInputPinValueChanged
        );
        timerTask = Task.Factory.StartNew(TimerTask, CancellationToken.None);
    }

    private void OnCoinInputPinValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
    {
        subjects.CoinInputPinValueChangesSubject.OnNext(pinValueChangedEventArgs);
    }

    private void OnCoinInputPinChangeDetected(PinValueChangedEventArgs args)
    {
        Log("Coin input pin {0}", args.ChangeType);
        // subjects.PowerOnInputPinValueChangeDetectedSubject.OnNext(args.ChangeType);
        if (args.ChangeType == PinEventTypes.Falling)
        {
            lock (_state)
            {
                if (!_state.IsTimerOn)
                {
                    _state.StartedAt = DateTime.Now;
                    _state.IsTimerOn = true;
                    _state.CoinsIn = 1;
                }
                else
                {
                    _state.CoinsIn += 1;
                }
                _state.TotalCoinsInCount++;
            }
            LogState();
            // SetTryPowerOnOutputPinValue(PinValue.Low);
        }
    }

    public void StopMonitoring()
    {
        _state.TimerTaskEnabled = false;
    }

    private async void TimerTask()
    {
        while (_state.TimerTaskEnabled)
        {
            int remainingSeconds = 0;
            var now = DateTime.Now;
            lock (_state)
            {
                if (_state.IsTimerOn)
                {
                    var shouldStopAt = _state.StartedAt?.AddSeconds(_state.CoinsIn * _state.CoinTimeSeconds);
                    var diff = shouldStopAt - now;
                    remainingSeconds = (int)diff?.TotalSeconds;
                }
                if (remainingSeconds == null || remainingSeconds < 0)
                {
                    remainingSeconds = 0;
                    _state.CoinsIn = 0;
                    _state.IsTimerOn = false;
                    RaiseRemainingSeconds(0, false);
                }
                else if (remainingSeconds >= 0)
                {
                    RaiseRemainingSeconds(remainingSeconds, _state.IsTimerOn);
                }
                _state.RemainingSeconds = remainingSeconds;
                _state.GpioController.SetOutputPinValue(_state.OutputPinNumber, _state.IsTimerOn);
                Console.WriteLine("Time started {0} at {1}, coins in {2}, remaining seconds {3}", _state.IsTimerOn, _state.StartedAt, _state.CoinsIn, _state.RemainingSeconds);
            }
            await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
        }
    }

    private void RaiseRemainingSeconds(int seconds, bool isTimerOn)
    {
        RemainingTimeChanged?.Invoke(this, new RemainingTimeChangedEventArgs() { RemainingSeconds = seconds, IsTimerOn = isTimerOn });
    }

    // private void StartTime()
    // {
    //     lock (_lockObject)
    //     {
    //         _state.IsTimerOn = true;
    //         _state.CoinsIn = 1;
    //         _state.StartedAt = DateTime.Now;
    //     }
    // }

    // private void AddCoin()
    // {
    //     lock (_lockObject)
    //     {
    //         _state.CoinsIn++;
    //     }
    // }

    private void LogState()
    {
        var text = string.Format($"TotalCoinsInCount={_state.TotalCoinsInCount}");
        Log(text);
    }

    private void Log(string format, params object[] arg)
    {
        var now = DateTime.Now.ToString("O");
        Console.WriteLine($"{now} - TimerManager - {format}", arg);
    }

    private class TimerManagerState
    {
        public int InputPinNumber { get; set; }
        public int OutputPinNumber { get; set; }
        public RaspberryPi5Gpio GpioController { get; set; }
        // public GpioButton InputButton { get; set; }
        public int RemainingSeconds { get; set; }
        public int CoinsIn { get; set; }
        public int TotalCoinsInCount { get; set; }
        public int CoinTimeSeconds { get; set; }
        public bool IsTimerOn { get; set; }
        public DateTime? StartedAt { get; set; }
        public bool TimerTaskEnabled { get; set; }
    }

    private class Subjects
    {
        public Subject<PinValueChangedEventArgs> CoinInputPinValueChangesSubject { get; set; }
        public IObservable<PinValueChangedEventArgs> CoinInputPinValueChangesObservable { get; set; }
        public Subject<PinEventTypes> CoinInputPinValueChangeDetectedSubject { get; set; }
    }
}
