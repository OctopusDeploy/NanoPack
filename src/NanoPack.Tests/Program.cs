using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NanoPack.Tests
{
    public class Program
    {
        //This is a shell around NanoPack.exe so we can use it in .net core testing, since in .net core when we reference the
        //NanoPack project we only get the dll, not the exe
        public static int Main(string[] args)
        {
            return NanoPack.Program.Main(args);
        }
    }
}
