using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal static class Iteration
  {
    public static IEnumerable<T> Interleave<T>(
      IEnumerable<T> first,
      IEnumerable<T> second)
    {
      using (IEnumerator<T> firstEnumerator = first.GetEnumerator(),
                            secondEnumerator = second.GetEnumerator())
      {
        bool hasFirstItem = true;
        bool hasSecondItem = true;

        while (hasFirstItem || hasSecondItem)
        {
          if (hasFirstItem)
          {
            hasFirstItem = firstEnumerator.MoveNext();

            if (hasFirstItem)
            {
              yield return firstEnumerator.Current;
            }
          }

          if (hasSecondItem)
          {
            hasSecondItem = secondEnumerator.MoveNext();
            if (hasSecondItem)
            {
              yield return secondEnumerator.Current;
            }
          }
        }
      }
    }
  }
}
