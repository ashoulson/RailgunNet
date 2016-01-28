using System;
using System.Collections.Generic;

using UnityEngine;

using Railgun;

public class Test : MonoBehaviour
{
  void Start()
  {
    Railgun.Testing.RunTests();
  }
}
