# Changelog

## [3.0.0](https://github.com/atc-net/atc-opc-ua/compare/v2.0.1...v3.0.0) (2026-01-22)


### ⚠ BREAKING CHANGES

* add rich DataType representation with enum support

### Features

* add enum DataType reading support ([4d8d34d](https://github.com/atc-net/atc-opc-ua/commit/4d8d34d8676968918eb518ffb76f8c020026ef0c))
* add rich DataType representation with enum support ([0807527](https://github.com/atc-net/atc-opc-ua/commit/0807527d87e39a2158c8638edb594a876d05b072))
* **cli:** add datatype read commands ([7b24c64](https://github.com/atc-net/atc-opc-ua/commit/7b24c647c33518ecf656e77058b576a1e527f948))
* **sample:** add enum DataType reading demo ([4027f5d](https://github.com/atc-net/atc-opc-ua/commit/4027f5da8609443364433568653a3ce6c7bcc389))


### Bug Fixes

* **logging:** add trace logging for DataType resolution failures ([880e3d2](https://github.com/atc-net/atc-opc-ua/commit/880e3d2e7da33b79ff9b1bfdba0265e79f372849))

## [2.0.1](https://github.com/atc-net/atc-opc-ua/compare/v2.0.0...v2.0.1) (2025-11-04)


### Bug Fixes

* align usage of cancellationToken and Async suffix of methods ([bd6da3a](https://github.com/atc-net/atc-opc-ua/commit/bd6da3a30d0c438e72bb9de7f236e6d4a9efac4a))
* ensure pascalcase is used in all LoggerMessages ([c1c62b1](https://github.com/atc-net/atc-opc-ua/commit/c1c62b1fec3b28fdd7baab0345d3096df4bd0c5e))

## [2.0.0](https://github.com/atc-net/atc-opc-ua/compare/v1.4.1...v2.0.0) (2025-10-31)


### ⚠ BREAKING CHANGES

* migrate to OPC UA 1.5.377.21 with async patterns

### Features

* migrate to OPC UA 1.5.377.21 with async patterns ([11d2647](https://github.com/atc-net/atc-opc-ua/commit/11d26475cdda8a734f243e0224a6c08806f49282))


### Bug Fixes

* include variables in backwards browser for parent node resolution ([a1b9d55](https://github.com/atc-net/atc-opc-ua/commit/a1b9d55c7bb12b1dcd955b43e0dd38eac31822ae))

## [1.4.1](https://github.com/atc-net/atc-opc-ua/compare/v1.4.0...v1.4.1) (2025-09-01)


### Bug Fixes

* add OpcUaScanner to CLI DI container ([97a356a](https://github.com/atc-net/atc-opc-ua/commit/97a356aa5b54b3c071142dc3e415aa33783fc4c8))

## [1.4.0](https://github.com/atc-net/atc-opc-ua/compare/v1.3.1...v1.4.0) (2025-08-19)


### Features

* improve KeepAlive pattern ([9c36dec](https://github.com/atc-net/atc-opc-ua/commit/9c36dec71b4582f9b3bef6c66bec0d66ffbe8cd6))
* introduce OpcUaClientOptions and OpcUaClientKeepAliveOptions incl. OpcUaClient constructor overloads ([9feca8f](https://github.com/atc-net/atc-opc-ua/commit/9feca8fc7d2cf926ec2da110630a67153dcb838a))


### Bug Fixes

* ensure proper traversal for child NodeVariables in NodeTreeDiffer ([1c54734](https://github.com/atc-net/atc-opc-ua/commit/1c547340263d5f3776871852e86af3e98e7f9a91))

## [1.3.1](https://github.com/atc-net/atc-opc-ua/compare/v1.3.0...v1.3.1) (2025-08-14)


### Bug Fixes

* ensure OpcUaDataType Name is properly set for non built-in dataTypes ([a259b64](https://github.com/atc-net/atc-opc-ua/commit/a259b64c970d441c95affe1f3c6346bf9f1c03ea))

## [1.3.0](https://github.com/atc-net/atc-opc-ua/compare/v1.2.0...v1.3.0) (2025-08-12)


### Features

* add gpt5-chatmode agent prompt ([3380a3d](https://github.com/atc-net/atc-opc-ua/commit/3380a3de7b14d24bc787d02ffc2c69fe55fab720))
* add mcp server configuration for project ([3004f8e](https://github.com/atc-net/atc-opc-ua/commit/3004f8e5f8f0e98e6993420ee8e25fe6ab3799ea))
* add NodeTreeDiffer, which can be used to report differences between two OPC UA node trees ([be329a5](https://github.com/atc-net/atc-opc-ua/commit/be329a57c02600c7cd1cdc50963ddfe04b37e224))
* add OPC UA scanner ([9e4a700](https://github.com/atc-net/atc-opc-ua/commit/9e4a700fe73046262d65c058110d9d19cf37cbf7))
* improve OpcUaClient with inclusion/exclude lists and traversal to prevent double-processing ([88863b0](https://github.com/atc-net/atc-opc-ua/commit/88863b0d5c85f0f2b5f612df1d1801b350f61b99))

## [1.2.0](https://github.com/atc-net/atc-opc-ua/compare/v1.1.0...v1.2.0) (2025-05-09)


### Features

* extend cli command and settings with NodeVariableReadDepth ([7d1b7ac](https://github.com/atc-net/atc-opc-ua/commit/7d1b7ac30288da87e4653ca1ef96a5bc1427d1c5))
* extend NodeReadObjectCommand with nodeVariableReadDepth ([4e01fce](https://github.com/atc-net/atc-opc-ua/commit/4e01fce36aad673ee3562db70c09e0e20ccecf83))
* extend ReadNodeObjectAsync with nodeVariableReadDepth ([939ccaf](https://github.com/atc-net/atc-opc-ua/commit/939ccaf53c14fe16cf6cbcd518cbd86b009c31a4))
* extend ReadNodeVariableAsync with nodeVariableReadDepth ([7e639b2](https://github.com/atc-net/atc-opc-ua/commit/7e639b2bf614947fb1d15eaf4467f8c816a3f07f))
* update to dotnet 9 and updates nuget packages ([ff48071](https://github.com/atc-net/atc-opc-ua/commit/ff480713e359592f3e464227b2e5dce740e41bf4))


### Bug Fixes

* after OpcUa-nuget is updated - fix obsolete methods ([d67e318](https://github.com/atc-net/atc-opc-ua/commit/d67e3186a269fc9dea353e66c4c96bb6082e5a05))

## [1.1.0](https://github.com/atc-net/atc-opc-ua/compare/v1.0.77...v1.1.0) (2024-10-03)


### Features

* add sample application ([3a785dd](https://github.com/atc-net/atc-opc-ua/commit/3a785dde353b53784ce9cac62fcca8c8af317cf5))
* change BrowserFactory implementation to take an ISession - also adding documentation ([0fddf9d](https://github.com/atc-net/atc-opc-ua/commit/0fddf9db3ea49ef16852fd0b7aab8724544d6caa))


### Bug Fixes

* OpcUaClientReader will always return false, if any nodes were not read successfully ([6c578b7](https://github.com/atc-net/atc-opc-ua/commit/6c578b7d27de6883228f0faf983559a2605aa6c1))
