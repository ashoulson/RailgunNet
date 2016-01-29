using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Reservoir;

namespace Railgun
{
  /// <summary>
  /// An image is a frozen snapshot of an entity for a particular frame.
  /// It contains a recording of all of that entity's boxes with their data.
  /// </summary>
  internal class Image : Poolable<Image>
  {
    protected internal int Id { get; set; }

    public NodeList<Box> boxes;
    public Dictionary<byte, Box> typeToBox;

    public Image()
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

    internal void Add(Box box)
    {
      this.boxes.Add(box);
      this.typeToBox[box.Type] = box;
    }

    internal Box Get(byte boxType)
    {
      return this.typeToBox[boxType];
    }

    internal void Encode(BitPacker bitPacker)
    {
      foreach (Box box in this.boxes)
        box.Encode(bitPacker);
    }

    internal void Encode(BitPacker bitPacker, Image basis)
    {
      foreach (Box box in this.boxes)
        box.Encode(bitPacker, basis.typeToBox[box.Type]);
    }

    internal void Decode(BitPacker bitPacker)
    {
      // We assume the image is already populated with boxes before decoding
      foreach (Box box in this.boxes)
        box.Decode(bitPacker);
    }

    internal void Decode(BitPacker bitPacker, Image basis)
    {
      // We assume the image is already populated with boxes before decoding
      foreach (Box box in this.boxes)
        box.Decode(bitPacker, basis.typeToBox[box.Type]);
    }
  }

}
