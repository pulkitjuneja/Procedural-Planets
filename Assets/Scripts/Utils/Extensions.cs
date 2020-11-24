using System;

namespace ExtensionMethods
{
  public static class FloatExtensions
  {
    public static T[] SubArray<T>(this T[] data, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }
  }
}