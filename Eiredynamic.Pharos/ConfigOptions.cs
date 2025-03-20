using System;
using System.Net;

namespace Eiredynamic.Pharos;

public class ConfigOptions
{
    public Guid HostId { get; set; } = Guid.NewGuid();
    public int DestinationPort { get; set; } = 11000; // Port to listen on
    public int SourcePort { get; set; } = 11001; // Port to send from
    public IPAddress MulticastIP { get; set; } = IPAddress.Parse("224.168.100.2"); // Multicast group address
    public string SharedSecret { get; set; }

    public int BeaconInterval { get; set; } = 2000;
}
