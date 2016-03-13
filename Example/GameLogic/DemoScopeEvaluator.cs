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

  protected override bool IsInScope(RailEntity entity)
  {
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

      return (origin - point).sqrMagnitude < maxDistSqr;
    }
    return true;
  }

  protected override float GetPriority(RailEntity entity, int ticksSinceSend)
  {
    if (entity == this.controlled)
      return 0.0f;

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

      return (origin - point).sqrMagnitude / (float)ticksSinceSend;
    }

    return 0.0f;
  }
}
