using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public interface IEncodableType<T> 
    where T : struct
  {
    int RequiredBits { get; }
    uint Pack();
    T Unpack(uint data);
  }
}
