# Changelog

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
