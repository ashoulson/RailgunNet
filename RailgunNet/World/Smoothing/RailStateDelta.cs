using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class RailStateDelta
  {
    internal RailRingDelta<RailState> Delta { get; private set; }

    public RailState Prior { get { return this.Delta.Prior; } }
    public RailState Latest { get { return this.Delta.Latest; } }
    public RailState Next { get { return this.Delta.Next; } }

    public RailStateDelta()
    {
      this.Delta = new RailRingDelta<RailState>();
    }

    public void Set(RailState prior, RailState latest, RailState next)
    {
      this.Delta.Set(prior, latest, next);
    }

    public void Update(RailStateBuffer buffer, int currentTick)
    {
      buffer.PopulateDelta(this.Delta, currentTick);
    }
  }
}
