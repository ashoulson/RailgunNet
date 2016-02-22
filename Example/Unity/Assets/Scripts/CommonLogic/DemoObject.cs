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

  private float y0 = 0.0f;
  private float y1 = 0.0f;

  public bool DoInterpolation = false;

  void Start()
  {
    this.Entity.StateUpdated += OnStateUpdated;
  }

  void Update()
  {
    if (this.Entity != null)
    {
      float x, y;
      this.GetExtrapolated(out x, out y);
      transform.position = new Vector2(x, y);
    }

    if (Input.GetKeyDown(KeyCode.Alpha1))
      this.DoInterpolation = true;
    if (Input.GetKeyDown(KeyCode.Alpha2))
      this.DoInterpolation = false;
  }

  private void GetExtrapolated(out float x, out float y)
  {
    if ((this.DoInterpolation == false) || (this.tick0 == -1) || (this.tick1 == -1))
    {
      x = this.Entity.State.X;
      y = this.Entity.State.Y;
      return;
    }

    int tickDelta = this.tick1 - this.tick0;
    float xDelta = this.x1 - this.x0;
    float yDelta = this.y1 - this.y0;

    float velocityX = (xDelta / (float)tickDelta) / Time.fixedDeltaTime;
    float velocityY = (yDelta / (float)tickDelta) / Time.fixedDeltaTime;

    int currentTick = Client.Instance.RemoteTick;
    int framesSinceFirst = currentTick - this.tick0;

    float timeSinceFirst = (framesSinceFirst * Time.fixedDeltaTime) + (Time.time - Time.fixedTime);
    x = this.x0 + (velocityX * timeSinceFirst);
    y = this.y0 + (velocityY * timeSinceFirst);
  }

  void OnStateUpdated(int tick)
  {
    this.tick0 = this.tick1;
    this.x0 = this.x1;
    this.y0 = this.y1;

    this.tick1 = tick;
    this.x1 = this.Entity.State.X;
    this.y1 = this.Entity.State.Y;
  }
}
