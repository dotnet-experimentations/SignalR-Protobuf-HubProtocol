using System;
using System.Collections.Generic;
using System.Text;

namespace Protobuf.Protocol
{
    public class ArgumentDescriptor
    {
        public byte[] Argument { get; }

        public int Type { get; }

        public ArgumentDescriptor(int type, byte[] argument)
        {
            Argument = argument;
            Type = type;
        }
    }
}
