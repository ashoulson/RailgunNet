using System;
using System.Collections.Generic;

using UnityEngine;
using Railgun;

public class DemoObject : MonoBehaviour
{
  public DemoEntity Entity { get; set; }

  void Update()
  {
    if (this.Entity != null)
      transform.position = new Vector2(
        this.Entity.State.X, 
        this.Entity.State.Y);
  }
}
