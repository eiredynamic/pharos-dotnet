using System.Net;

namespace Eiredynamic.Pharos.Tests
{
    public class PharosBeaconTests: IDisposable
    {
        private readonly ConfigOptions _config;
        private readonly CancellationTokenSource _cts;
        private bool disposedValue;

        public PharosBeaconTests()
        {
            _config = new ConfigOptions
            {
                MulticastIP = IPAddress.Parse("239.0.0.1"),
                SourcePort = 11000,
                DestinationPort = 11001,
                BeaconInterval = 100
            };
            _cts = new CancellationTokenSource();
        }

        private class TestMessage
        {
            public required string Content { get; set; }
            public override string ToString() => Content;
        }

        [Fact]
        public void DefaultConstructor_CreatesNewConfigOptions()
        {
            // Act
            var beacon = new Beacon<TestMessage>();

            // Assert
            Assert.NotNull(beacon);
        }

        [Fact]
        public void Constructor_WithConfig_SetsConfig()
        {
            // Act
            var beacon = new Beacon<TestMessage>(_config);

            // Assert
            Assert.NotNull(beacon);
            Assert.Equal(_config.MulticastIP, beacon.config.MulticastIP);
        }

        [Fact]
        public void SendBeacon_CancellationRequested_StopsBeacon()
        {
            // Arrange
            var beacon = new Beacon<TestMessage>(_config);
            var message = new TestMessage { Content = "Test" };
            _cts.CancelAfter(150); // Cancel after ~1 beacon

            // Act & Assert
            Assert.ThrowsAsync<OperationCanceledException>(() =>
                Record.ExceptionAsync(() => beacon.SendBeacon(_cts.Token, message)));
        }

        [Fact]
        public void SendBeacon_NullMessage_ThrowsArgumentNullException()
        {
            // Arrange
            var beacon = new Beacon<TestMessage>(_config);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() =>
                beacon.SendBeacon(_cts.Token, () => null!));
        }

        [Fact]
        public void DummyBeacon_SendBeacon_ThrowsNotImplementedException()
        {
            // Arrange
            var dummyBeacon = new DummyBeacon<TestMessage>();
            var message = new TestMessage { Content = "Test" };

            // Act & Assert
            Assert.ThrowsAsync<NotImplementedException>(() =>
                dummyBeacon.SendBeacon(_cts.Token, message));
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
