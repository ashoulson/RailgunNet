using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DemoEvents
{
  public static event Action<DemoEntity> EntityCreated;

  public static void OnEntityAdded(DemoEntity entity)
  {
    if (DemoEvents.EntityCreated != null)
      DemoEvents.EntityCreated.Invoke(entity);
  }
}
