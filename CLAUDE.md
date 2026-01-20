# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Restore, build, and test
dotnet restore
dotnet build -c Release
dotnet test -c Release --filter "Category!=Integration"

# Run a single test
dotnet test --filter "FullyQualifiedName~NodeTreeDifferTests.Diff_Should_Handle_Nulls"

# Clean build
dotnet clean -c Release && dotnet nuget locals all --clear
```

## Project Structure

This is an OPC UA client library for .NET 9 that provides:
- **Atc.Opc.Ua** (`src/Atc.Opc.Ua/`) - Core library with OPC UA client and scanner
- **Atc.Opc.Ua.CLI** (`src/Atc.Opc.Ua.CLI/`) - Command-line tool (`atc-opc-ua`)
- **Atc.Opc.Ua.Tests** (`test/Atc.Opc.Ua.Tests/`) - Unit tests using xUnit and FluentAssertions
- **Atc.Opc.Ua.Sample** (`sample/`) - Sample application

## Architecture

### Core Services
- `IOpcUaClient` / `OpcUaClient` - Main client for connecting to OPC UA servers, reading/writing nodes, and executing methods. Split across partial classes:
  - `OpcUaClient.cs` - Connection management, keep-alive handling
  - `OpcUaClientReader.cs` - Read operations
  - `OpcUaClientWriter.cs` - Write operations
  - `OpcUaClientCommandExecution.cs` - Method execution
- `IOpcUaScanner` / `OpcUaScanner` - Scans OPC UA server address space with configurable depth and filtering

### Key Contracts (`Contracts/`)
- `NodeBase` - Base class for nodes
- `NodeObject` - Represents OPC UA object nodes (contains child objects and variables)
- `NodeVariable` - Represents OPC UA variable nodes (contains value and data type info)
- `NodeScanResult` - Result of scanner operations

### Options Configuration
- `OpcUaSecurityOptions` - Certificate and security settings
- `OpcUaClientOptions` - Application name, session timeout
- `OpcUaClientKeepAliveOptions` - Keep-alive behavior
- `OpcUaScannerOptions` - Scan depth, include/exclude filters

### Result Pattern
All async operations return tuples with `(bool Succeeded, T? Result, string? ErrorMessage)` pattern for error handling.

## Code Style

Uses ATC coding rules (`.editorconfig`):
- File-scoped namespaces
- `var` for all local variables
- Private fields: camelCase
- Constants/public fields: PascalCase
- Interfaces prefixed with `I`, type parameters with `T`
- Braces required for all control flow

Analyzers: StyleCop, Meziantou, SonarAnalyzer, AsyncFixer
