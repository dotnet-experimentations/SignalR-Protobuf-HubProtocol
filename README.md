# SignalR-Protobuf-HubProtocol

## /!\ This is an unofficial signalr protocol /!\
## /!\ Work In Progress /!\

A simple signalR Hub Protocol using protobuf to compress and send your data using binary.


## Usage

Call `AddProtobufProtocol` extension method in your `ConfigureService`.

```
public void ConfigureServices(IServiceCollection services)
        {
            // Some services
            services.AddSignalR().AddProtobufProtocol();
            // Some other services
        }
```
