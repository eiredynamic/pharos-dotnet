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

            // Join the multicast group on the local network interface
            _udpClient.JoinMulticastGroup(_config.MulticastIP);

            _logger.Info($"Listening for multicast messages on {_config.MulticastIP}:{_config.DestinationPort}");
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for a multicast message to arrive but allow cancellation so doesn't block indefinitely
                var receiveTask = _udpClient.ReceiveAsync();
                var cancelTask = Task.Delay(Timeout.Infinite, cancellationToken); // Will complete if canceled
                var completedTask = await Task.WhenAny(receiveTask, cancelTask);

                if (completedTask == cancelTask)
                {
                    // Cancellation was requested, so exit loop
                    break;
                }
                // We know receiveTask is completed because we checked it above
                UdpReceiveResult result = await receiveTask;
                var json = Encoding.UTF8.GetString(result.Buffer);
                _logger.Trace($"Received beacon from {result.RemoteEndPoint}");
                yield return JsonConvert.DeserializeObject<T>(json);
            }
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Info("Cancellation requested. Probe stopped.");
            }
        }
    }
}
