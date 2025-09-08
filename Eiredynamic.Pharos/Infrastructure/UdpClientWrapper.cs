using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NLog;

namespace Eiredynamic.Pharos.Infrastructure;

public class UdpClientWrapper : IUdpClient
{
    private readonly UdpClient _udpClient;
    private readonly static Logger _logger = LogManager.GetCurrentClassLogger();
    public UdpClientWrapper(UdpClient udpClient)
    {
        _udpClient = udpClient;
    }

    public UdpClientWrapper(UdpClient udpClient, int port)
    {
        _udpClient = udpClient;
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
    }

    public Task<UdpReceiveResult> ReceiveAsync()
    {
        return _udpClient.ReceiveAsync();
    }

    public void AllowNatTraversal(bool enabled)
    {
        if ( RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
             RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // AllowNatTraversal is not supported on Linux or macOS.
            _logger.Warn("AllowNatTraversal is not supported on Linux or macOS. Skipping setting.");
            return;
        }
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

    public Task SendAsync(byte[] datagram, int bytes, IPEndPoint endPoint)
    {
        return _udpClient.SendAsync(datagram, bytes, endPoint);
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
