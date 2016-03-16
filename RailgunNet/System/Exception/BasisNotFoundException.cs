using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class BasisNotFoundException : Exception
  {
    public BasisNotFoundException(string message) : base(message) { }
  }
}
