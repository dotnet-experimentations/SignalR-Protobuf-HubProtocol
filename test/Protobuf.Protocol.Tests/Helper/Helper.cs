﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Protobuf.Protocol.Tests.Helper
{
    public class Helpers
    {
        public static Dictionary<string, string> GetHeaders(params string[] kvp)
        {
            var headers = new Dictionary<string, string>(kvp.Length / 2);

            for (var i = 0; i < kvp.Length; i += 2)
            {
                headers.Add(kvp[i], kvp[i + 1]);
            }

            return headers;
        }
    }
}
