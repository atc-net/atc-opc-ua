[![NuGet Version](https://img.shields.io/nuget/v/atc.opc.ua.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/atc.opc.ua)

# Atc.Opc.Ua

OPC UA industrial library for executing commands, reads and writes on OPC UA servers

## CLI Tool

The `Atc.Opc.Ua.CLI` tool is available through a cross platform command line application.

### Requirements

* [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

### Installation

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

### Update

The tool can be updated by the following command

```powershell
dotnet tool update --global atc-opc-ua
```

### Usage

Since the tool is published as a .NET Tool, it can be launched from anywhere using any shell or command-line interface by calling **atc-opc-ua**. The help information is displayed when providing the `--help` argument to **atc-opc-ua**

#### Option <span style="color:yellow">--help</span>

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

## How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
