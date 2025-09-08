using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace Eiredynamic.Pharos.Infrastructure;

public interface IUdpClient : IDisposable
{
    Task<UdpReceiveResult> ReceiveAsync();
    void AllowNatTraversal(bool enabled);
    bool ExclusiveAddressUse { get; set; }
    void JoinMulticastGroup(IPAddress multicastAddress);
    void DropMulticastGroup(IPAddress multicastAddress);
    // can't access UdpClient.Client in Facade (well, its actually null), so instead of accessing UdpClient.Client, move socket-related methods directly into IUdpClient.
    void SetSocketOption(SocketOptionLevel level, SocketOptionName name, bool value);
    void Bind(IPEndPoint localEP);
    Task SendAsync(byte[] datagram, int bytes, IPEndPoint endPoint);
}

