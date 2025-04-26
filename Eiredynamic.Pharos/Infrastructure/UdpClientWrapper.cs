using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Eiredynamic.Pharos.Infrastructure;

public class UdpClientWrapper : IUdpClient
{
    private readonly UdpClient _udpClient;

    public UdpClientWrapper(UdpClient udpClient)
    {
        _udpClient = udpClient;
    }

    public Task<UdpReceiveResult> ReceiveAsync()
    {
        return _udpClient.ReceiveAsync();
    }

    public void AllowNatTraversal(bool enabled)
    {
        _udpClient.AllowNatTraversal(enabled);
    }

    public bool ExclusiveAddressUse
    {
        get => _udpClient.ExclusiveAddressUse;
        set => _udpClient.ExclusiveAddressUse = value;
    }

    public void SetSocketOption(SocketOptionLevel level, SocketOptionName name, bool value)
    {
        _udpClient.Client.SetSocketOption(level, name, value);
    }

    public void Bind(IPEndPoint localEP)
    {
        _udpClient.Client.Bind(localEP);
    }

    public void JoinMulticastGroup(IPAddress multicastAddress)
    {
        _udpClient.JoinMulticastGroup(multicastAddress);
    }

    public void DropMulticastGroup(IPAddress multicastAddress)
    {
        _udpClient.DropMulticastGroup(multicastAddress);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        _udpClient.Dispose();
    }

}
