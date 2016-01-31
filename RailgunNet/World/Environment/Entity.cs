using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class Entity : Image
  {
    public void Update()
    {

    }

    public T GetState<T>()
      where T : State<T>
    {
      return (T)this.State;
    }
  }
}
