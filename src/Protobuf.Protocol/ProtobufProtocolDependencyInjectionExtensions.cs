using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ProtobufProtocolDependencyInjectionExtensions
    {
        public static TBuilder AddProtobufProtocol<TBuilder>(this TBuilder builder) where TBuilder : ISignalRBuilder
            => AddProtobufProtocol(builder, Enumerable.Empty<Type>());

        public static TBuilder AddProtobufProtocol<TBuilder>(this TBuilder builder, IEnumerable<Type> protobufTypes) where TBuilder : ISignalRBuilder
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol>(p => new ProtobufHubProtocol(protobufTypes, p.GetRequiredService<ILogger<ProtobufHubProtocol>>())));
            return builder;
        }
    }
}