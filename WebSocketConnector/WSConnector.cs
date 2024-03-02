// using System.Net.Security;
// using System.Net.WebSockets;
// using System.Text;

namespace DeviceTimer.WebSocketConnector;

public class WSConnector
{
    private WSConnectorState _state;

    public void Start(string uri)
    {
        _state = CreateState(uri);
        // ClientWebSocket ws = new ClientWebSocket();
        // ws.Options.RemoteCertificateValidationCallback = (object sender, System.Security.Cryptography.X509Certificates.X509C>// {
        //     return true;
        // };
        // await ws.ConnectAsync(new Uri("wss://192.168.1.9:65443"), cancellationToken);
        // var arrSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"header\":{},\"body\":{\"type\":\"operator-initial>
        // var wsSendTask = Task.Factory.StartNew(async () =>
        // {
        //     while (!cancellationToken.IsCancellationRequested)
        //     {
        //         await ws.SendAsync(arrSegment, WebSocketMessageType.Text, true, cancellationToken);
        //         Thread.Sleep(TimeSpan.FromSeconds(5));
        //     }
        // }, cancellationToken);
    }

    private WSConnectorState CreateState(string uri)
    {
        return new WSConnectorState()
        {
            Config = new WSConnectorConfig()
            {
                Uri = uri
            }
        };
    }

    private class WSConnectorState
    {
        public required WSConnectorConfig Config { get; set; }
    }
}

