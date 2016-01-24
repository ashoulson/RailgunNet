using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public static class RailgunUtil
  {
    public static void Swap<T>(ref T a, ref T b)
    {
      T temp = b;
      b = a;
      a = temp;
    }
  }
}
