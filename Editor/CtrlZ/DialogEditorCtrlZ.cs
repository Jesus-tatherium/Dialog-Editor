#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogEditor
{
    public class DialogEditorCtrlZ
    {
        List<CtrlZAction> ctrlZActions = new List<CtrlZAction>();
        int currentAction = -1;

        public void UpdateRef(BaseNode _oldRef, BaseNode _newRef, CtrlZAction _except)
        {
            foreach (CtrlZAction action in ctrlZActions)
            {
                if (action != _except)
                {
                    if (action.targetNode == _oldRef)
                    {
                        action.targetNode = _newRef;
                    }
                    if (action is CtrlZNodeLinked)
                    {
                        if ((action as CtrlZNodeLinked).linkTarget == _oldRef)
                        {
                            (action as CtrlZNodeLinked).linkTarget = _newRef;
                        }
                    }
                    else if (action is CtrlZNodeDeLinked)
                    {
                        if ((action as CtrlZNodeDeLinked).linkTarget == _oldRef)
                        {
                            (action as CtrlZNodeDeLinked).linkTarget = _newRef;
                        }
                    }
                }
            }
        }

        public void Add(CtrlZAction _ctrlZAction)
        {
            int firstDel = currentAction + 1;
            if (firstDel < ctrlZActions.Count)
            {
                int end = ctrlZActions.Count - firstDel;
                ctrlZActions.RemoveRange(firstDel, end);
            }

            currentAction++;
            ctrlZActions.Add(_ctrlZAction);
            //Debug.Log("Current:" + currentAction);
        }

        public void Backward()
        {
            if (currentAction < 0)
            {
                return;
            }
            CtrlZAction toDo = ctrlZActions[currentAction];
            toDo.ApplyReverse(this);
            currentAction--;

            DialogEditor.Instance.FocusOnCtrlZAction(toDo);
            //Debug.Log("Current:" + currentAction);
        }


        public void Forward()
        {
            if (currentAction + 1 >= ctrlZActions.Count)
            {
                return;
            }
            currentAction++;
            CtrlZAction toDo = ctrlZActions[currentAction];
            toDo.Apply(this);

            DialogEditor.Instance.FocusOnCtrlZAction(toDo);
            //Debug.Log("Current:" + currentAction);
        }
    }
}
#endif
