using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class RailScopeEvaluator
  {
    protected internal virtual bool Evaluate(
      RailEvent evnt)
    {
      return true;
    }

    protected internal virtual bool Evaluate(
      RailEntity entity, 
      int ticksSinceSend,
      out float priority)
    {
      priority = 1.0f;
      return true;
    }
  }
}
