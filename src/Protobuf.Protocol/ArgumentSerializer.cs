using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protobuf.Protocol
{
    internal class ArgumentSerializer
    {
        private readonly Dictionary<Type, int> _protobufTypeToIndex;
        private readonly Dictionary<int, Type> _indexToProtobufType;

        private readonly int _numberOfNoProtobufObjectHandle = 8;

        internal ArgumentSerializer(IEnumerable<Type> protobufTypes)
        {
            var size = protobufTypes.Count();
            _protobufTypeToIndex = new Dictionary<Type, int>(size);
            _indexToProtobufType = new Dictionary<int, Type>(size);

            var enumerator = protobufTypes.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var index = _protobufTypeToIndex.Count + _numberOfNoProtobufObjectHandle + 1;
                _protobufTypeToIndex[enumerator.Current] = index;
                _indexToProtobufType[index] = enumerator.Current;
            }
        }

        internal List<ArgumentDescriptor> SerializeArguments(object[] arguments)
        {
            var argumentsDescriptors = new List<ArgumentDescriptor>(arguments.Length);

            for (var i = 0; i < arguments.Length; i++)
            {
                var argumentDescriptor = DescribeArgument(arguments[i]);
                argumentsDescriptors.Add(argumentDescriptor);
            }

            return argumentsDescriptors;
        }

        internal ArgumentDescriptor SerializeArgument(object argument)
        {
            return DescribeArgument(argument);
        }

        private ArgumentDescriptor DescribeArgument(object argument)
        {
            switch (argument)
            {
                case string item:
                    return new ArgumentDescriptor(ProtobufHubProtocolConstants.STRING_TYPE, Encoding.UTF8.GetBytes(item));
                case int item:
                    return new ArgumentDescriptor(ProtobufHubProtocolConstants.INT_TYPE, BitConverter.GetBytes(item));
                case double item:
                    return new ArgumentDescriptor(ProtobufHubProtocolConstants.DOUBLE_TYPE, BitConverter.GetBytes(item));
                case char item:
                    return new ArgumentDescriptor(ProtobufHubProtocolConstants.CHAR_TYPE, BitConverter.GetBytes(item));
                case float item:
                    return new ArgumentDescriptor(ProtobufHubProtocolConstants.FLOAT_TYPE, BitConverter.GetBytes(item));
                case byte item:
                    return new ArgumentDescriptor(ProtobufHubProtocolConstants.BYTE_TYPE, new[] { item });
                case bool item:
                    var value = (byte)(item ? 1 : 0);
                    return new ArgumentDescriptor(ProtobufHubProtocolConstants.BOOL_TYPE, new[] { value });
                case IMessage item:
                    return new ArgumentDescriptor(_protobufTypeToIndex[item.GetType()], item.ToByteArray());
                default:
                    return null;
            }
        }

        internal object[] DeserializeArguments(List<ArgumentDescriptor> argumentsDescriptor)
        {
            var arguments = new List<object>();

            for (var i = 0; i < argumentsDescriptor.Count; i++)
            {
                var currentDescriptor = argumentsDescriptor[i];

                if (currentDescriptor.Type <= _numberOfNoProtobufObjectHandle)
                {
                    var argument = DeserializeNotProtobufObjectArgument(argumentsDescriptor[i]);
                    arguments.Add(argument);
                }
                else
                {
                    var argument = (IMessage)Activator.CreateInstance(_indexToProtobufType[currentDescriptor.Type]);
                    argument.MergeFrom(currentDescriptor.Argument);
                    arguments.Add(argument);
                }
            }
            return arguments.ToArray();
        }

        private object DeserializeNotProtobufObjectArgument(ArgumentDescriptor argumentDescriptor)
        {
            switch (argumentDescriptor.Type)
            {
                case 2:
                    return Encoding.UTF8.GetString(argumentDescriptor.Argument);
                case 3:
                    return BinaryPrimitivesExtensions.ReadInt32(argumentDescriptor.Argument);
                case 4:
                    return BinaryPrimitivesExtensions.ReadDouble(argumentDescriptor.Argument);
                case 5:
                    return BinaryPrimitivesExtensions.ReadChar(argumentDescriptor.Argument);
                case 6:
                    return BinaryPrimitivesExtensions.ReadFloat(argumentDescriptor.Argument);
                case 7:
                    return BinaryPrimitivesExtensions.ReadByte(argumentDescriptor.Argument);
                case 8:
                    return BinaryPrimitivesExtensions.ReadByte(argumentDescriptor.Argument) == 1 ? true : false;
                default:
                    return null;
            }
        }

    }
}
