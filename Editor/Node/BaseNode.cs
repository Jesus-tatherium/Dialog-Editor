#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace DialogEditor
{
    public abstract class BaseNode
    {
        public Rect windowRect;

        public string windowTitle = "";
        public bool hasBeenClicked = false;

        public BaseNode()
        {
        }

        public BaseNode(Vector2 _pos, Vector2 _size)
        {
            windowRect = new Rect(_pos.x, _pos.y, _size.x, _size.y);
            SetTittleRect();
        }

        public Rect titleRect;
        public void SetTittleRect()
        {
            titleRect.x = windowRect.x;
            titleRect.y = windowRect.y;

            titleRect.width = windowRect.width;
            titleRect.height = 20.0f;
        }

        public Vector2 GetTopRightCornerPos()
        {
            Vector2 outPut;
            outPut.x = windowRect.x + windowRect.width;
            outPut.y = windowRect.y;

            return outPut;
        }

        public virtual void DrawWindow()
        {
            SetTittleRect();
        }


        public void SetPos(Vector2 _pos)
        {
            DialogEditor.Instance.dialogEditorCtrlZ.Add(new CtrlZNodeMoved(this, DialogEditor.Instance.currentDialog, windowRect.position, _pos));
            windowRect.position = _pos;
            DialogEditor.Instance.currentDialog.SetNodeToFront(this);

        }
    }
}
#endif


