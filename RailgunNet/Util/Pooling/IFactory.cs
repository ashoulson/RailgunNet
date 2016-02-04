using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public interface IFactory<T>
  {
    T Create();
  }
}
