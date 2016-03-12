using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public interface IEncodableType<T> 
    where T : struct
  {
    int GetCost();
    void Write(BitBuffer buffer);
    T Read(BitBuffer buffer);
    T Peek(BitBuffer buffer);
  }
}
