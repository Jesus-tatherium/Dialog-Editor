#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DialogEditor
{
    public class SubDialogNode : BaseNode
    {
        public Dialog dialog = null;
        public BaseNode connection = null;

        public enum eErrorType
        {
            NoError,
            Used,
            NoName
        }
        public eErrorType errorType = eErrorType.NoError;
        public bool showError = false;
        string[] error = { "", "Name used elsewhere", "Enter a name" };

        public SubDialogNode()
        {
            dialog = new Dialog();
            dialog.Initialize();
        }

        public SubDialogNode(SubDialogNode _dNode) : base(_dNode.windowRect.position, new Vector2(200, 60))
        {
            windowTitle = _dNode.windowTitle;
            connection = _dNode.connection;

            dialog = _dNode.dialog;
        }

        public SubDialogNode(Vector2 _pos) : base(_pos, new Vector2(200, 60))
        {
            dialog = new Dialog();
            dialog.Initialize();
        }

        public SubDialogNode(Vector2 _pos, Dialog _dialog) : base(_pos, new Vector2(200, 60))
        {
            dialog = _dialog;
        }

        public override void DrawWindow()
        {
            base.DrawWindow();
            Dialog DInstance = DialogEditor.Instance.currentDialog;
            windowTitle = "SubDialog:" + dialog.dialogName;

            bool isAcceptable = true;

            if (dialog.dialogName != "")
            {
                foreach (SubDialogNode sdNode in DInstance.SubDialogNodeList)
                {
                    if (sdNode != this && sdNode.dialog.dialogName == dialog.dialogName) 
                    {
                        isAcceptable = false;
                        errorType = eErrorType.Used;
                        break;
                    }
                }

                foreach (Dialog dialog in DialogEditor.Instance.loadedDialogs)
                {
                    if (dialog != this.dialog && dialog.dialogName == this.dialog.dialogName) 
                    {
                        isAcceptable = false;
                        errorType = eErrorType.Used;
                        break;
                    }
                }
            }
            else
            {
                isAcceptable = false;
                errorType = eErrorType.NoName;
            }

            showError = !isAcceptable;

            EditorGUILayout.LabelField("Dialog name");

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            dialog.dialogName = EditorGUILayout.TextField(dialog.dialogName, GUILayout.MinWidth(130.0f));

            if (GUILayout.Button("L", GUILayout.Width(19), GUILayout.Height(15)))
            {
                DInstance.StartLinkNode(this, connection);
                connection = null;
            }
            EditorGUILayout.EndHorizontal();

            if (showError)
            {
                EditorGUILayout.HelpBox(error[(int)errorType], MessageType.Error);
                if (windowRect.height < 105)
                {
                    windowRect.height = 105;
                }
            }
            else
            {
                windowRect.height = 60;
            }



        }
    }
}
#endif