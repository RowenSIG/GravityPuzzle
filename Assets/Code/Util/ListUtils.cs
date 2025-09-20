using System.Collections.Generic;
using UnityEngine;

public static class ListUtils
{
    public static T Random<T>(this List<T> list)
    {
        if (list == null || list.Count <= 0)
        {
            return default;
        }
        int count = list.Count;
        var rand = UnityEngine.Random.Range(0, count);
        return list[rand];
    }

    public static T End<T>(this List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            return default;
        }
        int count = list.Count;
        var value = list[count - 1];
        return value;
    }
}
