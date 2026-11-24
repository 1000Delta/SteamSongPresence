using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteamSongPresence;

// TcpListener avoids HTTP.sys URL reservations / administrator privileges.
// Only loopback GET requests are accepted; no CORS and no mutation endpoints.
internal sealed class LoopbackServer : IDisposable
{
    private readonly TcpListener listener;
    private readonly Func<PresenceState> readState;
    private readonly CancellationTokenSource stop = new CancellationTokenSource();
    private readonly SemaphoreSlim slots = new SemaphoreSlim(4);
    private readonly Task worker;
    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;

    public LoopbackServer(Func<PresenceState> readState, int port = 6749)
    {
        this.readState = readState;
        listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start(8);
        worker = Task.Run(AcceptLoop);
    }

    private async Task AcceptLoop()
    {
        while (!stop.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await listener.AcceptTcpClientAsync().ConfigureAwait(false); }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) when (stop.IsCancellationRequested) { break; }
            if (!slots.Wait(0)) { client.Close(); continue; }
            _ = Task.Run(() => Serve(client));
        }
    }

    private void Serve(TcpClient client)
    {
        try
        {
            using (client)
            {
                client.ReceiveTimeout = 1500; client.SendTimeout = 1500;
                using (var stream = client.GetStream())
                {
                    var header = new StringBuilder();
                    while (header.Length < 8192)
                    {
                        int next = stream.ReadByte();
                        if (next < 0) return;
                        header.Append((char)next);
                        if (header.Length >= 4 && header.ToString(header.Length - 4, 4) == "\r\n\r\n") break;
                    }
                    var lines = header.ToString().Split(new[] { "\r\n" }, StringSplitOptions.None);
                    string[] first = lines[0].Split(' ');
                    string host = "";
                    int contentLength = 0;
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase)) host = line.Substring(5).Trim();
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                            if (!int.TryParse(line.Substring(15).Trim(), out contentLength) || contentLength < 0 || contentLength > 8192) return;
                    }
                    // Drain a bounded request body before closing, otherwise Windows
                    // can reset the connection before a 405 response reaches the caller.
                    for (int i = 0; i < contentLength; i++) if (stream.ReadByte() < 0) return;
                    // Block DNS rebinding and requests relayed through another host.
                    bool localHost = host == $"127.0.0.1:{Port}" || host == $"localhost:{Port}";
                    string code; string json;
                    if (!localHost) { code = "403 Forbidden"; json = "{\"error\":\"loopback host required\"}"; }
                    else if (first.Length != 3 || first[0] != "GET") { code = "405 Method Not Allowed"; json = "{\"error\":\"GET only\"}"; }
                    else if (first[1] != "/" && first[1] != "/status") { code = "404 Not Found"; json = "{\"error\":\"not found\"}"; }
                    else { code = "200 OK"; json = readState().ToJson(); }
                    byte[] body = Encoding.UTF8.GetBytes(json);
                    byte[] response = Encoding.ASCII.GetBytes($"HTTP/1.1 {code}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {body.Length}\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n");
                    stream.Write(response, 0, response.Length); stream.Write(body, 0, body.Length);
                }
            }
        }
        catch (IOException) { }
        catch (SocketException) { }
        catch (ObjectDisposedException) { }
        finally { slots.Release(); }
    }

    public void Dispose()
    {
        if (stop.IsCancellationRequested) return;
        stop.Cancel(); listener.Stop();
        try { worker.Wait(2000); } catch (AggregateException) { }
        // Request handlers can still be unwinding; do not dispose their semaphore.
    }
}
