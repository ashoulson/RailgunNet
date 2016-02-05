using System;
using System.Collections.Generic;

using UnityEngine;
using Railgun;

public class TestHost : MonoBehaviour
{
  private Host host;

  void Start()
  {
    this.host = new Host(new StatePool<DemoState>());
  }

  void Update()
  {
  }

  void FixedUpdate()
  {
    this.host.Update();
  }
}
