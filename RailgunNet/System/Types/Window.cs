/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
*/

namespace Railgun
{
  public struct SequenceWindow
  {
    public const int HISTORY_LENGTH = BitArray64.LENGTH;

    public static bool AreInRange(SequenceId lowest, SequenceId highest)
    {
      return (highest - lowest) <= SequenceWindow.HISTORY_LENGTH;
    }

    private readonly SequenceId latest;
    private readonly BitArray64 historyArray;

    public SequenceId Latest { get { return this.latest; } }
    public bool IsValid { get { return latest.IsValid; } }

    public SequenceWindow(SequenceId latest)
    {
      RailDebug.Assert(latest.IsValid);
      this.latest = latest;
      this.historyArray = new BitArray64();
    }

    private SequenceWindow(SequenceId latest, BitArray64 history)
    {
      RailDebug.Assert(latest.IsValid);
      this.latest = latest;
      this.historyArray = history;
    }

    public SequenceWindow Store(SequenceId value)
    {
      SequenceId latest = this.latest;
      BitArray64 historyArray = this.historyArray;

      int difference = this.latest - value;
      if (difference > 0)
      {
        historyArray = this.historyArray.Store(difference - 1);
      }
      else
      {
        int offset = -difference;
        historyArray = (this.historyArray << offset).Store(offset - 1);
        latest = value;
      }

      return new SequenceWindow(latest, historyArray);
    }

    public bool Contains(SequenceId value)
    {
      int difference = (this.Latest - value);
      if (difference == 0)
        return true;
      return this.historyArray.Contains(difference - 1);
    }

    public bool IsNewId(SequenceId id)
    {
      if (this.ValueTooOld(id))
        return false;
      if (this.Contains(id))
        return false;
      return true;
    }

    public bool ValueTooOld(SequenceId value)
    {
      return ((this.Latest - value) > SequenceWindow.HISTORY_LENGTH);
    }
  }
}
