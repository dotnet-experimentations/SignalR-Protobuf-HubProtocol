using System;
using System.Collections.Generic;
using System.Text;

namespace Protobuf.Protocol
{
    public struct ProtobufHubProtocolConstants
    {
        public const int MESSAGE_HEADER_LENGTH = 9;

        public const int ARGUMENT_HEADER_LENGTH = 8;

        public const int TYPE_AND_TOTAL_LENGTH_HEADER = 5;

        public const int STRING_TYPE = 2;

        public const int INT_TYPE = 3;

        public const int DOUBLE_TYPE = 4;
    }
}
