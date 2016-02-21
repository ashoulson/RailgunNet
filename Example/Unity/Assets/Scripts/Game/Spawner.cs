using System;
using System.Collections.Generic;

using UnityEngine;

public class Spawner : MonoBehaviour
{
  void Awake()
  {
    DemoEvents.EntityCreated += this.OnEntityCreated;
  }

  void Start()
  {
  }

  void Update()
  {
  }

  private void OnEntityCreated(DemoEntity entity)
  {
    GameObject go = 
      ArchetypeLibrary.Instance.Instantiate(
        entity.State.ArchetypeId);

    DemoObject obj = go.GetComponent<DemoObject>();
    obj.Entity = entity;
  }
}
