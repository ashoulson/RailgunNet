
#if SERVER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Railgun;

public class DemoScopeEvaluator : RailScopeEvaluator
{
  private float maxDistSqr = 10000.0f;

  private readonly DemoControlled controlled;

  public DemoScopeEvaluator(DemoControlled controlled)
  {
    this.controlled = controlled;
  }

  protected override bool Evaluate(
    RailEntity entity, 
    int ticksSinceSend,
    out float priority)
  {
    priority = 0.0f;
    if (entity == this.controlled)
      return true;

    if (entity.State is DemoState)
    {
      DemoState controlledState = this.controlled.State;
      DemoState state = (DemoState)entity.State;

      Vector2 origin =
        new Vector2(
          controlledState.X,
          controlledState.Y);

      Vector2 point =
        new Vector2(
          state.X,
          state.Y);

      float distance = (origin - point).sqrMagnitude;
      if (distance > maxDistSqr)
        return false;

      priority = distance / (float)ticksSinceSend;
      return true;
    }

    return true;
  }
}
#endif