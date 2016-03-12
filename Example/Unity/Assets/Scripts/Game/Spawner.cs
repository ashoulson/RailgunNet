using System;
using System.Collections.Generic;

using UnityEngine;

public class Spawner : MonoBehaviour
{
  void Awake()
  {
    DemoEvents.ControlledCreated += this.OnControlledCreated;
    DemoEvents.DummyCreated += this.OnDummyCreated;
  }

  void Start()
  {
  }

  void Update()
  {
  }

  private void OnControlledCreated(DemoControlled entity)
  {
    GameObject go = 
      ArchetypeLibrary.Instance.Instantiate(
        entity.State.ArchetypeId);

    DemoObjectControlled obj = go.GetComponent<DemoObjectControlled>();
    obj.Entity = entity;
  }

  private void OnDummyCreated(DemoDummy entity)
  {
    Debug.Log(entity.State.ArchetypeId);

    GameObject go =
      ArchetypeLibrary.Instance.Instantiate(
        entity.State.ArchetypeId);

    DemoObjectDummy obj = go.GetComponent<DemoObjectDummy>();
    obj.Entity = entity;
  }
}
