using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Reservoir;

namespace Railgun
{
  public class Entity : Poolable<Entity>
  {
    public byte Type { get; internal set; }

    public NodeList<Box> boxes;
    public Dictionary<byte, Box> typeToBox;

    public Entity()
    {
      this.boxes = new NodeList<Box>();
      this.typeToBox = new Dictionary<byte, Box>();
    }

    protected override void Reset()
    {
      foreach (Box box in this.boxes)
        box.Free();
      this.typeToBox.Clear();
    }

    /// <summary>
    /// Deep-copies this entity into the given image.
    /// </summary>
    internal void Populate(Image image)
    {
      foreach (Box box in this.boxes)
        image.Add(box.Clone());
    }
  }
}
