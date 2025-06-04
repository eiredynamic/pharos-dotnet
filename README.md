# Pharos

Pharos is a .NET discovery library that enables beacon/probe detection and serialized message exchange. It provides a lightweight, serverless solution for service discovery and message broadcasting using UDP multicast.

## Overview

The library consists of two main components:
- **Beacon**: Broadcasts messages to the network at configurable intervals.
- **Probe**: Listens for and receives broadcasted messages.

## Use Cases
- Simple serverless messaging.
- Server discovery (Building block for consensus algorithms).
- Service mesh discovery.
- Network presence detection.

## Installation

Pharos targets .NET Standard 2.0 for maximum compatibility and can be used in projects targeting:
- .NET Core 2.0+
- .NET 5.0+
- .NET Framework 4.6.1+

### NuGet Installation
```sh
# Install via NuGet
 dotnet add package Eiredynamic.Pharos
```

## Quick Start

### Basic Usage

```csharp
// Create a message to broadcast
var message = "hello Pharos!";

// Start broadcasting messages (Beacon)
var beacon = new Beacon<string>();
// SendBeacon will continue broadcasting until the token is cancelled
await beacon.SendBeacon(cancellationToken, message);

// Listen for messages (Probe)
var probe = new Probe<string>();

await foreach (var receivedMessage in probe.StartReceiving(cancellationToken)) 
{
    Console.WriteLine($"Received message {receivedMessage}");
}
```

### Event-Based Listening

As an alternative to StartReceiving, you can use StartReceivingWithEventsOnly and subscribe to the OnEvent event:
```csharp
var probe = new Probe<string>();

probe.OnEvent += (sender, args) =>
{
    Console.WriteLine($"Received message: {args.Event}");
};

probe.StartReceivingWithEventsOnly(cancellationToken); // still required to start the receive loop
```

The event uses the following signature:
```csharp
public event EventHandler<ProbeEventArgs<T>>? OnEvent;
```

### Dynamic Beaconing

In scenarios where the message content may change between broadcasts, you can use the dynamic overload of `SendBeacon` that accepts a delegate:

```csharp
var beacon = new Beacon<CustomMessage>();

await beacon.SendBeacon(cancellationToken, () =>
{
    return new CustomMessage
    {
        Id = Guid.NewGuid(),
        Data = DateTime.UtcNow.ToString("O")
    };
});
```

#### Method Signature
```csharp
Task SendBeacon(CancellationToken cancellationToken, Func<T> getItem)
```

- `cancellationToken`: Gracefully terminates the beacon loop when triggered.

- `getItem`: A delegate returning a new instance of the message to broadcast.

This overload re-evaluates getItem() before each beacon is sent, enabling time-sensitive or real-time messages without recreating the Beacon<T> instance.

⚠️ _Use this method when your broadcast content is dynamic. If the message is static, prefer the simpler overload to avoid unnecessary object creation and serialization._

### Generic Messages

While string is used above and `PharosSampleMessage` is provided as a reference implementation, you can use any class as a message type as long as it meets these requirements:

1. The class must be serializable (compatible with Newtonsoft.Json).
2. The class must be a reference type with a parameterless constructor (`where T : class, new()`). However, this does **not** guarantee JSON compatibility—ensure all properties are serializable and avoid complex object graphs where possible.

Example of a minimal generic message:

```csharp
public class CustomMessage 
{ 
    public Guid Id { get; init; } = Guid.NewGuid(); 
    public string Data { get; set; } = string.Empty; 
}
```

#### Network Efficiency Tips

To minimize network overhead:
- Use compact property names.
- Include only essential data fields.
- Consider using value types (`int`, `Guid`) over strings where possible.
- Avoid nested complex objects unless necessary.
- Keep serialized payloads under ~1400 bytes to stay within safe UDP limits.
- Use JSON Ignore attributes to exclude non-essential properties.

Example usage:

```csharp
// Create your custom message
var message = new CustomMessage { Data = "Hello" };

// Use with Beacon
var beacon = new Beacon<CustomMessage>();
await beacon.SendBeacon(cancellationToken, message);

// Use with Probe
var probe = new Probe<CustomMessage>();
await foreach (var received in probe.StartReceiving(cancellationToken)) 
{
    Console.WriteLine($"Received: {received.Data}");
}
```

Your message type will be automatically serialized for network transmission and deserialized upon receipt.

### Configuration

The library can be configured using `ConfigOptions`:

```csharp
var config = new ConfigOptions 
{ 
    DestinationPort = 12345, 
    SourcePort = 0, // Dynamic port allocation 
    MulticastIP = IPAddress.Parse("239.0.0.1"), 
    BeaconInterval = 5000  // Milliseconds 
};
```

## Features

- **Generic Message Support**: Both Beacon and Probe support generic message types.
- **Cancellation Support**: All operations support graceful cancellation.
- **Configurable Intervals**: Customize beacon broadcast intervals.
- **NAT Considerations**: Pharos uses UDP multicast for discovery, which may require additional configuration when crossing NAT boundaries.
- **Logging Integration**: NLog integration for diagnostic logging.

## Example Application

The solution includes an example application demonstrating basic usage.

## Requirements

- .NET Standard 2.0 compatible runtime.
- A network that supports UDP multicast.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
