using System.Device.Gpio;
using DeviceTimer.IOController;
using DeviceTimer.WebSocketConnector;

namespace DeviceTimer.DeviceTimerApp;

public class App
{
    private AppState _state;
    private RaspberryPi5Gpio _gpio;
    private PlayStationPower _psPower;

    public App(string[] args)
    {
        _state = CreateAppState();
        _state.Args = args;
    }

    public async Task Start()
    {
        _gpio = CreateRaspberryPi5Gpio();
        ConfigurePins();
        _psPower = new PlayStationPower(
            _gpio,
            _state.StartPlayStationPowerInputPin,
            _state.StartPlayStationPowerOutputPin,
            _state.StartTimeOnOutputPin,
            _state.AdditionalOutputPin
        );
        StartWebSocket();
        StartGpioFlow();
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, _state.CancellationToken);
        }
        catch { }
        CleanUp();
    }

    private void StartWebSocket()
    {
        _state.WSConnector = new WSConnector();
        _state.WSConnector.SetConnectedAction(OnWebSocketConnected);
        _state.WSConnector.SetDataReceivedAction(OnWebSocketDataReceived);
        _state.WSConnector.SetDisconnectedAction(OnWebSocketDisconnected);
        _state.WSConnector.SetExceptionAction(OnWebSocketException);
        WSConnectorSettings wsSettings = new WSConnectorSettings()
        {
            Uri = new Uri("wss://192.168.1.9:65445"),
            // ClientCertificatePemFileText = File.ReadAllText("/etc/ssl/certs/device-timer.pem"),
            ClientCertificateCertFileText = File.ReadAllText("/etc/ssl/certs/device-timer.crt"),
            ClientCertificateKeyFileText = File.ReadAllText("/etc/ssl/certs/device-timer.key"),
            ServerCertificateThumbnail = _state.ServerCertificateThumbnail
        };
        _state.WSConnector.Init(wsSettings);
        _state.WSConnector.Connect();
    }

    private void OnWebSocketDataReceived(byte[] data)
    {
        Console.WriteLine("Data received {0}", data.Length);
    }

    private void OnWebSocketConnected()
    {
        _state.WSConnectorConnectionsCount++;
        Console.WriteLine("Connected {0}", _state.WSConnectorConnectionsCount);
    }
    private void OnWebSocketDisconnected()
    {
        _state.WSConnectorDisconnectionsCount++;
        Console.WriteLine("Disconnected {0}", _state.WSConnectorDisconnectionsCount);
    }
    private void OnWebSocketException(Exception ex)
    {
        _state.WSConnectorExceptionsCount++;
        Console.WriteLine("Exception {0} {1}", _state.WSConnectorExceptionsCount, ex);
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
        _state.StartTimeOnOutputPin = _gpio.ConfigurePinAsOutput(_state.StartTimeOnOutputPinNumber);
        _state.StartTimeOnOutputPin.Write(PinValue.Low);
        _state.AdditionalOutputPin = _gpio.ConfigurePinAsOutput(_state.AdditionalOutputPinNumber);
        _state.AdditionalOutputPin.Write(PinValue.Low);
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
            StartTimeOnOutputPinNumber = 22,
            AdditionalOutputPinNumber = 24,
        };
        state.CancellationToken = state.CancellationTokenSource.Token;
        // TODO: Get this from command line param / environment
        state.ServerCertificateThumbnail = "02C347A57731C65931D30D3D93298BDC610488A8";
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
        public required int StartTimeOnOutputPinNumber { get; set; }
        public GpioPin StartTimeOnOutputPin { get; set; }
        public required int AdditionalOutputPinNumber { get; set; }
        public GpioPin AdditionalOutputPin { get; set; }
        public required CancellationTokenSource CancellationTokenSource { get; set; }
        public required CancellationToken CancellationToken { get; set; }
        public string[] Args { get; set; }
        public WSConnector WSConnector { get; set; }
        public string ServerCertificateThumbnail { get; set; }
        public int WSConnectorConnectionsCount { get; set; }
        public int WSConnectorDisconnectionsCount { get; set; }
        public int WSConnectorExceptionsCount { get; set; }
        public long WSConnectorBytesReceivedCount { get; set; }
        public long WSConnectorBytesSentCount { get; set; }
    }
}
