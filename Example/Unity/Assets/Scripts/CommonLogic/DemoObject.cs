using System;
using System.Collections.Generic;

using UnityEngine;
using Railgun;

public class DemoObject : MonoBehaviour
{
  public DemoEntity Entity { get; set; }

  public bool DoSmoothing = false;
  public Color color = Color.white;

  void Start()
  {
  }

  void Update()
  {
    if (this.Entity != null)
      this.UpdatePosition();

    if (Input.GetKeyDown(KeyCode.Alpha1))
      this.DoSmoothing = true;
    if (Input.GetKeyDown(KeyCode.Alpha2))
      this.DoSmoothing = false;

    gameObject.GetComponent<Renderer>().material.color = this.color;
  }

  private void UpdatePosition()
  {
    if ((this.DoSmoothing) && (Entity.IsPredicted == false))
    {
      if (this.Entity.StateDelta.CanInterpolate())
      {
        this.Interpolate();
        this.color = Color.green;
      }
      else if (this.Entity.StateDelta.CanExtrapolate())
      {
        this.Extrapolate();
        this.color = Color.yellow;
      }
      else
      {
        this.transform.position =
          new Vector2(this.Entity.State.X, this.Entity.State.Y);
        this.color = Color.red;
      }
    }
    else
    {
      this.transform.position =
        new Vector2(this.Entity.State.X, this.Entity.State.Y);

      if (Entity.IsPredicted)
        this.color = Color.blue;
      else
        this.color = Color.red;
    }
  }

  private void Interpolate()
  {
    DemoState latest = (DemoState)this.Entity.StateDelta.Latest;
    DemoState next = (DemoState)this.Entity.StateDelta.Next;

    Vector2 latestPos = new Vector2(latest.X, latest.Y);
    Vector2 nextPos = new Vector2(next.X, next.Y);

    float interpolationScalar;
    this.Entity.StateDelta.GetInterpolationParams(
      this.Entity.CurrentTick,
      Time.time - Time.fixedTime,
      out interpolationScalar);

    this.transform.position = 
      Vector2.Lerp(latestPos, nextPos, interpolationScalar);
  }

  private void Extrapolate()
  {
    DemoState prior = (DemoState)this.Entity.StateDelta.Prior;
    DemoState latest = (DemoState)this.Entity.StateDelta.Latest;

    Vector2 priorPos = new Vector2(prior.X, prior.Y);
    Vector2 latestPos = new Vector2(latest.X, latest.Y);

    float timeSincePrior;
    float velocityScale;
    this.Entity.StateDelta.GetExtrapolationParams(
      this.Entity.CurrentTick,
      Time.time - Time.fixedTime,
      out timeSincePrior,
      out velocityScale);

    Vector2 velocity = (latestPos - priorPos) / velocityScale;
    this.transform.position = priorPos + (velocity * timeSincePrior);
  }
}
