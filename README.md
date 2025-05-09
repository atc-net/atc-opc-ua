[![NuGet Version](https://img.shields.io/nuget/v/atc.opc.ua.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/atc.opc.ua)

# Atc.Opc.Ua

OPC UA industrial library for executing commands, reads and writes on OPC UA servers

# Table of Contents

- [Atc.Opc.Ua](#atcopcua)
- [Table of Contents](#table-of-contents)
- [OpcUaClient](#opcuaclient)
  - [Configuring OPC UA Security Settings](#configuring-opc-ua-security-settings)
- [CLI Tool](#cli-tool)
  - [Requirements](#requirements)
  - [Installation](#installation)
  - [Update](#update)
  - [Usage](#usage)
    - [Option --help](#option---help)
- [How to contribute](#how-to-contribute)

# OpcUaClient

After installing the latest [nuget package](https://www.nuget.org/packages/atc.opc.ua), the OpcUaClient can be wired up as follows:

```csharp
services.AddTransient<IOpcUaClient, OpcUaClient>(s =>
{
    var loggerFactory = s.GetRequiredService<ILoggerFactory>();
    return new OpcUaClient(loggerFactory.CreateLogger<OpcUaClient>());
});
```

## Configuring OPC UA Security Settings

By default, the `OpcUaClient` will create its own self-signed certificate to present to external OPC UA Servers. However, if you have your own certificate to utilize, the service can be configured using the available `OpcUaSecurityOptions`. This class facilitates the configuration of certificate stores, the application certificate, and other essential security settings for secure communication.

These settings can be wired up from an `appsettings.json` file or manually constructed in code. Another constructor overload in OpcUaClient is available for injecting an instance of `IOptions<SecurityOptions>`.

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

# CLI Tool

The `Atc.Opc.Ua.CLI` tool is available through a cross platform command line application.

## Requirements

* [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

## Installation

The tool can be installed as a .NET global tool by the following command

```powershell
dotnet tool install --global atc-opc-ua
```

or by following the instructions [here](https://www.nuget.org/packages/atc-opc-ua/) to install a specific version of the tool.

A successful installation will output something like

```powershell
The tool can be invoked by the following command: atc-opc-ua
Tool 'atc-opc-ua' (version '1.0.xxx') was successfully installed.`
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

OPTIONS:
    -h, --help       Prints help information
    -v, --version    Prints version information

COMMANDS:
    testconnection    Tests if a connection can be made to a given server
    node              Operations related to nodes
```

# How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
