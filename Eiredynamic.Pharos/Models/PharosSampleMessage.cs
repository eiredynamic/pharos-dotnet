using System;
using System.Net;

namespace Eiredynamic.Pharos.Models;

public class PharosSampleMessage
{
    public Guid HostId { get; init; }
    public string Hostname { get; init; } = Dns.GetHostName();
    public Guid MessageId { get; init; } = Guid.NewGuid(); // duplicate detection
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Message { get; set; } = $"Hello from Pharos!";

    public PharosSampleMessage()
    {
        
    }

    public PharosSampleMessage(Guid hostId): base()
    {
        HostId = hostId;
    }

    public PharosSampleMessage(Guid hostId, string message) : base()
    {
        HostId = hostId;
        Message = message;
    }
}
