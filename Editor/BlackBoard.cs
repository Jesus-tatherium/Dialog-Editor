#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class BlackBoard
{
    List<BlackBoardItem> myVars = new List<BlackBoardItem>();
    public enum eVarTypes
    {
        eFloat,
        eInt,
        eBool,
        eObject
    }

    public class BlackBoardItem
    {
        public string name;
        public eVarTypes varType;
        public object obj;

        public BlackBoardItem()
        {

        }

        public BlackBoardItem(eVarTypes _type, object _obj)
        {
            varType = _type;
            obj = _obj;
        }

    }


    public void AddVar(string _name, object _obj)
    {
        _obj.GetType();
    }

    public void GetAllTypes()
    {
        Debug.Log("ok");
        Assembly assembly = typeof(string).Assembly;
        foreach (Type type in assembly.GetTypes())
        {
            Debug.Log(type.FullName);
        }
    }

    public void Display()
    {
        foreach (var item in myVars)
        {
            GUILayout.BeginHorizontal();

            item.varType = (eVarTypes)EditorGUILayout.EnumFlagsField(item.varType);
            GUILayout.EndHorizontal();
        }
    }

}
#endif