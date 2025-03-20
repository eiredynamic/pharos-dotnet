using Eiredynamic.Pharos.Models;
using NSubstitute;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Eiredynamic.Pharos.Infrastructure;

namespace Eiredynamic.Pharos.Tests
{
    public class PharosProbeTests: IDisposable
    {
        private readonly UdpClient _mockUdpClient;
        private readonly ConfigOptions _configOptions;
        private readonly Probe<PharosSampleMessage> _probe;
        private readonly CancellationTokenSource _cts;
        private bool disposedValue;

        public PharosProbeTests()
        {
            _mockUdpClient = Substitute.For<UdpClient>();
            _configOptions = new ConfigOptions
            {
                DestinationPort = 12345,
                MulticastIP = IPAddress.Parse("239.0.0.1")
            };
            _probe = new Probe<PharosSampleMessage>(_configOptions);
            _cts = new CancellationTokenSource();
        }

        [Fact]
        public async Task StartReceiving_ShouldReceiveMessages()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;
            var expectedMessage = new PharosSampleMessage { /* Initialize properties */ };
            var jsonMessage = JsonConvert.SerializeObject(expectedMessage);
            var receiveResult = new UdpReceiveResult(Encoding.UTF8.GetBytes(jsonMessage), new IPEndPoint(IPAddress.Any, 0));

            var mockUdpClient = Substitute.For<IUdpClient>();
            mockUdpClient.ReceiveAsync().Returns(Task.FromResult(receiveResult));
            mockUdpClient.ExclusiveAddressUse = false;

            // Add these to avoid null reference errors (not sure these are needed):
            mockUdpClient.When(x => x.SetSocketOption(Arg.Any<SocketOptionLevel>(), Arg.Any<SocketOptionName>(), Arg.Any<bool>())).Do(_ => { });
            mockUdpClient.When(x => x.Bind(Arg.Any<IPEndPoint>())).Do(_ => { });
            mockUdpClient.When(x => x.JoinMulticastGroup(Arg.Any<IPAddress>())).Do(_ => { });

            var probe = new Probe<PharosSampleMessage>(new ConfigOptions(), mockUdpClient);

            // Act
            var receivedMessages = new List<PharosSampleMessage>();
            await foreach (var message in probe.StartReceiving(cancellationToken))
            {
                receivedMessages.Add(message);
                if (receivedMessages.Count == 1) break; // Stop after receiving one message for the test
            }

            // Assert
            Assert.Single(receivedMessages);
            Assert.Equivalent(expectedMessage, receivedMessages[0]);
        }

        [Fact]
        public void StartReceiving_ShouldHandleCancellation()
        {
            // Arrange
            var cancellationToken = _cts.Token;
            _cts.CancelAfter(150); // Cancel quickly


            // Act & Assert
            Assert.ThrowsAsync<OperationCanceledException>(() =>
                    (Task)_probe.StartReceiving(cancellationToken));

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cts.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
