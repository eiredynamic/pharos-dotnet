using Newtonsoft.Json;
using NLog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eiredynamic.Pharos
{
    internal interface IBeacon<in T> where T : class
    {
        Task SendBeacon(CancellationToken cancellationToken, T item);
    }
    public class Beacon<T> : IBeacon<T> where T : class
    {
        private readonly static Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ConfigOptions _config;

        public Beacon() { 
            _config = new ConfigOptions();
        }

        public Beacon(ConfigOptions config)
        {
            _config = config;
        }
        public async Task SendBeacon(CancellationToken cancellationToken, T item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            byte[] buffer = Serialize(item);
            await SendBeaconLoop(cancellationToken, () => buffer);
        }

        public async Task SendBeacon(CancellationToken cancellationToken, Func<T> getItem)
        {
            await SendBeaconLoop(cancellationToken, () =>
            {
                T item = getItem();
                if (item is null)
                {
                    throw new ArgumentNullException(nameof(getItem));
                }
                return Serialize(item);
            });
        }

        private byte[] Serialize(T item)
        {
            byte[] buffer;
            try
            {
                buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
            }
            catch (JsonSerializationException ex)
            {
                _logger.Error(ex, $"Serialization failed for type {typeof(T).Name}. Check that all properties are serializable and properly decorated with [JsonProperty] if needed.");
                throw new InvalidOperationException($"Beacon serialization failed for type {typeof(T).Name}.", ex);
            }

            if (buffer.Length > 1400)
                _logger.Warn($"Beacon payload for {typeof(T).Name} may exceed safe UDP limits ({buffer.Length} bytes)");

            return buffer;
        }

        private async Task SendBeaconLoop(CancellationToken cancellationToken, Func<byte[]> getBuffer)
        {
            using (UdpClient sender = new UdpClient(_config.SourcePort))
            {
                sender.AllowNatTraversal(true);
                IPEndPoint _multicastEndpoint = new IPEndPoint(_config.MulticastIP, _config.DestinationPort);
                _logger.Info($"Starting beacon to send to {_config.MulticastIP}:{_config.DestinationPort}");

                while (!cancellationToken.IsCancellationRequested)
                {
#pragma warning disable S2139 // Re-throwing caught exception
                    byte[] buffer = getBuffer();
                    try
                    {
                        await sender.SendAsync(buffer, buffer.Length, _multicastEndpoint);
                        _logger.Trace($"Sent beacon of type {typeof(T).Name}");
                        await Task.Delay(_config.BeaconInterval, cancellationToken);
                    }
                    catch (SocketException ex)
                    {
                        _logger.Error(ex, "Failed to send beacon. Network may be unreachable or multicast misconfigured.");
                        break;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.Error(ex, "Unexpected error during beacon send.");
                        throw;
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
#pragma warning restore S2139
                }
                _logger.Info("Cancellation requested. Beacon stopped.");
            }
        }
    }

    public class DummyBeacon<T>: IBeacon<T> where T : class
    {
        public Task SendBeacon(CancellationToken cancellationToken, T item)
        {
            throw new NotImplementedException();
        }
    }
}
