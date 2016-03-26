using System;
using System.Collections.Generic;

using UnityEngine;

public class Spawner : MonoBehaviour
{
  void Awake()
  {
    DemoEvents.ControlledCreated += this.OnControlledCreated;
    DemoEvents.DummyCreated += this.OnDummyCreated;
    DemoEvents.MimicCreated += this.OnMimicCreated;
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
    GameObject go =
      ArchetypeLibrary.Instance.Instantiate(
        entity.State.ArchetypeId);

    DemoObjectDummy obj = go.GetComponent<DemoObjectDummy>();
    obj.Entity = entity;
  }

  private void OnMimicCreated(DemoMimic entity)
  {
    GameObject go =
      ArchetypeLibrary.Instance.Instantiate(
        entity.State.ArchetypeId);

    DemoObjectMimic obj = go.GetComponent<DemoObjectMimic>();
    obj.Entity = entity;
  }

}
