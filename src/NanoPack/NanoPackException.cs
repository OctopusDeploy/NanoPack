﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NanoPack
{
    public class NanoPackException : Exception
    {
        public NanoPackException(string message):base(message)
        {
        }
    }
}
