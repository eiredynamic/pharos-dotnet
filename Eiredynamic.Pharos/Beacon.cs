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
        private static Logger _logger = LogManager.GetCurrentClassLogger();
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
            // Create a UdpClient (local port can be dynamically assigned)
            using (UdpClient sender = new UdpClient(_config.SourcePort))
            {
                sender.AllowNatTraversal(true);
                IPEndPoint _multicastEndpoint = new IPEndPoint(_config.MulticastIP, _config.DestinationPort);
                _logger.Info($"Starting beacon to send to {_config.MulticastIP}:{_config.DestinationPort}");
                
                byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Send the multicast message
                    await sender.SendAsync(buffer, buffer.Length, _multicastEndpoint);
                    _logger.Trace($"Sent: {item.ToString()}");

                    // Wait for a period before sending the next beacon (e.g., 5 seconds)
                    Thread.Sleep(_config.BeaconInterval);
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Info("Cancellation requested. Beacon stopped.");
                }
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
