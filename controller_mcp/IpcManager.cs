using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace controller_mcp
{
    public static class IpcManager
    {
        private const string PipeName = "ControllerMcpIPC_Daemon";
        private static CancellationTokenSource _serverCts;
        private static ConcurrentDictionary<Guid, StreamWriter> _clients = new ConcurrentDictionary<Guid, StreamWriter>();
        private static StreamWriter _clientWriter;

        public static void StartServer(Action<string> onCommandReceived)
        {
            _serverCts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!_serverCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                        await pipeServer.WaitForConnectionAsync(_serverCts.Token);
                        
                        var writer = new StreamWriter(pipeServer) { AutoFlush = true };
                        var clientId = Guid.NewGuid();
                        _clients.TryAdd(clientId, writer);

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var reader = new StreamReader(pipeServer);
                                while (!reader.EndOfStream)
                                {
                                    string line = await reader.ReadLineAsync();
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        onCommandReceived?.Invoke(line);
                                    }
                                }
                            }
                            catch
                            {
                                await Task.Delay(2000);
                            }
                            finally
                            {
                                _clients.TryRemove(clientId, out _);
                                pipeServer.Dispose();
                            }
                        });
                    }
                    catch
                    {
                        try { await Task.Delay(1000, _serverCts.Token); } catch { }
                    }
                }
            });
        }

        public static void BroadcastLog(string message)
        {
            foreach (var kvp in _clients)
            {
                try
                {
                    kvp.Value.WriteLine($"LOG:{message}");
                }
                catch 
                { 
                    // Remove disconnected client
                    _clients.TryRemove(kvp.Key, out _);
                }
            }
        }

        public static bool TryConnectClient(Action<string> onLogReceived)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        using (var clientStream = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
                        {
                            await clientStream.ConnectAsync(1000);
                            clientStream.ReadMode = PipeTransmissionMode.Message;
                            _clientWriter = new StreamWriter(clientStream) { AutoFlush = true };

                            var reader = new StreamReader(clientStream);
                            while (!reader.EndOfStream)
                            {
                                string line = await reader.ReadLineAsync();
                                if (line != null && line.StartsWith("LOG:"))
                                {
                                    onLogReceived?.Invoke(line.Substring(4));
                                }
                            }
                        }
                    }
                    catch { } // Disconnected or connection refused
                    
                    _clientWriter = null;
                    await Task.Delay(2000); // Wait 2 seconds before attempting reconnect
                }
            });
            return true;
        }

        public static void SendCommand(string command)
        {
            try
            {
                _clientWriter?.WriteLine(command);
            }
            catch { }
        }
    }
}
