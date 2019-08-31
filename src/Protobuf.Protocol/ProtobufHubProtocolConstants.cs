using System;
using System.Collections.Generic;
using System.Text;

namespace Protobuf.Protocol
{
    public struct ProtobufHubProtocolConstants
    {
        public const int TYPE_PLACEHOLDER_SIZE = 1;

        public const int TOTAL_LENGTH_PLACEHOLDER_SIZE = 4;

        public const int PROTOBUF_MESSAGE_LENGTH_PLACEHOLDER_SIZE = 4;

        public const int ARG_TYPE_PLACEHOLDER_SIZE = 4;

        public const int ARG_LENGTH_PLACEHOLDER_SIZE = 4;
        
        public const int MESSAGE_HEADER_LENGTH = 9;

        public const int ARGUMENT_HEADER_LENGTH = 8;

        public const int TYPE_AND_TOTAL_LENGTH_HEADER = 5;

        

        public const int STRING_TYPE = 2;

        public const int INT_TYPE = 3;

        public const int DOUBLE_TYPE = 4;

        public const int CHAR_TYPE = 5;

        public const int FLOAT_TYPE = 6;

        public const int BYTE_TYPE = 7;

        public const int BOOL_TYPE = 8;
    }
}
