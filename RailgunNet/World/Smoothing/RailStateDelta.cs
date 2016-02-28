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

    public bool CanInterpolate()
    {
      return (this.Latest != null) && (this.Next != null);
    }

    public bool CanExtrapolate()
    {
      return (this.Latest != null) && (this.Prior != null);
    }

    public void GetInterpolationParams(
      int currentTick, 
      float frameDelta,
      out float interpolationScalar,
      float fixedDeltaTime = RailConfig.FIXED_DELTA_TIME)
    {
      float latestTime = this.Latest.Tick * fixedDeltaTime;
      float nextTime = this.Next.Tick * fixedDeltaTime;
      float currentTime = (currentTick * fixedDeltaTime) + frameDelta;

      float place = currentTime - latestTime;
      float span = nextTime - latestTime;

      interpolationScalar = place / span;
    }

    public void GetExtrapolationParams(
      int currentTick, 
      float frameDelta,
      out float timeSincePrior,
      out float velocityScale,
      float fixedDeltaTime = RailConfig.FIXED_DELTA_TIME)
    {
      float priorTime = this.Prior.Tick * fixedDeltaTime;
      float latestTime = this.Latest.Tick * fixedDeltaTime;
      float currentTime = (currentTick * fixedDeltaTime) + frameDelta;

      timeSincePrior = currentTime - priorTime;
      velocityScale = latestTime - priorTime;
    }
  }
}
