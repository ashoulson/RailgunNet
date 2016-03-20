using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal interface IRailLookup<TKey, TValue>
  {
    bool TryGet(TKey key, out TValue value);
  }
}
