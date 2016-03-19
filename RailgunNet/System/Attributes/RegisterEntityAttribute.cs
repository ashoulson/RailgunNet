using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class RegisterEntityAttribute : Attribute
  {
    public Type StateType { get { return this.stateType; } }

    private readonly Type stateType;

    public RegisterEntityAttribute(Type stateType)
    {
      this.stateType = stateType;
    }
  }
}
