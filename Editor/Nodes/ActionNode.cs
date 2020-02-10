#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace DialogEditor
{
    public class ActionNode : BaseNode
    {
        public DialogNode targetNode = null;

        public ActionNode(Vector2 _pos, Vector2 _size, DialogNode _dNode) : base(_pos, _size)
        {
            targetNode = _dNode;
        }
    }

    public class RenameNode : ActionNode
    {
        public string newName = "";

        public enum eErrorType
        {
            NoError,
            Used,
            NoName
        }
        public eErrorType errorType = eErrorType.NoError;
        public bool showError = false;
        string[] error = { "", "Name used elsewhere", "Enter a name" };

        public RenameNode(DialogNode _dNode) : base(_dNode.windowRect.position, new Vector2(200, 60), _dNode)
        {
            windowTitle = "ChangeNodeTitle";
        }

        public override void DrawWindow()
        {
            base.DrawWindow();
            Dialog DInstance = DialogEditor.Instance.currentDialog;

            hasBeenClicked = false;

            newName = EditorGUILayout.TextField(newName);
            if (showError)
            {
                EditorGUILayout.HelpBox(error[(int)errorType], MessageType.Error);
                if (windowRect.height < 105)
                {
                    windowRect.height = 105;
                }
            }
            /*else
            {
                EditorGUILayout.LabelField("");
            }*/

            //follow target node
            Vector2 topRPos = targetNode.GetTopRightCornerPos();
            windowRect.x = topRPos.x;
            windowRect.y = topRPos.y;


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))
            {
                hasBeenClicked = true;
                bool isAcceptable = true;

                if (newName != "")
                {
                    foreach (BaseNode node in DInstance.NodeList)
                    {
                        if (node.windowTitle == newName)
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
                if (isAcceptable)
                {
                    foreach (BaseNode node in DInstance.ActionNodeList)
                    {
                        //rename dialogText attached
                        if (node is DialogText)
                        {
                            if ((node as DialogText).targetNode == targetNode)
                            {
                                (node as DialogText).windowTitle = "Text from:" + newName;
                            }
                        }
                        //rename AnswerText attached
                        else if (node is AnswerText)
                        {
                            if ((node as AnswerText).targetNode == targetNode)
                            {
                                string prevTitle = (node as AnswerText).windowTitle;
                                int posString = prevTitle.IndexOf(":") + 1;
                                prevTitle = prevTitle.Substring(0, posString);

                                (node as AnswerText).windowTitle = prevTitle + newName;
                            }
                        }
                    }

                    DInstance.DECtrlZInstance.Add(new CtrlZNodeRenamed(targetNode, DInstance, targetNode.windowTitle, newName));
                    targetNode.windowTitle = newName;
                    DInstance.RemoveRenameNode(targetNode);
                }
                else
                {
                    showError = true;
                }
            }
            if (GUILayout.Button("Cancel"))
            {
                hasBeenClicked = true;
                DInstance.RemoveRenameNode(this);
            }
            GUILayout.EndHorizontal();

            if (hasBeenClicked)
            {
                DInstance.SetNodeToFront(this);
            }
            DInstance.SetLastDrawnWindow(this);
        }
    }

    public class DialogText : ActionNode
    {
        Vector2 scrollPos;

        public DialogText(DialogNode _dNode) : base(_dNode.windowRect.position, new Vector2(200, 150), _dNode)
        {
            windowTitle = "DialogText from:" + _dNode.windowTitle;
        }

        public override void DrawWindow()
        {
            base.DrawWindow();
            Dialog DInstance = DialogEditor.Instance.currentDialog;


            windowRect.x = targetNode.windowRect.x + targetNode.windowRect.width;
            windowRect.y = targetNode.windowRect.y;


            GUILayout.BeginVertical();

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(105));
            targetNode.text = GUILayout.TextArea(targetNode.text, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();


            if (GUILayout.Button("Close tab"))
            {
                DInstance.RemoveActionNode(this);
            }
            GUILayout.EndVertical();
            DInstance.SetLastDrawnWindow(this);
        }
    }

    public class AnswerText : ActionNode
    {
        public int targetAnswer;

        Vector2 scrollPos;

        public AnswerText(DialogNode _dNode, int _answerNb) : base(_dNode.windowRect.position, new Vector2(200, 150), _dNode)
        {
            windowTitle = "Answer" + (_answerNb + 1) + " from:" + _dNode.windowTitle;
            targetAnswer = _answerNb;
        }

        public override void DrawWindow()
        {
            base.DrawWindow();
            Dialog DInstance = DialogEditor.Instance.currentDialog;

            windowRect.x = targetNode.windowRect.x + targetNode.windowRect.width;
            windowRect.y = targetNode.windowRect.y + 93 + targetAnswer * 18;


            GUILayout.BeginVertical();

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(105));
            targetNode.answers[targetAnswer] = GUILayout.TextArea(targetNode.answers[targetAnswer], GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();


            if (GUILayout.Button("Close tab"))
            {
                DInstance.RemoveActionNode(this);
            }
            GUILayout.EndVertical();
            DInstance.SetLastDrawnWindow(this);
        }
    }

}
#endif

