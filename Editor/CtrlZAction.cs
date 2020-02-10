#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogEditor
{
    public abstract class CtrlZAction
    {
        public enum eCtrlZAction
        {
            TextChanged,
            AnswerNbChanged,
            NodeMoved,
            NodeRenamed,
            NodeLinked,
            NodeDeLinked,
            NodeInstanciated,
            NodeDeleted
        }
        public eCtrlZAction type;
        public BaseNode targetNode;
        public Dialog targetDialog;

        protected CtrlZAction(eCtrlZAction _type, BaseNode _targetNode, Dialog _targetDialog)
        {
            type = _type;
            targetNode = _targetNode;
            targetDialog = _targetDialog;
        }

        public abstract void Apply(DialogEditorCtrlZ _dialogEditorCtrlZ);
        public abstract void ApplyReverse(DialogEditorCtrlZ _dialogEditorCtrlZ);

    }

    public class CtrlZText : CtrlZAction
    {
        string oldText;
        public string text;
        public int nb;


        public CtrlZText(BaseNode _targetNode, Dialog _targetDialog, string _newText, string _oldText, int _answerNb = -2) : base(eCtrlZAction.TextChanged, _targetNode, _targetDialog)
        {
            oldText = _oldText;
            text = _newText;
            nb = _answerNb;
        }

        public override void Apply(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            if (targetNode is DialogNode)
            {
                DialogNode target = targetNode as DialogNode;
                if (nb == -2)
                {
                    target.windowTitle = text;
                }
                else if (nb == -1)
                {
                    target.text = text;
                }
                else
                {
                    target.answers[nb] = text;
                }
            }
            else if (targetNode is SubDialogNode)
            {
                (targetNode as SubDialogNode).dialog.dialogName = text;
            }
        }

        public override void ApplyReverse(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            if (targetNode is DialogNode)
            {
                DialogNode target = targetNode as DialogNode;
                if (nb == -2)
                {
                    target.windowTitle = oldText;
                }
                else if (nb == -1)
                {
                    target.text = oldText;
                }
                else
                {
                    target.answers[nb] = oldText;
                }
            }
            else if (targetNode is SubDialogNode)
            {
                (targetNode as SubDialogNode).dialog.dialogName = oldText;
            }
        }
    }

    public class CtrlZAnsNb : CtrlZAction
    {
        public int nb;

        public CtrlZAnsNb(BaseNode _targetNode, Dialog _targetDialog, int _answerNbDelta) : base(eCtrlZAction.AnswerNbChanged, _targetNode, _targetDialog)
        {
            nb = _answerNbDelta;
        }

        //only dialog nodes have answers and so only dialog nodes have an answer nb
        public override void Apply(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            (targetNode as DialogNode).nbOfAnswers += nb;
        }

        public override void ApplyReverse(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            (targetNode as DialogNode).nbOfAnswers -= nb;
        }
    }

    public class CtrlZNodeMoved : CtrlZAction
    {
        public Vector2 oldPos;
        public Vector2 newPos;


        public CtrlZNodeMoved(BaseNode _targetNode, Dialog _targetDialog, Vector2 _oldPos, Vector2 _newPos) : base(eCtrlZAction.NodeMoved, _targetNode, _targetDialog)
        {
            oldPos = _oldPos;
            newPos = _newPos;

        }

        public override void Apply(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            targetNode.windowRect.position = newPos;
            targetDialog.UpdateBezierRectPosition(targetNode);
        }

        public override void ApplyReverse(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            targetNode.windowRect.position = oldPos;
            targetDialog.UpdateBezierRectPosition(targetNode);
        }
    }

    public class CtrlZNodeRenamed : CtrlZAction
    {
        public string oldName;
        public string newName;

        public CtrlZNodeRenamed(BaseNode _targetNode, Dialog _targetDialog, string _oldName, string _newName) : base(eCtrlZAction.NodeLinked, _targetNode, _targetDialog)
        {
            oldName = _oldName;
            newName = _newName;
        }

        public override void Apply(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            targetNode.windowTitle = newName;
        }

        public override void ApplyReverse(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            targetNode.windowTitle = oldName;
        }
    }

    public class CtrlZNodeLinked : CtrlZAction
    {
        public BaseNode linkTarget;
        public int nb;

        public CtrlZNodeLinked(BaseNode _targetNode, Dialog _targetDialog, int _answerNb, BaseNode _target) : base(eCtrlZAction.NodeLinked, _targetNode, _targetDialog)
        {
            nb = _answerNb;
            linkTarget = _target;
        }

        public override void Apply(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            if (targetNode is DialogNode)
            {
                (targetNode as DialogNode).connections[nb] = linkTarget;

            }
            else if (targetNode is BeginDialogTagNode)
            {
                (targetNode as BeginDialogTagNode).firstDialogNode = linkTarget as DialogNode;
            }
            targetDialog.curvesHitboxes.Add(new KeyForCurve(targetNode, linkTarget, nb), Dialog.CreateBezierRect(targetNode, linkTarget, nb));
        }

        public override void ApplyReverse(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            if (targetNode is DialogNode)
            {
                (targetNode as DialogNode).connections.Remove(nb);

            }
            else if (targetNode is BeginDialogTagNode)
            {
                (targetNode as BeginDialogTagNode).firstDialogNode = null;
            }

            if (targetDialog.curvesHitboxes.ContainsKey(new KeyForCurve(targetNode, linkTarget, nb)))
            {
                targetDialog.curvesHitboxes.Remove(new KeyForCurve(targetNode, linkTarget, nb));
            }
        }
    }

    public class CtrlZNodeDeLinked : CtrlZAction
    {
        public BaseNode linkTarget;
        public int nb;

        public CtrlZNodeDeLinked(BaseNode _targetNode, Dialog _targetDialog, int _answerNb, BaseNode _target) : base(eCtrlZAction.NodeDeLinked, _targetNode, _targetDialog)
        {
            nb = _answerNb;
            linkTarget = _target;
        }

        public override void Apply(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            if (targetNode is DialogNode)
            {
                (targetNode as DialogNode).connections.Remove(nb);

            }
            else if (targetNode is BeginDialogTagNode)
            {
                (targetNode as BeginDialogTagNode).firstDialogNode = null;
            }

            if (targetDialog.curvesHitboxes.ContainsKey(new KeyForCurve(targetNode, linkTarget, nb)))
            {
                targetDialog.curvesHitboxes.Remove(new KeyForCurve(targetNode, linkTarget, nb));
            }
        }

        public override void ApplyReverse(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            if (targetNode is DialogNode)
            {
                (targetNode as DialogNode).connections[nb] = linkTarget;
            }
            else if (targetNode is BeginDialogTagNode)
            {
                (targetNode as BeginDialogTagNode).firstDialogNode = linkTarget as DialogNode;
            }
            targetDialog.curvesHitboxes.Add(new KeyForCurve(targetNode, linkTarget, nb), Dialog.CreateBezierRect(targetNode, linkTarget, nb));
        }
    }

    public class CtrlZNodeInstanciated : CtrlZAction
    {
        public BaseNode copy;

        public CtrlZNodeInstanciated(DialogEditorCtrlZ _dialogEditorCtrlZ, BaseNode _targetNode, Dialog _targetDialog) : base(eCtrlZAction.NodeInstanciated, _targetNode, _targetDialog)
        {
            if (_targetNode is DialogNode)
            {
                copy = new DialogNode(_targetNode as DialogNode);
                _dialogEditorCtrlZ.UpdateRef(targetNode, copy, this);
            }
            else if (_targetNode is SubDialogNode)
            {
                copy = new SubDialogNode(_targetNode as SubDialogNode);
                _dialogEditorCtrlZ.UpdateRef(targetNode, copy, this);
            }
        }

        public override void Apply(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            if (targetNode is DialogNode)
            {
                targetNode = targetDialog.AddDialogNodeNoCtrlZ(copy as DialogNode);
                _dialogEditorCtrlZ.UpdateRef(copy, targetNode, this);
            }
            else if (targetNode is SubDialogNode)
            {
                targetNode = targetDialog.AddSubDialogNoCtrlZ(copy as SubDialogNode);
                _dialogEditorCtrlZ.UpdateRef(copy, targetNode, this);
            }
        }

        public override void ApplyReverse(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            if (targetNode is DialogNode)
            {
                copy = new DialogNode(targetNode as DialogNode);
                _dialogEditorCtrlZ.UpdateRef(targetNode, copy, this);
                targetDialog.RemoveDialogNode(targetNode as DialogNode, false);
            }
            else if (targetNode is SubDialogNode)
            {
                copy = new SubDialogNode(targetNode as SubDialogNode);
                _dialogEditorCtrlZ.UpdateRef(targetNode, copy, this);
                targetDialog.RemoveSubDialogNode(targetNode as SubDialogNode, false);
            }
        }
    }

    public class CtrlZNodeDeleted : CtrlZAction
    {
        public BaseNode copy;

        public CtrlZNodeDeleted(DialogEditorCtrlZ _dialogEditorCtrlZ, BaseNode _targetNode, Dialog _targetDialog) : base(eCtrlZAction.NodeDeleted, _targetNode, _targetDialog)
        {
            if (targetNode is DialogNode)
            {
                copy = new DialogNode(_targetNode as DialogNode);
                _dialogEditorCtrlZ.UpdateRef(targetNode, copy, this);
            }
            else if (_targetNode is SubDialogNode)
            {
                copy = new SubDialogNode(_targetNode as SubDialogNode);
                _dialogEditorCtrlZ.UpdateRef(targetNode, copy, this);
            }
        }

        public override void Apply(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            if (targetNode is DialogNode)
            {
                copy = new DialogNode(targetNode as DialogNode);
                _dialogEditorCtrlZ.UpdateRef(targetNode, copy, this);
                targetDialog.RemoveDialogNode(targetNode as DialogNode, false);
            }
            else if (targetNode is SubDialogNode)
            {
                copy = new SubDialogNode(targetNode as SubDialogNode);
                _dialogEditorCtrlZ.UpdateRef(targetNode, copy, this);
                targetDialog.RemoveSubDialogNode(targetNode as SubDialogNode, false);
            }
        }

        public override void ApplyReverse(DialogEditorCtrlZ _dialogEditorCtrlZ)
        {
            if (targetNode is DialogNode)
            {
                targetNode = targetDialog.AddDialogNodeNoCtrlZ(targetNode as DialogNode);
                _dialogEditorCtrlZ.UpdateRef(copy, targetNode, this);
            }
            else if (targetNode is SubDialogNode)
            {
                targetNode = targetDialog.AddSubDialogNoCtrlZ(copy as SubDialogNode);
                _dialogEditorCtrlZ.UpdateRef(copy, targetNode, this);
            }
        }
    }
}

#endif
