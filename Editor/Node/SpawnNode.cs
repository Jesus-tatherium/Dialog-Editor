#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogEditor
{
    public class SpawnAction
    {
        public enum eActionType
        {
            Event,
            Continuous
        }
        public eActionType type;

        public string name;
        public Action action;

        public SpawnAction()
        {

        }

        public SpawnAction(string _name, Action _action, eActionType _type = eActionType.Event)
        {
            name = _name;
            action = _action;
            type = _type;
        }

    }

    public class SpawnNode : BaseNode
    {
        public List<SpawnAction> spawnActions = new List<SpawnAction>();
        public SpawnNode child = null;
        public int buttonIndex = 0;

        public SpawnNode(Vector2 _pos, string _title, int _butIndex) : base(_pos, new Vector2(DialogEditor.WidthOfText(_title) + 20, 0))
        {
            windowTitle = _title;
            buttonIndex = _butIndex;
        }

        public override void DrawWindow()
        {
            windowRect.height = titleRect.height + 18 * spawnActions.Count;

            foreach (SpawnAction spAct in spawnActions)
            {
                if (GUILayout.Button(spAct.name, GUILayout.Height(15)))
                {
                    spAct.action();
                }
            }
        }

        public void AddAction(SpawnAction _spAct)
        {
            spawnActions.Add(_spAct);
            float tempWidth = DialogEditor.WidthOfText(_spAct.name);

            if (tempWidth + 20 > windowRect.width)
            {
                windowRect.width = tempWidth + 20;
            }
        }

        /// <summary>
        /// if child name == _name, return child
        /// else clear children and add new child
        /// </summary>
        /// <param name="_butIndex">button index</param>
        /// <param name="_name">child name</param>
        /// <param name="_dialog"></param>
        /// <returns></returns>
        public SpawnNode AddChild(int _butIndex, string _name, Dialog _dialog)
        {
            if (child != null)
            {
                if (child.windowTitle == _name)
                {
                    return child;
                }
                else
                {
                    //clean other childs
                    RemoveChilds();
                }
            }


            Vector2 pos = GetRightPos(_butIndex);
            child = _dialog.AddSpawnNode(pos, _name, _butIndex);

            return child;
        }

        public Vector2 GetRightPos(int _actIndex = 0)
        {
            return GetTopRightCornerPos() + new Vector2(0, titleRect.height + 18 * _actIndex);
        }

        private void RemoveChilds()
        {
            List<SpawnNode> spNodes = new List<SpawnNode>();
            spNodes.Add(this);
            while (spNodes[0].child != null)
            {
                spNodes.Insert(0, spNodes[0].child);
            }
            while (spNodes.Count != 1)
            {
                DialogEditor.Instance.currentDialog.RemoveSpawnNode(spNodes[0]);
                spNodes.RemoveAt(0);
            }
        }


        /**public void ExecuteAction(Vector2 _mousePos)
        {
            Rect A = windowRect.Minus(titleRect);
            Rect worlRect = A.ScaleNodeWindow(DialogEditor.Instance.zoomScale, A.TopLeft()); ;

            float value = _mousePos.y - worlRect.y;
            value /= 18 * DialogEditor.Instance.zoomScale ; // 18 is the size of 1 answer
            int result = (int)value;

            spawnActions[result].action();
        }*/
    }
}
#endif
