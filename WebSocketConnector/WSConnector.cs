// using System.Net.Security;
// using System.Net.WebSockets;
// using System.Text;

using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DeviceTimer.WebSocketConnector;

public class ReconnectingWebSocket
{
    private ReconnectingWebSocketState state;
    private Action connectedAction;
    private Action disconnectedAction;
    private Action<Exception> exceptionAction;
    private Action<byte[]> dataReceivedAction;

    public void Init(ReconnectingWebSocketSettings settings)
    {
        state = new ReconnectingWebSocketState
        {
            Settings = settings,
            CancellationTokenSource = new CancellationTokenSource()
        };
        state.CancellationToken = state.CancellationTokenSource.Token;
    }

    public void SetConnectedAction(Action action)
    {
        connectedAction = action;
    }

    public void SetExceptionAction(Action<Exception> action)
    {
        exceptionAction = action;
    }

    public void SetDisconnectedAction(Action action)
    {
        disconnectedAction = action;
    }

    public void SetDataReceivedAction(Action<byte[]> action)
    {
        dataReceivedAction = action;
    }

    public void Connect()
    {
        ConnectWebSocket();
    }

    private async void ConnectWebSocket()
    {
        while (true)
        {
            try
            {
                if (state.WebSocket is not null)
                {
                    try
                    {
                        state.WebSocket.Abort();
                        state.WebSocket.Dispose();
                    }
                    catch { }
                }
                state.WebSocket = new ClientWebSocket();
                SetupCertificates();
                //Log("Connecting to " + uriString);
                await state.WebSocket.ConnectAsync(state.Settings.Uri, state.CancellationToken);
                //Log("Connected");
                connectedAction();
                break;
            }
            catch (Exception ex)
            {
                exceptionAction(ex);
                //Log("Cannot connect. " + ex.ToString());
            }
        }
        StartReceiving();
    }
    private async void StartReceiving()
    {
        //List<byte> message = new List<byte>();
        byte[] buffer = new byte[1 * 1024 * 1024];
        while (true)
        {
            Memory<byte> memory = new(buffer);
            try
            {
                var result = await state.WebSocket.ReceiveAsync(memory, state.CancellationToken);
                //Log("Received " + result.Count + " bytes. End of message: " + result.EndOfMessage + ". Message type: " + result.MessageType);
                if (result.Count > 0 && result.MessageType != WebSocketMessageType.Close)
                {
                    if (result.EndOfMessage)
                    {
                        byte[] data = memory[..result.Count].ToArray();
                        dataReceivedAction(data);
                        //string stringData = Encoding.UTF8.GetString(data);
                        //Log("Received: " + stringData);
                        //Message<object> msg = Deserialize(stringData);
                        //string msgType = msg.Header.Type;
                        //if (msgType == MessageType.DeviceSetStatus) {
                        //    ProcessDeviceSetStatusMessage(msg);
                        //}

                        //                        if (stringData.IndexOf("switch-to-default-desktop") >= 0)
                        //                        {
                        //                            Log("Switching to default desktop");
                        //#if !CCS3_NO_DESKTOP_SWITCH
                        //                            dm.SwitchToDesktop(defaultDesktopPtr);
                        //#endif
                        //                        }
                        //                        else if (stringData.IndexOf("switch-to-secured-desktop") >= 0)
                        //                        {
                        //                            Log("Switching to secured desktop");
                        //#if !CCS3_NO_DESKTOP_SWITCH
                        //                            dm.SwitchToDesktop(securedDesktopPtr);
                        //#endif
                        //                        }
                    }
                    else
                    {
                        // TODO: Collect bytes until the whole message is received
                        //message.AddRange(memory.Slice(0, result.Count).ToArray());
                    }
                }
                else
                {
                    // Socket was closed - reconnect
                    //Log("The socket has been closed, reconnecting");
                    disconnectedAction();
                    ConnectWebSocket();
                    break;
                }
            }
            catch (Exception ex)
            {
                //Log("Error on receiving: " + ex.ToString());
                exceptionAction(ex);
                ConnectWebSocket();
                break;
            }
        }
    }

    private void SetupCertificates()
    {
        X509Certificate2 cert = X509Certificate2.CreateFromPem(
            state.Settings.ClientCertificateCertFileText.AsSpan(),
            state.Settings.ClientCertificateKeyFileText.AsSpan()
        );
        state.WebSocket.Options.ClientCertificates = new X509Certificate2Collection(cert);
        state.WebSocket.Options.RemoteCertificateValidationCallback = (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) =>
        {
            if (certificate is null)
            {
                return false;
            }
            string certThumbprint = certificate.GetCertHashString();
            if (string.Compare(certThumbprint, state.Settings.ServerCertificateThumbnail, false) != 0)
            {
                return false;
            }
            return true;
        };
    }

    private class ReconnectingWebSocketState
    {
        public ReconnectingWebSocketSettings Settings { get; set; }
        public ClientWebSocket WebSocket { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}

public class ReconnectingWebSocketSettings
{
    public Uri Uri { get; set; }
    public string ClientCertificateCertFileText { get; set; }
    public string ClientCertificateKeyFileText { get; set; }
    public string ServerCertificateThumbnail { get; set; }
}