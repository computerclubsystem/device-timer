using DeviceTimer.IOController;

namespace DeviceTimerApp;

public class App
{
    private AppState _state;
    private RaspberryPi5Gpio _gpio;

    public async Task Start()
    {
        _state = CreateAppState();
        _gpio = CreateRaspberryPi5Gpio();
        ConfigurePins();
        SetStartPlayStationPinValue(false);
        // TODO: Start the flow and wait for the cancellation token
    }

    private void SetStartPlayStationPinValue(bool value)
    {
        _gpio.SetOutputPinValue(_state.StartPlayStationPin, value);
    }

    private void ConfigurePins()
    {
        _gpio.ConfigurePinAsOutput(_state.StartPlayStationPin);
    }

    private RaspberryPi5Gpio CreateRaspberryPi5Gpio()
    {
        RaspberryPi5Gpio rpi5Gpio = new();
        rpi5Gpio.Init();
        return rpi5Gpio;
    }
    private AppState CreateAppState()
    {
        AppState state = new()
        {
            CancellationTokenSource = new CancellationTokenSource(),
            StartPlayStationPin = 23,
        };
        state.CancellationToken = state.CancellationTokenSource.Token;
        return state;
    }

    private class AppState
    {
        public required int StartPlayStationPin { get; set; }
        public required CancellationTokenSource CancellationTokenSource { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}
