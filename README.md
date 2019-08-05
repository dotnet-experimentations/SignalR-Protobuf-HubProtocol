# SignalR-Protobuf-HubProtocol

## /!\ This is an unofficial signalr protocol /!\

## /!\ Work In Progress /!\

A simple signalR Hub Protocol using protobuf to compress and send your data using binary.


## Usage

Call `AddProtobufProtocol` extension method in your `ConfigureService` with `types` of protobuf message you will serialize.
Types are needed in order to be able to deserialize the message. Note that `client` and `server` must supply the same types to deserialize property. 

```
public void ConfigureServices(IServiceCollection services)
        {
            // Some services
            services.AddSignalR().AddProtobufProtocol(new[] { typeof(ProtobufObject1), ...,  typeof(ProtobufObjectN)});
            // Some other services
        }
```

## Supported

 * Protobuf Arguments
 * String
 * Int
 * Double

You can provide any number of arguments of any supported type.

## Not Supported

 * Other primitives types
 * List<T>
 * Array<T>

## Protocol

```
- ---- ---- [-------------------] [[---- ---- [------]] ... [---- ---- [------]]]
T TL   PML           PM              AT   AL    ARG           AT   AL    ARG

T => Type (1 byte)
TL => Total Length (4 bytes)
PML => Protobuf Message Length (4 bytes)
PM => Protobuf Message (X bytes)

AT => Argument Type (4 bytes)
AL => Argument Length (4 bytes)
ARG => Argument (Y bytes)
```