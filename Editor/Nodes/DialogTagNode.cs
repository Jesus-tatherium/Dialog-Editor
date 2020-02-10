#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DialogEditor
{
    public class DialogTagNode : BaseNode
    {
        protected GUIStyle style;

        public DialogTagNode(Vector2 _pos, Vector2 _size) : base(_pos, _size)
        {
            style = new GUIStyle();
            style.fontSize = 22;
            style.alignment = TextAnchor.MiddleCenter;
        }
    }

    public class BeginDialogTagNode : DialogTagNode
    {
        public BaseNode firstDialogNode = null;

        public BeginDialogTagNode(Vector2 _pos) : base(_pos, new Vector2(200, 50))
        {

        }

        public override void DrawWindow()
        {
            Dialog DInstance = DialogEditor.Instance.currentDialog;
            base.DrawWindow();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("  Begin Dialog", style);
            if (GUILayout.Button("L", GUILayout.Width(19), GUILayout.Height(15)))
            {
                DInstance.StartLinkNode(this, firstDialogNode);
                firstDialogNode = null;
            }
            EditorGUILayout.EndHorizontal();


        }
    }

    public class EndDialogTagNode : DialogTagNode
    {
        public EndDialogTagNode(Vector2 _pos) : base(_pos, new Vector2(200, 50))
        {

        }

        public override void DrawWindow()
        {
            base.DrawWindow();
            EditorGUILayout.LabelField("End Dialog", style);
        }
    }
}
#endif
