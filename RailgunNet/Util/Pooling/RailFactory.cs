using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal abstract class RailFactory<T1>
  {
    public abstract T1 Create();
  }

  internal class RailFactory<TBase, TDerived> : RailFactory<TBase>
    where TDerived : TBase, new()
  {
    public override TBase Create()
    {
      return new TDerived();
    }
  }
}
