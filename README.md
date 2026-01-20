[![NuGet Version](https://img.shields.io/nuget/v/atc.opc.ua.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/atc.opc.ua)

# Atc.Opc.Ua

OPC UA industrial library for executing commands, reads and writes on OPC UA servers

# Table of Contents

- [Atc.Opc.Ua](#atcopcua)
- [Table of Contents](#table-of-contents)
- [Quick Start](#quick-start)
- [OpcUaClient](#opcuaclient)
  - [Basic Usage](#basic-usage)
  - [Reading Enum DataTypes](#reading-enum-datatypes)
  - [Configuring OPC UA Security Settings](#configuring-opc-ua-security-settings)
  - [Configuring OPC UA Client Options](#configuring-opc-ua-client-options)
  - [Keep-alive behavior and options](#keep-alive-behavior-and-options)
  - [Best Practices](#best-practices)
- [CLI Tool](#cli-tool)
  - [Installation](#installation)
  - [Update](#update)
  - [Usage](#usage)
    - [Option --help](#option---help)
    - [Scanning the Address Space](#scanning-the-address-space)
    - [Reading Enum DataType Definitions](#reading-enum-datatype-definitions)
- [Requirements](#requirements)
- [How to contribute](#how-to-contribute)

# Quick Start

Here's a minimal example to connect to an OPC UA server, read a node, and disconnect:

```csharp
using Atc.Opc.Ua.Services;
using Microsoft.Extensions.Logging;

// Create logger
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<OpcUaClient>();

// Create client
using var client = new OpcUaClient(logger);

// Connect
var serverUri = new Uri("opc.tcp://opcuaserver.com:48010");
var (connected, connectError) = await client.ConnectAsync(serverUri, CancellationToken.None);

if (!connected)
{
    Console.WriteLine($"Connection failed: {connectError}");
    return;
}

// Read a node variable
var (succeeded, nodeVariable, readError) = await client.ReadNodeVariableAsync(
    "ns=2;s=Demo.Dynamic.Scalar.Float",
    includeSampleValue: true,
    CancellationToken.None);

if (succeeded && nodeVariable != null)
{
    Console.WriteLine($"Value: {nodeVariable.Value}");
}
else
{
    Console.WriteLine($"Read failed: {readError}");
}

// Disconnect
await client.DisconnectAsync(CancellationToken.None);
```

# OpcUaClient

## Basic Usage

After installing the latest [nuget package](https://www.nuget.org/packages/atc.opc.ua), the OpcUaClient can be wired up with dependency injection:

```csharp
services.AddTransient<IOpcUaClient, OpcUaClient>(s =>
{
    var loggerFactory = s.GetRequiredService<ILoggerFactory>();
    return new OpcUaClient(loggerFactory.CreateLogger<OpcUaClient>());
});
```

Then use it in your services:

```csharp
public class MyService
{
    private readonly IOpcUaClient opcUaClient;

    public MyService(IOpcUaClient opcUaClient)
    {
        this.opcUaClient = opcUaClient;
    }

    public async Task ReadDataAsync(CancellationToken cancellationToken)
    {
        var serverUri = new Uri("opc.tcp://opcuaserver.com:48010");

        // Connect
        var (connected, error) = await opcUaClient.ConnectAsync(
            serverUri,
            cancellationToken);

        if (!connected)
        {
            // Handle error
            return;
        }

        // Read node
        var (succeeded, nodeVariable, readError) = await opcUaClient.ReadNodeVariableAsync(
            "ns=2;s=Demo.Dynamic.Scalar.Float",
            includeSampleValue: true,
            cancellationToken);

        // Process data...

        // Disconnect
        await opcUaClient.DisconnectAsync(cancellationToken);
    }
}
```

## Reading Enum DataTypes

The client can read OPC UA enumeration DataType definitions, which is useful for displaying enum options in UIs or mapping numeric values to their symbolic names.

OPC UA enumerations can be defined in two ways:
- **EnumStrings**: Simple `LocalizedText[]` where the array index equals the enum value (e.g., `ServerState`)
- **EnumValues**: Complex `EnumValueType[]` with explicit Value, Name, DisplayName, and Description (e.g., vendor-specific enums)

The client automatically detects and handles both formats.

```csharp
// Read a single enum DataType
var (succeeded, enumDataType, error) = await opcUaClient.ReadEnumDataTypeAsync(
    "i=852", // ServerState enum
    cancellationToken);

if (succeeded && enumDataType != null)
{
    Console.WriteLine($"Enum: {enumDataType.Name} ({enumDataType.Members.Count} members)");

    foreach (var member in enumDataType.Members.OrderBy(m => m.Value))
    {
        Console.WriteLine($"  {member.Value} = {member.Name}");
    }
}

// Read multiple enum DataTypes
var nodeIds = new[] { "i=852", "ns=3;i=3063" };
var (succeeded, enumDataTypes, error) = await opcUaClient.ReadEnumDataTypesAsync(
    nodeIds,
    cancellationToken);
```

The `OpcUaEnumDataType` contains:
- `NodeId`, `Name`, `DisplayName`, `Description` - DataType metadata
- `HasEnumValues` - `true` if defined using EnumValues (complex), `false` for EnumStrings (simple)
- `Members` - Collection of `OpcUaEnumMember` with `Value`, `Name`, `DisplayName`, and `Description`

## Configuring OPC UA Security Settings

By default, the `OpcUaClient` will create its own self-signed certificate to present to external OPC UA Servers. However, if you have your own certificate to utilize, the service can be configured using the available `OpcUaSecurityOptions`. This class facilitates the configuration of certificate stores, the application certificate, and other essential security settings for secure communication.

These settings can be wired up from an `appsettings.json` file or manually constructed in code. Another constructor overload in OpcUaClient is available for injecting an instance of `IOptions<OpcUaSecurityOptions>`.

An example of this configuration in `appsettings.json` could look like the following.

> Note: The example values below will be the default values, if they are not provided.
Except subjectName, which will be something like 'OpcUaClient [RANDOM_SERIAL_NUMBER]' for the self-signed certificate generated.

```json
{
  "OpcUaSecurityOptions": {
    "PkiRootPath": "opc/pki",
    "ApplicationCertificate": {
      "StoreType": "Directory",
      "StorePath": "own",
      "SubjectName": "CN=YourApp"
    },
    "RejectedCertificates": {
      "StoreType": "Directory",
      "StorePath": "rejected"
    },
    "TrustedIssuerCertificates": {
      "StoreType": "Directory",
      "StorePath": "issuers"
    },
    "TrustedPeerCertificates": {
      "StoreType": "Directory",
      "StorePath": "trusted"
    },
    "AddAppCertToTrustedStore": true,
    "AutoAcceptUntrustedCertificates": false,
    "MinimumCertificateKeySize": 1024,
    "RejectSha1SignedCertificates": true,
    "RejectUnknownRevocationStatus": true
  }
}
```

and from your C# code:

```csharp
services
    .AddOptions<OpcUaSecurityOptions>()
    .Bind(configuration.GetRequiredSection(nameof(OpcUaSecurityOptions)))
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddTransient<IOpcUaClient, OpcUaClient>();
```

Then use with proper async patterns:

```csharp
public async Task ConnectSecurelyAsync(CancellationToken cancellationToken)
{
    var serverUri = new Uri("opc.tcp://opcuaserver.com:48010");
    var (connected, error) = await _opcUaClient.ConnectAsync(
        serverUri,
        userName: "myuser",
        password: "mypassword",
        cancellationToken);

    // ... use client
}
```

## Configuring OPC UA Client Options

You can customize the client application name and the OPC UA session timeout via `OpcUaClientOptions`. Bind them from configuration or construct them programmatically.

Example `appsettings.json`:

```json
{
  "OpcUaClientOptions": {
    "ApplicationName": "MyOpcUaClientApp",
    "SessionTimeoutMilliseconds": 1800000
  }
}
```

And wire them up:

```csharp
services
    .AddOptions<OpcUaClientOptions>()
    .Bind(configuration.GetRequiredSection(nameof(OpcUaClientOptions)))
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddTransient<IOpcUaClient, OpcUaClient>();
```

Defaults (if not provided):

- `ApplicationName`: "OpcUaClient"
- `SessionTimeoutMilliseconds`: 1,800,000 (30 minutes)

## Keep-alive behavior and options

When enabled, the client monitors the connection using OPC UA keep-alive and will attempt a background reconnect after a configurable number of consecutive failures. You can tune or disable the behavior via `OpcUaClientKeepAliveOptions`.

Example `appsettings.json`:

```json
{
  "OpcUaClientKeepAliveOptions": {
    "Enable": true,
    "IntervalMilliseconds": 15000,
    "MaxFailuresBeforeReconnect": 3,
    "ReconnectPeriodMilliseconds": 10000
  }
}
```

And wire them up:

```csharp
services
    .AddOptions<OpcUaClientKeepAliveOptions>()
    .Bind(configuration.GetRequiredSection(nameof(OpcUaClientKeepAliveOptions)))
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddTransient<IOpcUaClient, OpcUaClient>();
```

## Best Practices

When working with `OpcUaClient`, follow these async/await best practices:

- ✅ **Always pass CancellationToken**: All async methods require a `CancellationToken`. Use `CancellationToken.None` only for non-cancellable operations.

  ```csharp
  // Good - from method parameter
  public async Task ProcessAsync(CancellationToken cancellationToken)
  {
      await client.ConnectAsync(serverUri, cancellationToken);
  }

  // Good - with timeout
  using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
  await client.ConnectAsync(serverUri, cts.Token);
  ```

- ✅ **Use `using` statements**: Ensure proper disposal of the client to release resources.

  ```csharp
  using var client = new OpcUaClient(logger);
  await client.ConnectAsync(serverUri, cancellationToken);
  // Client is automatically disposed
  ```

- ✅ **Handle connection errors**: Always check the `Succeeded` flag from async operations.

  ```csharp
  var (succeeded, nodeVariable, error) = await client.ReadNodeVariableAsync(
      nodeId,
      includeSampleValue: true,
      cancellationToken);

  if (!succeeded)
  {
      _logger.LogError("Read failed: {Error}", error);
      // Handle error appropriately
  }
  ```

- ✅ **Disconnect explicitly**: Call `DisconnectAsync` before disposal for clean shutdown.

  ```csharp
  await client.DisconnectAsync(cancellationToken);
  ```

- ✅ **Reuse client instances**: For multiple operations, reuse the same connected client rather than creating new instances.

# CLI Tool

The `Atc.Opc.Ua.CLI` tool is available through a cross platform command line application.

## Installation

The tool can be installed as a .NET global tool by the following command

```powershell
dotnet tool install --global atc-opc-ua
```

or by following the instructions [here](https://www.nuget.org/packages/atc-opc-ua/) to install a specific version of the tool.

A successful installation will output something like

```powershell
The tool can be invoked by the following command: atc-opc-ua
Tool 'atc-opc-ua' (version '2.0.xxx') was successfully installed.
```

## Update

The tool can be updated by the following command

```powershell
dotnet tool update --global atc-opc-ua
```

## Usage

Since the tool is published as a .NET Tool, it can be launched from anywhere using any shell or command-line interface by calling **atc-opc-ua**. The help information is displayed when providing the `--help` argument to **atc-opc-ua**

### Option <span style="color:yellow">--help</span>

```powershell
atc-opc-ua --help


USAGE:
    atc-opc-ua.exe [OPTIONS] <COMMAND>

EXAMPLES:
    atc-opc-ua.exe testconnection -s opc.tcp://opcuaserver.com:48010
    atc-opc-ua.exe testconnection -s opc.tcp://opcuaserver.com:48010 -u username -p password
    atc-opc-ua.exe node read object -s opc.tcp://opcuaserver.com:48010 -n "ns=2;s=Demo.Dynamic.Scalar"
    atc-opc-ua.exe node read variable single -s opc.tcp://opcuaserver.com:48010 -n "ns=2;s=Demo.Dynamic.Scalar.Float"
    atc-opc-ua.exe node read variable multi -s opc.tcp://opcuaserver.com:48010 -n "ns=2;s=Demo.Dynamic.Scalar.Float" -n "ns=2;s=Demo.Dynamic.Scalar.Int32"
    atc-opc-ua.exe node read datatype single -s opc.tcp://opcuaserver.com:48010 -n "i=852"
    atc-opc-ua.exe node read datatype multi -s opc.tcp://opcuaserver.com:48010 -n "i=852" -n "ns=3;i=3063"
    atc-opc-ua.exe node scan -s opc.tcp://opcuaserver.com:48010 --starting-node-id "ns=2;s=Demo.Dynamic.Scalar" --object-depth 2 --variable-depth 1

OPTIONS:
    -h, --help       Prints help information
    -v, --version    Prints version information

COMMANDS:
    testconnection    Tests if a connection can be made to a given server
    node              Operations related to nodes
```

### Scanning the Address Space

The scan command builds an object/variable tree starting from a specified node (default: ObjectsFolder). Include / exclude filters are applied DURING traversal so unwanted branches are skipped early (reducing server browse load) rather than pruned afterwards:

```powershell
atc-opc-ua node scan -s opc.tcp://opcuaserver.com:48010 --starting-node-id "ns=2;s=Demo.Dynamic.Scalar" --object-depth 2 --variable-depth 1 --include-sample-values
```

Key options:

- `--starting-node-id` Starting object node (defaults to ObjectsFolder when omitted/empty).
- `--object-depth` Maximum depth of object traversal (0 = only starting object). Default 1.
- `--variable-depth` Maximum depth for nested variable browsing (0 = only direct variables). Default 0.
- `--include-sample-values` If set, attempts to read a representative value for variables.
- `--include-object-node-id` One or more object NodeIds to explicitly include (acts as allow‑list). When provided, objects not listed are skipped during traversal (unless explicitly excluded).
- `--exclude-object-node-id` One or more object NodeIds to exclude.
- `--include-variable-node-id` One or more variable NodeIds to explicitly include.
- `--exclude-variable-node-id` One or more variable NodeIds to exclude.

In conflicts (the same id both included and excluded) exclusion wins. When an include list is present it acts as a whitelist and nodes not listed are never browsed deeper.

Example restricting to a single variable while excluding an object:

```powershell
atc-opc-ua node scan -s opc.tcp://opcuaserver.com:48010 --starting-node-id "ns=2;s=Demo.Dynamic.Scalar" --include-variable-node-id "ns=2;s=Demo.Dynamic.Scalar.Float" --exclude-object-node-id "ns=2;s=Unwanted.Object"
```

### Reading Enum DataType Definitions

Read OPC UA enumeration DataType definitions to discover the possible values and their names:

```powershell
# Read a single enum DataType (e.g., ServerState)
atc-opc-ua node read datatype single -s opc.tcp://opcuaserver.com:48010 -n "i=852"

# Read multiple enum DataTypes
atc-opc-ua node read datatype multi -s opc.tcp://opcuaserver.com:48010 -n "i=852" -n "ns=3;i=3063"
```

The output displays a table with the enum members including their Value, Name, DisplayName, and Description.

This is useful for:
- Discovering what values an enum variable can hold
- Building UI dropdowns or selection lists
- Mapping numeric values read from variables to human-readable names

# Requirements

- **.NET 9 SDK** or later
- **OPCFoundation.NetStandard.Opc.Ua** 1.5.377.21

# How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
