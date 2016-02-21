using System;
using System.Collections.Generic;

using UnityEngine;
using Railgun;

public class DemoObject : MonoBehaviour
{
  public DemoEntity Entity { get; set; }

  private int tick0 = -1;
  private int tick1 = -1;

  private float x0 = 0.0f;
  private float x1 = 0.0f;

  public bool DoInterpolation = false;

  void Start()
  {
    this.Entity.StateUpdated += OnStateUpdated;
  }

  void Update()
  {
    if (this.Entity != null)
      transform.position = new Vector2(
        this.GetExtrapolatedX(), 
        this.Entity.State.Y);

    if (Input.GetKeyDown(KeyCode.Alpha1))
      this.DoInterpolation = true;
    if (Input.GetKeyDown(KeyCode.Alpha2))
      this.DoInterpolation = false;
  }

  private float GetExtrapolatedX()
  {
    if (this.DoInterpolation == false)
      return this.Entity.State.X;
    if ((this.tick0 == -1) || (this.tick1 == -1))
      return this.Entity.State.X;

    int tickDelta = this.tick1 - this.tick0;
    float xDelta = this.x1 - this.x0;

    float velocity = (xDelta / (float)tickDelta) / Time.fixedDeltaTime;

    int currentTick = Client.Instance.RemoteTick;
    int framesSinceFirst = currentTick - this.tick0;

    float timeSinceFirst = (framesSinceFirst * Time.fixedDeltaTime) + (Time.time - Time.fixedTime);
    return this.x0 + (velocity * timeSinceFirst);
  }

  void OnStateUpdated(int tick)
  {
    this.tick0 = this.tick1;
    this.x0 = this.x1;

    this.tick1 = tick;
    this.x1 = this.Entity.State.X;
  }
}
