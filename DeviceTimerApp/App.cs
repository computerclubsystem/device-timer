using System.Device.Gpio;
using DeviceTimer.IOController;

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
        _psPower = new PlayStationPower(_gpio, _state.StartPlayStationPowerInputPin, _state.StartPlayStationPowerOutputPin);
        // TODO: Start WebSocket
        StartGpioFlow();
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, _state.CancellationToken);
        }
        catch { }
        CleanUp();
    }

    private void StartGpioFlow()
    {
        _psPower.StartMonitoring();
    }

    private void ConfigurePins()
    {
        _state.StartPlayStationPowerOutputPin = _gpio.ConfigurePinAsOutput(_state.StartPlayStationPowerOutputPinNumber);
        _state.StartPlayStationPowerOutputPin.Write(PinValue.Low);
        _state.StartPlayStationPowerInputPin = _gpio.ConfigurePinAsInput(_state.StartPlayStationPowerInputPinNumber, PinMode.InputPullDown);
    }

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
        _psPower.Dispose();
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
