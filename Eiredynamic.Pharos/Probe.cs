using Eiredynamic.Pharos.Infrastructure;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eiredynamic.Pharos;

internal interface IProbe<out T> where T : class
{
    public IAsyncEnumerable<T> StartReceiving(CancellationToken cancellationToken);
}

public class Probe<T>: IProbe<T> where T : class
{
    private static Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly ConfigOptions _config;
    private IUdpClient _udpClient;
    public event EventHandler<ProbeEventArgs<T>>? OnEvent;

    public Probe()
    {
        _config = new ConfigOptions();
    }

    public Probe(ConfigOptions config)
    {
        _config = config;
    }

    public Probe(IUdpClient udpClient)
    {
        _udpClient = udpClient;
    }

    public Probe(ConfigOptions config, IUdpClient udpClient)
    {
        _config = config;
        _udpClient = udpClient;
    }

    public async IAsyncEnumerable<T> StartReceiving([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in ReceiveMessages(cancellationToken))
        {
            yield return item;
        }
    }

    public async Task StartReceivingWithEventsOnly(CancellationToken cancellationToken)
    {
        await foreach (var item in ReceiveMessages(cancellationToken))
        {
            OnEvent?.Invoke(this, new ProbeEventArgs<T>(item));
        }
        _logger.Info("Event-only probe shut down.");
    }

    private async IAsyncEnumerable<T> ReceiveMessages([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_udpClient == null)
        {
            _udpClient = new UdpClientWrapper(new UdpClient());
        }

        using (_udpClient)
        {
            IPEndPoint _endPoint = new IPEndPoint(IPAddress.Any, _config.DestinationPort);
            _udpClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.AllowNatTraversal(true);
            _udpClient.ExclusiveAddressUse = false;
            _udpClient.Bind(_endPoint);

            _udpClient.JoinMulticastGroup(_config.MulticastIP);
            _logger.Info($"Listening for multicast messages on {_config.MulticastIP}:{_config.DestinationPort}");

            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    var receiveTask = _udpClient.ReceiveAsync();
                    var cancelTask = Task.Delay(Timeout.Infinite, cancellationToken);
                    var completedTask = await Task.WhenAny(receiveTask, cancelTask);
                    if (completedTask == cancelTask)
                    {
                        break;
                    }

                    result = await receiveTask;
                }
                catch (SocketException ex)
                {
                    _logger.Error(ex, "Socket exception during receive. Stopping probe.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unexpected error during beacon reception.");
                    throw;
                }

                if (result.Buffer == null || result.Buffer.Length == 0)
                {
                    _logger.Warn($"Received empty or null beacon from {result.RemoteEndPoint}. Ignored.");
                    continue;
                }

                string json = Encoding.UTF8.GetString(result.Buffer);
                _logger.Trace($"Received beacon from {result.RemoteEndPoint}");

                T obj = null;
                try
                {
                    obj = JsonConvert.DeserializeObject<T>(json);
                }
                catch (JsonException ex)
                {
                    _logger.Error(ex, $"Failed to deserialize beacon from {result.RemoteEndPoint}. Ignored bad packet.");
                }

                if (obj != null)
                {
                    yield return obj;
                }
                else
                {
                    _logger.Warn($"Deserialization returned null for beacon from {result.RemoteEndPoint}. Ignored.");
                }
            }

            try
            {
                _udpClient.DropMulticastGroup(_config.MulticastIP);
                _logger.Info("Left multicast group cleanly.");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to leave multicast group cleanly.");
            }
            _logger.Info("Cancellation requested. Probe stopped.");
        }
    }

}
