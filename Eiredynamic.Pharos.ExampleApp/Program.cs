using Eiredynamic.Pharos.Models;
using NLog;

namespace Eiredynamic.Pharos.ExampleApp
{
    internal static class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static ConfigOptions _config = new();
        static async Task Main(string[] args)
        {
            _logger.Info("Starting Pharos Example App!");
            using var _cts = new CancellationTokenSource();
            _cts.CancelAfter(5000);
            
            List<Task> tasks = [Probe(_cts), Beacon(_cts)];
            await Task.WhenAll(tasks);

            // This is a blocking call to keep the console window open
            Console.ReadLine();
        }

        async static Task Beacon(CancellationTokenSource cts)
        {
            _logger.Info("Starting Beacon!");
            
            var beacon = new Beacon<PharosSampleMessage>();
            var message = new PharosSampleMessage(_config.HostId);
            await beacon.SendBeacon(cts.Token, message);
        }

        async static Task Probe(CancellationTokenSource cts)
        {
            // Dictionary to store the hosts that have sent messages
            Dictionary<Guid, Tuple<string, DateTime>> hosts = new();
            // List to store the messages received
            List<PharosSampleMessage> messages = [];
            _logger.Info("Starting Probe!");
            var probe = new Probe<PharosSampleMessage>();
            await foreach (var pharosMessage in probe.StartReceiving(cts.Token))
            {
                messages.Add(pharosMessage);

                if (hosts.ContainsKey(pharosMessage.HostId))
                {
                    // Store the most recent message from the host
                    hosts[pharosMessage.HostId] = new Tuple<string, DateTime>(pharosMessage.Hostname, pharosMessage.Timestamp);
                }
                else
                {
                    // Add the host to the dictionary
                    hosts.Add(pharosMessage.HostId, new Tuple<string, DateTime>(pharosMessage.Hostname, pharosMessage.Timestamp));
                }

                _logger.Info(pharosMessage.Message);
                _logger.Debug($"Hosts count: {hosts.Count}");
                _logger.Debug($"Message count: {messages.Count}");   
            }
        }
    }
}
