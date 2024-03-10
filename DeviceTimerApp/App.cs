using System.Device.Gpio;
using DeviceTimer.IOController;
using DeviceTimerApp;

namespace DeviceTimer.DeviceTimerApp;

public class App
{
    private AppState _state;
    private RaspberryPi5Gpio _gpio;
    private PlayStationPower _psPower;

    public async Task Start()
    {
        _state = CreateAppState();
        _gpio = CreateRaspberryPi5Gpio();
        ConfigurePins();
        // RegisterPinValueChangedCallbacks();
        _psPower = new PlayStationPower(_gpio, _state.StartPlayStationPowerInputPin, _state.StartPlayStationPowerOutputPin);
        // _psPower.StartMonitoring();
        // SetStartPlayStationPowerOutputPinValue(false);
        // TODO: Start WebSocket
        // TODO: Start the GPIO flow and wait for the cancellation token
        StartGpioFlow();
        await Task.Delay(Timeout.InfiniteTimeSpan, _state.CancellationToken);
        CleanUp();
    }

    private async void StartGpioFlow()
    {
        _psPower.StartMonitoring();
        // await Task.Factory.StartNew(async () =>
        // {
        //     string state = "power-up-playstation";
        //     while (!_state.CancellationToken.IsCancellationRequested)
        //     {
        //         if (state == "power-up-playstation")
        //         {
        //             _gpio.SetOutputPinValue(_state.StartPlayStationPowerOutputPinNumber, false);
        //             await Task.Delay(TimeSpan.FromSeconds(5), _state.CancellationToken);
        //             _gpio.SetOutputPinValue(_state.StartPlayStationPowerOutputPinNumber, true);
        //             await Task.Delay(TimeSpan.FromSeconds(1), _state.CancellationToken);
        //         }
        //     }
        // }, _state.CancellationToken);
    }


    // private void SetStartPlayStationPowerOutputPinValue(bool value)
    // {
    //     _gpio.SetOutputPinValue(_state.StartPlayStationPowerOutputPinNumber, value);
    // }

    private void ConfigurePins()
    {
        _state.StartPlayStationPowerOutputPin = _gpio.ConfigurePinAsOutput(_state.StartPlayStationPowerOutputPinNumber);
        _state.StartPlayStationPowerOutputPin.Write(PinValue.Low);
        _state.StartPlayStationPowerInputPin = _gpio.ConfigurePinAsInput(_state.StartPlayStationPowerInputPinNumber, PinMode.InputPullDown);
    }

    // private void RegisterPinValueChangedCallbacks()
    // {
    //     _gpio.RegisterCallbackForPinValueChangedEvent(
    //         _state.StartPlayStationPowerInputPinNumber,
    //          PinEventTypes.Rising,
    //          OnPlayStationPowerInputPinValueChanged
    //     );
    // }

    // private void OnPlayStationPowerInputPinValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
    // {
    //     Console.WriteLine("{0} : PlayStationPowerInputPin value {1}", DateTime.Now.ToString("s"), pinValueChangedEventArgs.ChangeType);
    // }

    private RaspberryPi5Gpio CreateRaspberryPi5Gpio()
    {
        RaspberryPi5Gpio rpi5Gpio = new();
        rpi5Gpio.Init();
        return rpi5Gpio;
    }
    private AppState CreateAppState()
    {

        var cts = new CancellationTokenSource();
        AppState state = new()
        {
            CancellationTokenSource = cts,
            CancellationToken = cts.Token,
            StartPlayStationPowerInputPinNumber = 25,
            StartPlayStationPowerOutputPinNumber = 23,
        };
        state.CancellationToken = state.CancellationTokenSource.Token;
        return state;
    }

    private void CleanUp()
    {
        _gpio.Dispose();
    }

    private class AppState
    {
        public required int StartPlayStationPowerInputPinNumber { get; set; }
        public GpioPin StartPlayStationPowerInputPin { get; set; }
        public required int StartPlayStationPowerOutputPinNumber { get; set; }
        public GpioPin StartPlayStationPowerOutputPin { get; set; }
        public required CancellationTokenSource CancellationTokenSource { get; set; }
        public required CancellationToken CancellationToken { get; set; }
    }
}
