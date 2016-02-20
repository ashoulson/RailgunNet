using System;
using System.Collections.Generic;

using UnityEngine;
using Railgun;

public class TestHost : MonoBehaviour
{
  private Host host;

  void Awake()
  {
    this.host = new Host(new StatePool<DemoState>());
  }

  void Start()
  {
    this.CreateDemoEntity(0);
  }

  public void CreateDemoEntity(int archetypeId)
  {
    DemoEntity entity = 
      this.host.CreateEntity<DemoEntity>(DemoTypes.TYPE_DEMO);
    entity.InitializeHost(archetypeId);
    this.host.AddEntity(entity);
  }

  void FixedUpdate()
  {
    this.host.Update();
  }
}
