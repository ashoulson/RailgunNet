using System;
using System.Collections.Generic;

using UnityEngine;

public class ArchetypeLibrary : MonoBehaviour
{
  public static ArchetypeLibrary Instance { get; private set; }

  [SerializeField]
  GameObject[] archetypePrefabs;

  void Awake()
  {
    ArchetypeLibrary.Instance = this;
  }

  public GameObject Instantiate(int archetypeId)
  {
    return GameObject.Instantiate(this.archetypePrefabs[archetypeId]);
  }
}
