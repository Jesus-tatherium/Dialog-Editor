#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.Linq;

namespace DialogEditor
{
    public class DialogNode : BaseNode
    {
        DialogText dialogText;
        public string text = "";
        public int nbOfAnswers = 0;
        int previousAnswersNb = 0;
        public bool isDraggable = true;


        public List<string> answers = new List<string>();
        public Dictionary<int, BaseNode> connections = new Dictionary<int, BaseNode>();

        public DialogNode(Vector2 _pos) : base(_pos, new Vector2(200, 200))
        {

        }

        public DialogNode(DialogNode _dNode) : base(_dNode.windowRect.position, new Vector2(200, 200))
        {
            text = _dNode.text;
            nbOfAnswers = _dNode.nbOfAnswers;
            windowTitle = _dNode.windowTitle;

            foreach (string _answer in _dNode.answers)
            {
                answers.Add(_answer);
            }

            foreach (int _key in _dNode.connections.Keys)
            {
                connections.Add(_key, _dNode.connections[_key]);
            }
        }

        public DialogNode(Vector2 _pos, DialogNode _dNode) : base(_pos, new Vector2(200, 200))
        {
            text = _dNode.text;
            nbOfAnswers = _dNode.nbOfAnswers;

            foreach (string _answer in _dNode.answers)
            {
                answers.Add(_answer);
            }

            foreach (int _key in _dNode.connections.Keys)
            {
                connections.Add(_key, _dNode.connections[_key]);
            }
        }

        public DialogNode(DialogNodeData _dNodeData) : base(_dNodeData.pos, new Vector2(200, 200))
        {
            windowTitle = _dNodeData.name;
            SetTittleRect();

            text = _dNodeData.text;
            nbOfAnswers = _dNodeData.nbAnswers;

            for (int i = 0; i < nbOfAnswers; i++)
            {
                answers.Add(_dNodeData.answers[i]);
            }
        }

        public bool IsEmpty()
        {
            if (text != "" || nbOfAnswers != 0)
            {
                return false;
            }
            return true;
        }

        public override void DrawWindow()
        {
            base.DrawWindow();
            Dialog DInstance = DialogEditor.Instance.currentDialog;

            //int tempWinID = DialogEditor.Instance.GetWindowIDForNode(this);
            //GUI.SetNextControlName(tempWinID + " Node");

            hasBeenClicked = false;
            if (nbOfAnswers > 6)
            {
                windowRect.height = 200 + (nbOfAnswers - 6) * 18;
            }
            else
            {
                windowRect.height = 200;
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Dialog text");
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

            text = EditorGUILayout.TextField(text, GUILayout.MinWidth(130.0f));

            if (GUILayout.Button(".", GUILayout.Width(15), GUILayout.Height(15)))
            {
                hasBeenClicked = true;
                bool isFound = false;
                DialogText diaText = null;

                //if there is rename node attached, destroy it
                DInstance.RemoveRenameNode(this);

                foreach (ActionNode node in DInstance.ActionNodeList)
                {
                    if (node.targetNode == this)
                    {
                        if (node is DialogText)
                        {
                            isFound = true;
                            diaText = node as DialogText;
                            break;
                        }
                        else if (node is AnswerText)
                        {
                            //if there is a dialog box for answer, it might overlap, so we clear the answer box
                            if ((node as AnswerText).targetNode == this)
                            {
                                DInstance.RemoveActionNode(node);
                                break;
                            }
                        }
                    }
                }
                //if already an existing one, we dont allocate a new one
                if (!isFound)
                {
                    diaText = DInstance.AddDialogTextNode(this);
                }
                //swap focus and put window to front
                DInstance.SetNodeToFront(diaText);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Answers:", GUILayout.ExpandWidth(false), GUILayout.Width(55));
            if (GUILayout.Button("-", GUILayout.Width(22), GUILayout.Height(15)))
            {
                hasBeenClicked = true;
                if (nbOfAnswers > 0)
                {
                    DInstance.RemoveAnswerText(this, nbOfAnswers - 1);
                    nbOfAnswers--;
                    DInstance.DECtrlZInstance.Add(new CtrlZAnsNb(this, DInstance, -1));
                }
            }
            if (GUILayout.Button("+", GUILayout.Width(22), GUILayout.Height(15)))
            {
                hasBeenClicked = true;
                nbOfAnswers++;
                DInstance.DECtrlZInstance.Add(new CtrlZAnsNb(this, DInstance, 1));
            }
            EditorGUILayout.EndHorizontal();

            #region ListGestion
            if (previousAnswersNb < nbOfAnswers)
            {
                for (int i = previousAnswersNb; i < nbOfAnswers; i++)
                {
                    string empty = "";
                    answers.Add(empty);
                }
            }
            else if (nbOfAnswers < previousAnswersNb)
            {
                for (int i = nbOfAnswers; i < previousAnswersNb; i++)
                {
                    answers.RemoveAt(i);
                    if (connections.ContainsKey(i))
                    {
                        connections.Remove(i);
                    }
                }
            }
            previousAnswersNb = nbOfAnswers;
            #endregion

            for (int i = 0; i < nbOfAnswers; i++)
            {
                EditorGUILayout.BeginHorizontal();
                answers[i] = EditorGUILayout.TextField(answers[i]);
                //if we want to open an answer box, we must close all other boxes attached to this node, to avoid overlaping
                if (GUILayout.Button(".", GUILayout.Width(15), GUILayout.Height(15)))
                {
                    hasBeenClicked = true;
                    //no need to remove rename node, they dont overlap

                    DInstance.RemoveDialogText(this);
                    DInstance.RemoveAnswerText(this);

                    AnswerText ansText = DInstance.AddAnswerText(this, i);

                    //swap focus and put window to front
                    DInstance.SetNodeToFront(ansText);
                }
                if (GUILayout.Button("L", GUILayout.Width(19), GUILayout.Height(15)))
                {
                    hasBeenClicked = true;
                    //remove answerText to have better visibility
                    DInstance.RemoveAnswerText(this);
                    
                    BaseNode tempRef = null;
                    if (connections.ContainsKey(i))
                    {
                        tempRef = connections[i];
                        connections.Remove(i);
                    }
                    DInstance.StartLinkNode(this, tempRef, i);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (hasBeenClicked)
            {
                DInstance.SetNodeToFront(this);
            }

            EditorGUILayout.EndVertical();

            DInstance.SetLastDrawnWindow(this);
        }

    }
}
#endif


