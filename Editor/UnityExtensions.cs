using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnityExtensions
{
    public static string Minus(this string _left, string _right)
    {
        int index = _left.LastIndexOf(_right);
        if (index == -1)
        {
            Debug.LogError("Couldnt find:" + _right + " int:" + _left);
            return _left;
        }
        return _left.Substring(0, index);
    }






}
