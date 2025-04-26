using NLog;

namespace Eiredynamic.Pharos.ExampleApp
{
    internal static class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        static async Task Main(string[] args)
        {
            _logger.Info("Starting Pharos Example App!");
            using var _cts = new CancellationTokenSource();
            _cts.CancelAfter(5000);
            
            List<Task> tasks = [Probe(_cts), Beacon(_cts)];
            await Task.WhenAll(tasks);

            // This is a blocking call to keep the console window open
            Console.WriteLine("Press enter key to exit...");
            Console.ReadLine();
        }

        async static Task Beacon(CancellationTokenSource cts)
        {
            _logger.Info("Starting Beacon!");
            var message = "Hello Pharos";
            var beacon = new Beacon<string>();
            await beacon.SendBeacon(cts.Token, message);
        }

        async static Task Probe(CancellationTokenSource cts)
        {
            _logger.Info("Starting Probe!");
            var probe = new Probe<string>();
            await foreach (var pharosMessage in probe.StartReceiving(cts.Token))
            {
                _logger.Info(pharosMessage) ;
            }
        }
    }
}
