using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal static class RailgunMath
  {
    // http://stackoverflow.com/questions/15967240/fastest-implementation-of-log2int-and-log2float
    private static readonly int[] DeBruijnLookup = new int[32]
    {
        0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
        8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
    };

    internal static int Log2(int v)
    {
      v |= v >> 1; // Round down to one less than a power of 2 
      v |= v >> 2;
      v |= v >> 4;
      v |= v >> 8;
      v |= v >> 16;

      return DeBruijnLookup[(uint)(v * 0x07C4ACDDU) >> 27];
    }

    internal static int Abs(int a)
    {
      if (a < 0)
        return -a;
      return a;
    }
  }
}
