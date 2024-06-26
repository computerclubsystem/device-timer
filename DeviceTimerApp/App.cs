using System.Device.Gpio;
// using System.Reflection.Metadata;
// using System.Runtime.CompilerServices;
using DeviceTimer.IOController;
using DeviceTimer.Sensors;
using DeviceTimer.WebSocketConnector;
using DeviceTimer.CountdownDisplay;
using DeviceTimer.Tm1637Ported;
using System.Globalization;
// using Iot.Device.Common;
// using Microsoft.Extensions.Logging;

namespace DeviceTimer.DeviceTimerApp;

public class App
{
    private AppState _state;
    private RaspberryPi5Gpio _gpio;
    private PlayStationPower _psPower;
    private TimerManager _timerManager;
    private FourDigitDisplay _fourDigitsDisplay;
    private Tm1637PortedDisplay _tmPortedDisplay;
    private Temperatures _temperatures = new();

    public App(string[] args)
    {
        _state = CreateAppState(args);
    }

    public async Task Start()
    {
        _gpio = CreateRaspberryPi5Gpio();
        ConfigurePins();
        StartGpioModules();
        StartWebSocket();
        _timerManager.RemainingTimeChanged += OnRemainingTimeChanged;
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, _state.CancellationToken);
        }
        catch { }
        CleanUp();
    }

    private void StartTm1637PortedDisplay()
    {
        Tm1637PortedDisplayConfig config = new()
        {
            ClockPin = 5,
            DataPin = 6,
            ClockWidth = TimeSpan.FromMicroseconds(7),
            GpioController = _gpio.GetController(),
        };
        _tmPortedDisplay = new Tm1637PortedDisplay(config);
    }

    private void StartFourDigitDisplay()
    {
        // _fourDigitsDisplay = new FourDigitDisplay(_gpio.GetController(), _state.FourDigitsDisplayClockPinNumber, _state.FourDigitsDisplayDataPinNumber);
        // _fourDigitsDisplay.Test();
        // // // Thread.Sleep(500);
        // // // _fourDigitsDisplay.Init();
        // // // _fourDigitsDisplay.ClearDisplay();
        // // // _fourDigitsDisplay.SetScreenOnOffState(true);
        // // // _fourDigitsDisplay.ShowText("68:68");
        // // // _fourDigitsDisplay.ShowCharacterAtPosition(0, '5');
        // // // _fourDigitsDisplay.Dispose();
        // // _gpio.Dispose();
        // return;

        _fourDigitsDisplay = new FourDigitDisplay(_gpio.GetController(), _state.FourDigitsDisplayClockPinNumber, _state.FourDigitsDisplayDataPinNumber);
        _fourDigitsDisplay.Init();
        _fourDigitsDisplay.ClearDisplay();
        _fourDigitsDisplay.SetScreenOnOffState(true);
    }

    private void OnRemainingTimeChanged(object sender, RemainingTimeChangedEventArgs e)
    {
        _fourDigitsDisplay.ShowSecondsAsTime(e.RemainingSeconds);
        
        // // // _tmPortedDisplay.ShowSecondsAsTime(e.RemainingSeconds);
        // var ttt = e.RemainingSeconds.ToString(CultureInfo.InvariantCulture);
        // char lastChar = ttt[^1];
        // string hhh = new(lastChar, 4);
        // _fourDigitsDisplay.ShowText(hhh);
    }

    private void StartTemperatureMeasurement()
    {
        Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                double cpuTemp = _temperatures.GetCpuTemperature();
                Console.WriteLine("CPU temp {0}", cpuTemp);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        });
    }

    private void StartWebSocket()
    {
        _state.WSConnector = new WSConnector();
        _state.WSConnector.SetConnectedAction(OnWebSocketConnected);
        _state.WSConnector.SetDataReceivedAction(OnWebSocketDataReceived);
        _state.WSConnector.SetDisconnectedAction(OnWebSocketDisconnected);
        _state.WSConnector.SetExceptionAction(OnWebSocketException);
        WSConnectorSettings wsSettings = new()
        {
            Uri = new Uri(_state.EnvironmentValues.WebSocketUrl),
            // ClientCertificatePemFileText = File.ReadAllText("/etc/ssl/certs/device-timer.pem"),
            ClientCertificateCertFileText = File.ReadAllText("/etc/ssl/certs/device-timer.crt"),
            ClientCertificateKeyFileText = File.ReadAllText("/etc/ssl/certs/device-timer.key"),
            // 02C347A57731C65931D30D3D93298BDC610488A8
            ServerCertificateThumbnail = _state.EnvironmentValues.ServerCertificateThumbprint,
        };
        _state.WSConnector.Init(wsSettings);
        _state.WSConnector.Connect();
    }

    private void OnWebSocketDataReceived(byte[] data)
    {
        // Console.WriteLine("Data received {0}", data.Length);
    }

    private void OnWebSocketConnected()
    {
        _state.WSConnectorConnectionsCount++;
        // Console.WriteLine("Connected {0}", _state.WSConnectorConnectionsCount);
    }
    private void OnWebSocketDisconnected()
    {
        _state.WSConnectorDisconnectionsCount++;
        // Console.WriteLine("Disconnected {0}", _state.WSConnectorDisconnectionsCount);
    }
    private void OnWebSocketException(Exception ex)
    {
        _state.WSConnectorExceptionsCount++;
        // Console.WriteLine("Exception {0} {1}", _state.WSConnectorExceptionsCount, ex);
    }

    private void StartGpioModules()
    {
        StartPsPower();
        StartTimerManager();
        StartFourDigitDisplay();
        StartTemperatureMeasurement();
    }

    private void StartPsPower()
    {
        _psPower = new PlayStationPower(
            _gpio,
            _state.StartPlayStationPowerInputPin,
            _state.StartPlayStationPowerOutputPin
        );
        _psPower.StartMonitoring();
    }

    private void StartTimerManager()
    {
        _timerManager = new TimerManager(_gpio, _state.StartTimeOnInputPinNumber, _state.StartTimeOnOutputPinNumber);
        _timerManager.StartMonitoring();
    }

    private void ConfigurePins()
    {
        _state.StartPlayStationPowerOutputPin = _gpio.ConfigurePinAsOutput(_state.StartPlayStationPowerOutputPinNumber);
        _state.StartPlayStationPowerOutputPin.Write(PinValue.Low);
        _state.StartPlayStationPowerInputPin = _gpio.ConfigurePinAsInput(_state.StartPlayStationPowerInputPinNumber, PinMode.InputPullDown);
        // _state.StartTimeOnOutputPin = _gpio.ConfigurePinAsOutput(_state.StartTimeOnOutputPinNumber);
        // _state.StartTimeOnOutputPin.Write(PinValue.Low);
        // _state.AdditionalOutputPin = _gpio.ConfigurePinAsOutput(_state.AdditionalOutputPinNumber);
        // _state.AdditionalOutputPin.Write(PinValue.Low);
    }

    private RaspberryPi5Gpio CreateRaspberryPi5Gpio()
    {
        RaspberryPi5Gpio rpi5Gpio = new();
        rpi5Gpio.Init();
        return rpi5Gpio;
    }

    private AppState CreateAppState(string[] args)
    {
        var cts = new CancellationTokenSource();
        AppState state = new()
        {
            Args = args,
            CancellationTokenSource = cts,
            CancellationToken = cts.Token,
            StartPlayStationPowerInputPinNumber = 25,
            StartPlayStationPowerOutputPinNumber = 23,
            StartTimeOnInputPinNumber = 16,
            StartTimeOnOutputPinNumber = 22,
            FourDigitsDisplayClockPinNumber = 5,
            FourDigitsDisplayDataPinNumber = 6,
            EnvironmentValues = CreateEnvironmentValues(args),
        };
        state.CancellationToken = state.CancellationTokenSource.Token;
        return state;
    }

    private EnvironmentValues CreateEnvironmentValues(string[] args)
    {
        EnvironmentValues result = new();
        var cmdLineDict = CreateCommandLineValuesDictionary(args);
        result.WebSocketUrl = cmdLineDict.GetValueOrDefault("--web-socket-url") ?? Environment.GetEnvironmentVariable("DEVICE_TIMER_WEB_SOCKET_URL");
        result.ServerCertificateThumbprint = cmdLineDict.GetValueOrDefault("--server-certificate-thumbprint") ?? Environment.GetEnvironmentVariable("DEVICE_TIMER_SERVER_CERTIFICATE_THUMBPRINT");
        return result;
    }

    private Dictionary<string, string> CreateCommandLineValuesDictionary(string[] cmdLineArgs)
    {
        Dictionary<string, string> result = [];
        if (cmdLineArgs.Length == 0)
        {
            return result;
        }
        for (int i = 0; i < cmdLineArgs.Length - 1; i += 2)
        {
            result.Add(cmdLineArgs[i], cmdLineArgs[i + 1]);
        }
        return result;
    }

    private void CleanUp()
    {
        _psPower.Dispose();
        _fourDigitsDisplay.Dispose();
        _gpio.Dispose();
    }

    private class AppState
    {
        public required int StartPlayStationPowerInputPinNumber { get; set; }
        public GpioPin StartPlayStationPowerInputPin { get; set; }
        public required int StartPlayStationPowerOutputPinNumber { get; set; }
        public GpioPin StartPlayStationPowerOutputPin { get; set; }
        public required int StartTimeOnInputPinNumber { get; set; }
        public required int StartTimeOnOutputPinNumber { get; set; }
        public int FourDigitsDisplayClockPinNumber { get; set; }
        public int FourDigitsDisplayDataPinNumber { get; set; }
        // public required int AdditionalOutputPinNumber { get; set; }
        // public GpioPin AdditionalOutputPin { get; set; }
        public required CancellationTokenSource CancellationTokenSource { get; set; }
        public required CancellationToken CancellationToken { get; set; }
        public string[] Args { get; set; }
        public WSConnector WSConnector { get; set; }
        public int WSConnectorConnectionsCount { get; set; }
        public int WSConnectorDisconnectionsCount { get; set; }
        public int WSConnectorExceptionsCount { get; set; }
        public long WSConnectorBytesReceivedCount { get; set; }
        public long WSConnectorBytesSentCount { get; set; }
        public EnvironmentValues EnvironmentValues { get; set; }
    }

    private class EnvironmentValues
    {
        public string WebSocketUrl { get; set; }
        public string ServerCertificateThumbprint { get; set; }
    }
}
