#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using System.Linq;
using System.IO;
using System;

namespace DialogEditor
{
    public class Dialog : EditorWindow
    {
        List<DialogNode> nodeList = new List<DialogNode>();
        public List<DialogNode> NodeList
        {
            get => nodeList;
        }
        List<ActionNode> actionNodeList = new List<ActionNode>();
        public List<ActionNode> ActionNodeList
        {
            get => actionNodeList;
        }
        List<SpawnNode> spawnNodeList = new List<SpawnNode>();
        public List<SpawnNode> SpawnNodeList
        {
            get => spawnNodeList;
        }
        List<SubDialogNode> subDialogNodeList = new List<SubDialogNode>();
        public List<SubDialogNode> SubDialogNodeList
        {
            get => subDialogNodeList;
        }
        List<HandleNode> handleNodeList = new List<HandleNode>();
        public List<HandleNode> HandleNode
        {
            get => handleNodeList;
        }

        BeginDialogTagNode beginNode;
        public BeginDialogTagNode BeginNode
        {
            get => beginNode;
        }
        EndDialogTagNode endNode;
        public EndDialogTagNode EndNode
        {
            get => endNode;
        }


        public Dictionary<KeyForCurve, Rect> curvesHitboxes = new Dictionary<KeyForCurve, Rect>();
        List<KeyForCurve> overlapingHitboxes = new List<KeyForCurve>();
        int hitboxToCheck = -1;
        int currentHitbox = -1;

        public string dialogName = "New dialog";
        string savePath = "";

        #region LinkNodes
        public BaseNode selectedNodeToLink = null;
        public int selectedNodeAnswer = -1;
        public bool connectNodeClick = false;
        #endregion

        #region PutToFront
        List<BaseNode> nodeToPutToFront = new List<BaseNode>();
        bool hasToPutToFront = false;
        #endregion

        #region CtrlZText
        DialogNode copy_dNodeForText = null;
        BaseNode focusedNodeForText = null;
        string previousNameDialog = "";
        #endregion

        #region LastDrawnWindow
        private int previousLastDrawnWindow = -1;
        public int lastDrawnWindow = -1;
        #endregion

        #region MoveNodeCtrlZ
        BaseNode currentDraggedNode = null;
        Vector2 beginDraggedNodePos;
        bool isDragging = false;
        #endregion

        public DialogEditor DEInstance;
        public DialogEditorCtrlZ DECtrlZInstance;

        Vector2 copyZoomCoordsOrigin;
        float copyZoomScale;

        bool isInBezierMenu = false; // used to highligh curves
        bool isInMenu = false;

        public void Initialize()
        {
            lastDrawnWindow = 0;

            DEInstance = DialogEditor.Instance;
            DECtrlZInstance = DEInstance.dialogEditorCtrlZ;

            DEInstance.AddToOnDoubleClick(MouseDoubleClick);
            DEInstance.AddToOnMouseHold(DraggedMouse);
            DEInstance.AddToOnMouseIdle(IdleMouse);
            DEInstance.AddToOnMouseQuitIdle(QuitIdleMouse);
            DEInstance.AddToOnKeyBoardStopInputs(KeyBoardStopInput);

            copy_dNodeForText = null;
            focusedNodeForText = null;

            if (nodeList.Count != 0)
            {
                nodeList.Clear();
            }
            if (actionNodeList.Count != 0)
            {
                actionNodeList.Clear();
            }
            if (spawnNodeList.Count != 0)
            {
                spawnNodeList.Clear();
            }
            if (subDialogNodeList.Count != 0)
            {
                subDialogNodeList.Clear();
            }
            if (handleNodeList.Count != 0)
            {
                handleNodeList.Clear();
            }
            if (curvesHitboxes.Count != 0)
            {
                curvesHitboxes.Clear();
            }
            if (overlapingHitboxes.Count != 0)
            {
                overlapingHitboxes.Clear();
            }
            hitboxToCheck = -1;

            if (beginNode != null)
            {
                //DestroyImmediate(beginNode);
                beginNode = null;
            }
            if (endNode != null)
            {
                //DestroyImmediate(endNode);
                endNode = null;
            }

            AddDialogNode(new Vector2(350, 100));

            //AddDialogNode(new Vector2(50, 50));
            AddTagNode(new Vector2(50, 50), true);
            AddTagNode(new Vector2(50, 100), false);
        }

        public void UpdateAndDraw(Event e)
        {
            copyZoomCoordsOrigin = DEInstance.zoomCoordsOrigin;
            copyZoomScale = DEInstance.zoomScale;

            //curves
            foreach (DialogNode dNode in nodeList)
            {
                for (int i = 0; i < dNode.answers.Count; i++)
                {
                    if (dNode.connections.ContainsKey(i))
                    {
                        DrawCurvesForLinkedNodes(dNode, i, dNode.connections[i]);
                    }
                }
            }
            foreach (SubDialogNode sdNode in subDialogNodeList)
            {
                if (sdNode.connection != null)
                {
                    DrawCurvesForLinkedNodes(sdNode, -1, sdNode.connection);
                }

            }
            if (beginNode.firstDialogNode != null)
            {
                DrawCurvesForLinkedNodes(beginNode, -1, beginNode.firstDialogNode);
            }

            if (connectNodeClick)
            {
                Vector2 target = e.mousePosition;

                DrawBezier(GetStartPosForBezier(selectedNodeToLink, selectedNodeAnswer), target, Color.gray);
            }

            if (previousLastDrawnWindow != lastDrawnWindow && nodeList.Count != 0)
            {
                previousLastDrawnWindow = lastDrawnWindow;
                BaseNode tempNode = GetNodeForWindowID(lastDrawnWindow);
                SetWindowToFront(tempNode);
            }

            DrawWindows();
            Handles.color = Color.blue;
            //Handles.DrawSolidDisc(e.mousePosition, Vector3.forward * -1, 5f);
        }

        #region Bezier
        bool StartBezierTest(BaseNode begin, BaseNode end, Vector2 mousePos, int answerNb = -1)
        {
            Rect beginInWorld = begin.windowRect.ScaleNodeWindow(copyZoomScale, begin.windowRect.TopLeft());
            beginInWorld.position -= copyZoomCoordsOrigin * copyZoomScale;

            Rect endInWorld = end.windowRect.ScaleNodeWindow(copyZoomScale, end.windowRect.TopLeft());
            endInWorld.position -= copyZoomCoordsOrigin * copyZoomScale;

            Vector2 p0;

            if (begin is DialogNode)
            {
                p0 = new Vector2(beginInWorld.x + beginInWorld.width, beginInWorld.y + (100 + answerNb * 18) * copyZoomScale);
            }
            else if (begin is BeginDialogTagNode)
            {
                p0 = new Vector2(beginInWorld.x + beginInWorld.width, beginInWorld.y + 30 * copyZoomScale);
            }
            else if (begin is SubDialogNode)
            {
                p0 = new Vector2(beginInWorld.x + beginInWorld.width, beginInWorld.y + 45 * copyZoomScale);
            }
            else if (begin is HandleNode)
            {
                p0 = beginInWorld.position + beginInWorld.size / 2;
            }
            else
            {
                return false;
            }

            Vector3 p1 = p0 + Vector2.right * 50 * copyZoomScale;

            Vector2 p3 = endInWorld.position + new Vector2(0, 10 * copyZoomScale);
            Vector2 p2 = p3 + Vector2.left * 50 * copyZoomScale;

            Vector2 k = mousePos;

            //Debug.Log("pos:" + (k - p0).x / (p3 - p0).x);

            return BezierClickCheck(p0, p1, p2, p3, k, 0.5f);
        }

        //Alexandre COPPE ( Pacmensch )
        bool BezierClickCheck(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 k, float t, int cpt = 1)
        {
            //Calcul bezier - pos de la souris (k)
            float x = (-p0.x + 3 * p1.x - 3 * p2.x + p3.x) * t * t * t + (3 * p0.x - 6 * p1.x + 3 * p2.x) * t * t + (-3 * p0.x + 3 * p1.x) * t + p0.x - k.x;
            float y = (-p0.y + 3 * p1.y - 3 * p2.y + p3.y) * t * t * t + (3 * p0.y - 6 * p1.y + 3 * p2.y) * t * t + (-3 * p0.y + 3 * p1.y) * t + p0.y - k.y;

            float precision = 5f;

            Vector2 pente;
            pente.x = Mathf.Sign(p3.x - p0.x);
            pente.y = Mathf.Sign(p3.y - p0.y);

            if (cpt > 10) // précision de 1/ 2^n
            {
                //Debug.Log("Stack overflow exception");
                return false;
            }

            if (Mathf.Abs(x) < precision && Mathf.Abs(y) < precision)
            {
                //Debug.Log("oui " /*+ cpt*/);
                return true;
            }
            else if (IsSameSignAndDir(x, y, precision, pente, out int sign))
            {
                if (sign == 1 ? true : false)
                {
                    //mouse is between begin and current checked point
                    return BezierClickCheck(p0, p1, p2, p3, k, t - Mathf.Pow((1 / 2f), cpt + 1), cpt + 1);
                }
                else
                {
                    //mouse is between current and end checked point
                    return BezierClickCheck(p0, p1, p2, p3, k, t + Mathf.Pow((1 / 2f), cpt + 1), cpt + 1);
                }
            }
            else
            {
                //Debug.Log("nope " /*+ cpt*/);
                return false;
            }
        }

        bool IsSameSignAndDir(float x, float y, float prec, Vector2 pente, out int sign)
        {
            //multiplying by pente => same orthonormal system
            bool sameSign = Math.Sign(x) * pente.x == Mathf.Sign(y) * pente.y;

            bool xabs = Mathf.Abs(x) < prec;
            bool yabs = Mathf.Abs(y) < prec;

            if (sameSign && Mathf.Sign(x) * pente.x == 1) // if both positive 
            {
                sign = 1;
                return true;
            }
            if (Mathf.Sign(x) * pente.x == 1 && yabs || Mathf.Sign(y) * pente.y == 1 && xabs) // if one positive and other inside precision
            {
                sign = 1;
                return true;
            }

            if (sameSign && Mathf.Sign(x) * pente.x == -1) // if both negative
            {
                sign = -1;
                return true;
            }
            if (Mathf.Sign(x) * pente.x == -1 && yabs || Mathf.Sign(y) * pente.y == -1 && xabs) // if one negative and other inside precision
            {
                sign = -1;
                return true;
            }

            sign = 0;
            return false;
        }


        public static Rect CreateBezierRect(BaseNode nodeA, BaseNode nodeB, int ansNb = -1)
        {
            Vector2 nodeARightPos = nodeA.GetTopRightCornerPos();
            if (nodeA is DialogNode)
            {
                nodeARightPos.y += 100 + ansNb * 18;
            }
            else if (nodeA is BeginDialogTagNode)
            {
                nodeARightPos.y += 30;
            }
            else if (nodeA is SubDialogNode)
            {
                nodeARightPos.y += 45;
            }
            else if (nodeA is HandleNode)
            {
                nodeARightPos = (nodeA as HandleNode).GetCenterRight();
            }

            //create the rect to optimise bezier checks
            Vector2 hitPos = new Vector2();
            if (nodeARightPos.x <= nodeB.windowRect.x)
            {
                hitPos.x = nodeARightPos.x;
            }
            else
            {
                hitPos.x = nodeB.windowRect.x;
            }
            if (nodeARightPos.y <= nodeB.windowRect.y)
            {
                hitPos.y = nodeARightPos.y;
            }
            else
            {
                hitPos.y = nodeB.windowRect.y;
            }
            //10 pixel of marging around the rect
            Vector2 tempSize = nodeARightPos - nodeB.windowRect.position - new Vector2(10, 10);
            Vector2 hitSize = new Vector2(Mathf.Abs(tempSize.x) + 20, Mathf.Abs(tempSize.y) + 30);//+10 bc node links 10 pix below pos 

            return new Rect(hitPos - new Vector2(10, 10), hitSize);
        }

        public void UpdateBezierRectPosition(BaseNode nodeThatMoved)
        {
            //foreach (var item in curvesHitboxes)
            for (int i = 0; i < curvesHitboxes.Count; i++)
            {
                KeyForCurve actualKey = curvesHitboxes.Keys.ElementAt(i);
                if (actualKey.nodeA == nodeThatMoved || actualKey.nodeB == nodeThatMoved)
                {
                    curvesHitboxes[actualKey] = CreateBezierRect(actualKey.nodeA, actualKey.nodeB, actualKey.answerNb);
                }
            }
        }

        void RemoveBezier(int _toDel)
        {
            Debug.Log("hitB count:" + curvesHitboxes.Count);

            //nodeA is linked to nodeB
            KeyForCurve key = curvesHitboxes.ElementAt(_toDel).Key;
            BaseNode nodeA = key.nodeA;
            BaseNode nodeB = key.nodeB;

            if (nodeB is HandleNode)
            {
                if ((nodeB as HandleNode).prev.Count > 1)
                {
                    DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeDeLinked(nodeA, this, -1, nodeB));

                    RemovehandleNode(nodeA as HandleNode);
                    (nodeB as HandleNode).prev.Remove(nodeA);
                }
                else
                {
                    RemoveHandle((nodeB as HandleNode));
                }
            }

            if (nodeA is HandleNode)
            {
                (nodeA as HandleNode).next = null;
            }
            else if (nodeA is DialogNode)
            {
                if ((nodeA as DialogNode).connections.ContainsValue(nodeB))
                {
                    (nodeA as DialogNode).connections.Remove((nodeA as DialogNode).connections.Where(x => x.Value == nodeB).FirstOrDefault().Key);
                }
            }
            else if (nodeA is BeginDialogTagNode)
            {
                (nodeA as BeginDialogTagNode).firstDialogNode = null;
            }
            else if (nodeA is SubDialogNode)
            {
                (nodeA as SubDialogNode).connection = null;
            }

            curvesHitboxes.Remove(key);

            Debug.Log("hitB count after:" + curvesHitboxes.Count);
        }

        #region MouseBezier
        private void TestIfMouseOnBezier(Event e)
        {
            List<KeyForCurve> tempKeys = new List<KeyForCurve>();
            if (overlapingHitboxes.Count == 0)
            {
                for (int i = 0; i < curvesHitboxes.Count; i++)
                {
                    //Debug.Log(curvesHitboxes.ElementAt(i).Value);
                    if (DEInstance.ContainsInWorld(curvesHitboxes.ElementAt(i).Value, e.mousePosition))
                    {
                        tempKeys.Add(curvesHitboxes.ElementAt(i).Key);
                    }
                }

                if (tempKeys.Count <= 0)
                {
                    return;
                }

                for (int i = 0; i < tempKeys.Count; i++)
                {
                    KeyForCurve temp = tempKeys[i];
                    if (!StartBezierTest(temp.nodeA, temp.nodeB, e.mousePosition, temp.answerNb))
                    {
                        tempKeys.RemoveAt(i);
                        i--;
                    }
                }
                overlapingHitboxes = tempKeys;

                if (overlapingHitboxes.Count > 0)
                {
                    hitboxToCheck = 0;
                    currentHitbox = hitboxToCheck;
                }
            }
            else
            {
                hitboxToCheck++;
                if (hitboxToCheck >= overlapingHitboxes.Count)
                {
                    hitboxToCheck = 0;
                }
                KeyForCurve temp = overlapingHitboxes[hitboxToCheck];
                if (StartBezierTest(temp.nodeA, temp.nodeB, e.mousePosition, temp.answerNb))
                {
                    currentHitbox = hitboxToCheck;
                }
            }
        }

        void ResetMouseOnBezier()
        {
            overlapingHitboxes.Clear();
            hitboxToCheck = -1;
            currentHitbox = -1;
        }
        #endregion

        void CreatePointOnBezier(KeyForCurve key, Vector2 pos)
        {
            BaseNode nodeA = key.nodeA;
            BaseNode nodeB = key.nodeB;

            curvesHitboxes.Remove(key);
            HandleNode hNode = AddHandleNode(pos, 5f, nodeA, nodeB);

            if (nodeA is DialogNode)
            {
                (nodeA as DialogNode).connections.Remove(key.answerNb);
                (nodeA as DialogNode).connections.Add(key.answerNb, hNode);
            }
            else if (nodeA is BeginDialogTagNode)
            {
                (nodeA as BeginDialogTagNode).firstDialogNode = hNode;
            }
            else if (nodeA is SubDialogNode)
            {
                (nodeA as SubDialogNode).connection = hNode;
            }
            else if (nodeA is HandleNode)
            {
                (nodeA as HandleNode).next = hNode;
            }
            curvesHitboxes.Add(new KeyForCurve(nodeA, hNode, key.answerNb), CreateBezierRect(nodeA, hNode, key.answerNb));
            curvesHitboxes.Add(new KeyForCurve(hNode, nodeB), CreateBezierRect(hNode, nodeB));
        }

        void RemoveHandle(HandleNode from)
        {
            BaseNode head = from;

            do
            {
                // usefull bc recursive, the first iteration this is useless => connection is cleaned with RemoveAllConnectionsToThisNode
                curvesHitboxes.Remove(curvesHitboxes.Keys.Where(x => x.nodeA == head && x.nodeB == (head as HandleNode).next).First());
                DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeDeLinked(head, this, -1, (head as HandleNode).next));
                if (head is HandleNode)
                {
                    BaseNode nextHead = (head as HandleNode).next;

                    if (nextHead is HandleNode)
                    {
                        if ((nextHead as HandleNode).prev.Count > 1)
                        {
                            DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeDeleted(DECtrlZInstance, head, this));
                            handleNodeList.Remove(head as HandleNode);
                            (nextHead as HandleNode).prev.Remove(head);
                            break;
                        }
                        else
                        {
                            curvesHitboxes.Remove(new KeyForCurve(head, nextHead));
                        }
                    }
                    head = nextHead;
                }

            } while (head is HandleNode);
        }

        Vector3 GetStartPosForBezier(BaseNode _bNode, int answerNb = -1)
        {
            Vector3 startPosition = Vector3.zero;
            Rect start = _bNode.windowRect;

            if (_bNode is DialogNode)
            {
                startPosition = new Vector3(start.x + start.width - copyZoomCoordsOrigin.x, start.y + 100 + answerNb * 18 - copyZoomCoordsOrigin.y, 0);
            }
            else if (_bNode is BeginDialogTagNode)
            {
                startPosition = new Vector3(start.x + start.width - copyZoomCoordsOrigin.x, start.y + 30 - copyZoomCoordsOrigin.y, 0);
            }
            else if (_bNode is SubDialogNode)
            {
                startPosition = new Vector3(start.x + start.width - copyZoomCoordsOrigin.x, start.y + 45 - copyZoomCoordsOrigin.y, 0);
            }
            else if (_bNode is HandleNode)
            {
                startPosition = (_bNode as HandleNode).GetCenterRight();
            }
            return startPosition;
        }

        Vector3 GetEndPosForBezier(BaseNode _bNode)
        {
            Vector3 endPosition = Vector3.zero;

            if (_bNode is DialogNode || _bNode is BeginDialogTagNode || _bNode is SubDialogNode)
            {
                endPosition = new Vector3(_bNode.windowRect.x - copyZoomCoordsOrigin.x, _bNode.windowRect.y + 10 - copyZoomCoordsOrigin.y, 0);
            }
            else if (_bNode is HandleNode)
            {
                endPosition = (_bNode as HandleNode).GetCenterLeft();
            }

            return endPosition;
        }
        #endregion

        #region Inputs
        public void LeftClick(Event e)
        {
            //SendTextDataToCtrlZ();
            //ResetTextDataCtrlZ();

            Vector2 mouseClickPos = e.mousePosition;

            if (connectNodeClick)
            {
                if (IsNodeCLicked(mouseClickPos, out BaseNode clickedNode))
                {
                    //if the target is a node that can be linked
                    if (clickedNode is DialogNode || clickedNode is SubDialogNode || clickedNode is EndDialogTagNode)
                    {
                        if (selectedNodeToLink is DialogNode)
                        {
                            (selectedNodeToLink as DialogNode).connections.Add(selectedNodeAnswer, clickedNode);
                            DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeLinked(selectedNodeToLink, this, selectedNodeAnswer, clickedNode));
                        }
                        else if (selectedNodeToLink is BeginDialogTagNode)
                        {
                            (selectedNodeToLink as BeginDialogTagNode).firstDialogNode = clickedNode;
                            DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeLinked(selectedNodeToLink, this, -1, clickedNode));
                        }
                        else if (selectedNodeToLink is SubDialogNode)
                        {
                            (selectedNodeToLink as SubDialogNode).connection = clickedNode;
                            DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeLinked(selectedNodeToLink, this, -1, clickedNode));
                        }
                    }

                    curvesHitboxes.Add(new KeyForCurve(selectedNodeToLink, clickedNode, selectedNodeAnswer), CreateBezierRect(selectedNodeToLink, clickedNode, selectedNodeAnswer));
                }

                ResetNodeToLink();

                e.Use();
            }
            else
            {
                // dialog node is the only node that has multiple text fields, hence we will have to check which one was changed when we send the data to the ctrlZ => store a copy
                // if we clicked on an action node it already has the ref of which dialognode text it is changing
                if (IsNodeCLicked(mouseClickPos, out BaseNode clickedNode))
                {
                    if (clickedNode is DialogNode)
                    {
                        //Debug.Log("DialogNode");
                        copy_dNodeForText = new DialogNode(clickedNode as DialogNode);
                    }
                    else if (clickedNode is ActionNode)
                    {
                        if (clickedNode is RenameNode)
                        {
                            //Debug.Log("RenameNode");
                            copy_dNodeForText = new DialogNode((clickedNode as ActionNode).targetNode);
                        }
                        else if (clickedNode is DialogText)
                        {
                            //Debug.Log("DialogText");
                            copy_dNodeForText = new DialogNode((clickedNode as ActionNode).targetNode);
                        }
                        else if (clickedNode is AnswerText)
                        {
                            //Debug.Log("AnswerText");
                            copy_dNodeForText = new DialogNode((clickedNode as ActionNode).targetNode);
                        }
                    }
                    else if (clickedNode is SubDialogNode)
                    {
                        //Debug.Log("SubDialogNode");
                        copy_dNodeForText = null;
                        previousNameDialog = (clickedNode as SubDialogNode).dialog.dialogName;
                    }
                    focusedNodeForText = clickedNode;
                }
                else //if we didnt click on anything
                {
                    if (DEInstance.MouseIdle)
                    {
                        TestIfMouseOnBezier(e);
                    }
                }
            }
        }

        public void RightClick(Event e)
        {
            isInMenu = true;
            //SendTextDataToCtrlZ();
            //ResetTextDataCtrlZ();
            ResetNodeToLink();

            Vector2 mouseClickPos = e.mousePosition;
            //check where we clicked

            int index;

            if (DialogTagNodeClicked(mouseClickPos, true))//on beginNode
            {
                e.Use();
            }
            else if (DialogTagNodeClicked(mouseClickPos, false))//on endNode
            {
                e.Use();
            }
            else if (DialogNodeClicked(mouseClickPos, out index))
            {
                Menu manageNode = new Menu("Manage Node");
                manageNode.AddButton(new Button("Remove Node", () =>
                {
                    RemoveDialogNode(nodeList[index]);
                    DEInstance.AddToExecuteLast(() => clearSpawnList());
                }));
                manageNode.AddButton(new Button("Duplicate Node", () =>
                {
                    AddDialogNode(DEInstance.GetWorldPos(mouseClickPos), nodeList[index]);
                }));
                ComputeBaseMenu(manageNode, mouseClickPos * (1 / copyZoomScale));

                SetNodeToFront(nodeList[index]);

                e.Use();
            }
            else if (ActionNodeClicked(mouseClickPos, out index))
            {
                Menu manageNode = new Menu("Manage Node");
                manageNode.AddButton(new Button("Remove Node", () =>
                {
                    RemoveActionNode(actionNodeList[index]);
                    DEInstance.AddToExecuteLast(() => clearSpawnList());
                }));
                ComputeBaseMenu(manageNode, mouseClickPos * (1 / copyZoomScale));

                SetNodeToFront(actionNodeList[index]);

                e.Use();
            }
            else if (SubDialogNodeClicked(mouseClickPos, out index))
            {
                Menu manageNode = new Menu("Manage Node");
                manageNode.AddButton(new Button("Remove Node", () =>
                 {
                     RemoveSubDialogNode(subDialogNodeList[index]);
                     DEInstance.AddToExecuteLast(() => clearSpawnList());
                 }));
                manageNode.AddButton(new Button("Open Dialog", () =>
                {
                    int currentIndex = DEInstance.loadedDialogs.Count;
                    DEInstance.loadedDialogs.Add(subDialogNodeList[index].dialog);
                    DEInstance.currentDialog = DEInstance.loadedDialogs[currentIndex];
                    DEInstance.AddToExecuteLast(() => clearSpawnList());
                }));
                ComputeBaseMenu(manageNode, mouseClickPos * (1 / copyZoomScale));

                SetNodeToFront(subDialogNodeList[index]);

                e.Use();
            }
            else if (HandleNodeClicked(mouseClickPos, out index))
            {
                Menu manageNode = new Menu("Manage Node");
                manageNode.AddButton(new Button("Remove Node", () =>
                {
                    RemovehandleNode(handleNodeList[index]);
                    DEInstance.AddToExecuteLast(() => clearSpawnList());
                }));
                ComputeBaseMenu(manageNode, mouseClickPos * (1 / copyZoomScale));

                SetNodeToFront(handleNodeList[index]);

                e.Use();
            }
            else if (currentHitbox != -1)
            {
                isInBezierMenu = true;

                Menu manageNode = new Menu("Manage Link");

                //if i dont get the id, the ref is lost because mouse move to click on but, and list is erased, and comparing with a copy doesnt work
                int currentIndex = curvesHitboxes.Keys.ToList().IndexOf(overlapingHitboxes[currentHitbox]);
               /* manageNode.AddButton(new Button("Break Link", () =>
                {
                    RemoveBezier(currentIndex);
                    ResetMouseOnBezier();

                    DEInstance.AddToExecuteLast(() => clearSpawnList());
                }));*/
                /*manageNode.AddButton(new Button("Add Handler", () =>
                {
                    CreatePointOnBezier(curvesHitboxes.Keys.ElementAt(currentIndex), DEInstance.GetWorldPos(mouseClickPos));
                    ResetMouseOnBezier();

                    DEInstance.AddToExecuteLast(() => clearSpawnList());
                }));*/
                ComputeBaseMenu(manageNode, mouseClickPos * (1 / copyZoomScale));

                e.Use();
            }
            else
            {
                ElsewhereMenu(mouseClickPos);

                e.Use();
            }
        }

        public void LeftRelease(Event e)
        {
            if (isDragging)
            {
                Vector2 delta = beginDraggedNodePos - currentDraggedNode.windowRect.position;
                if (delta != Vector2.zero)
                {
                    DECtrlZInstance.Add(new CtrlZNodeMoved(currentDraggedNode, this, beginDraggedNodePos, currentDraggedNode.windowRect.position));
                    UpdateBezierRectPosition(currentDraggedNode);

                    ResetDraggedMouse();
                }

            }
        }

        public void RightRelease(Event e)
        {
            isInBezierMenu = false;
            isInMenu = false;
            TestIfMouseOnBezier(e);

            if (spawnNodeList.Count != 0)
            {
                DEInstance.AddToExecuteLast(() => clearSpawnList());
                e.Use();
            }
        }

        public void KeyUse(Event e)
        {
            DialogEditor.KeyboardInput(e, KeyCode.Backspace, () => Debug.Log("backSpace"));
            DialogEditor.KeyboardInput(e, KeyCode.Space, () => { SendTextDataToCtrlZ(); UpdateCopyTextDataCtrlZ(); });
            DialogEditor.KeyboardInput(e, KeyCode.Backspace, () => { Debug.Log("ok"); SendTextDataToCtrlZ(); UpdateCopyTextDataCtrlZ(); });
        }
        #endregion

        #region TextCtrlZ
        void SendTextDataToCtrlZ()
        {
            if (focusedNodeForText == null)
            {
                return;
            }
            if (focusedNodeForText is DialogNode)
            {
                if ((focusedNodeForText as DialogNode).windowTitle != copy_dNodeForText.windowTitle)
                {
                    DECtrlZInstance.Add(new CtrlZText(focusedNodeForText, this, (focusedNodeForText as DialogNode).windowTitle, copy_dNodeForText.windowTitle, -2));
                    UpdateCopyTextDataCtrlZ();
                    return;
                }
                if ((focusedNodeForText as DialogNode).text != copy_dNodeForText.text)
                {
                    DECtrlZInstance.Add(new CtrlZText(focusedNodeForText, this, (focusedNodeForText as DialogNode).text, copy_dNodeForText.text, -1));
                    UpdateCopyTextDataCtrlZ();
                    return;
                }
                for (int i = 0; i < (focusedNodeForText as DialogNode).nbOfAnswers; i++)
                {
                    if ((focusedNodeForText as DialogNode).answers.Count > i && copy_dNodeForText.answers.Count > i)
                    {
                        if ((focusedNodeForText as DialogNode).answers[i] != copy_dNodeForText.answers[i])
                        {
                            DECtrlZInstance.Add(new CtrlZText(focusedNodeForText, this, (focusedNodeForText as DialogNode).answers[i], copy_dNodeForText.answers[i], i));
                            UpdateCopyTextDataCtrlZ();
                            return;
                        }
                    }
                }
            }
            else if (focusedNodeForText is ActionNode)
            {
                ActionNode focusNode = focusedNodeForText as ActionNode;
                if (focusedNodeForText is RenameNode)
                {
                    if ((focusedNodeForText as RenameNode).newName != copy_dNodeForText.windowTitle)
                    {
                        DECtrlZInstance.Add(new CtrlZText(focusNode.targetNode, this, (focusedNodeForText as RenameNode).newName, copy_dNodeForText.windowTitle, -2));
                    }
                    UpdateCopyTextDataCtrlZ();
                    return;
                }
                else if (focusedNodeForText is DialogText)
                {
                    if ((focusedNodeForText as DialogText).targetNode.text != copy_dNodeForText.text)
                    {
                        DECtrlZInstance.Add(new CtrlZText(focusNode.targetNode, this, (focusedNodeForText as DialogText).targetNode.text, copy_dNodeForText.text, -1));
                    }
                    UpdateCopyTextDataCtrlZ();
                    return;
                }
                else if (focusedNodeForText is AnswerText)
                {
                    int index = (focusedNodeForText as AnswerText).targetAnswer;
                    if ((focusedNodeForText as AnswerText).targetNode.answers[index] != copy_dNodeForText.answers[index])
                    {
                        DECtrlZInstance.Add(new CtrlZText(focusNode.targetNode, this, (focusedNodeForText as AnswerText).targetNode.answers[index], copy_dNodeForText.answers[index], index));
                    }
                    UpdateCopyTextDataCtrlZ();
                    return;
                }
            }
            else if (focusedNodeForText is SubDialogNode)
            {
                if ((focusedNodeForText as SubDialogNode).dialog.dialogName != previousNameDialog)
                {
                    DECtrlZInstance.Add(new CtrlZText(focusedNodeForText, this, (focusedNodeForText as SubDialogNode).dialog.dialogName, previousNameDialog, -1));

                }
                UpdateCopyTextDataCtrlZ();
                return;
            }
        }

        void UpdateCopyTextDataCtrlZ()
        {
            if (focusedNodeForText is DialogNode)
            {
                copy_dNodeForText = new DialogNode(focusedNodeForText as DialogNode);
            }
            else if (focusedNodeForText is ActionNode)
            {
                copy_dNodeForText = new DialogNode((focusedNodeForText as ActionNode).targetNode);
            }
            else if (focusedNodeForText is SubDialogNode)
            {
                previousNameDialog = (focusedNodeForText as SubDialogNode).dialog.dialogName;
            }
        }

        void ResetTextDataCtrlZ()
        {
            copy_dNodeForText = null;
            focusedNodeForText = null;
        }
        #endregion

        #region LinkNodes
        public void StartLinkNode(BaseNode _node, BaseNode _connection = null, int answer = -1)
        {
            connectNodeClick = true;
            selectedNodeToLink = _node;

            //if changing and already existing link, remove the existing one
            if (_connection != null)
            {
                curvesHitboxes.Remove(new KeyForCurve(_node, _connection, answer));
            }

            if (answer != -1)
            {
                selectedNodeAnswer = answer;
            }
        }

        void ResetNodeToLink()
        {
            selectedNodeToLink = null;
            selectedNodeAnswer = -1;
            connectNodeClick = false;
        }
        #endregion

        void ElsewhereMenu(Vector2 mouseClickPos)
        {
            Menu manage = new Menu("Manage");

            Menu nodes = new Menu("Nodes");
            manage.AddButton(new Button(nodes));

            Menu addNodes = new Menu("Add Nodes");
            Menu bringNodes = new Menu("Bring Nodes");
            nodes.AddButton(new Button(addNodes));
            nodes.AddButton(new Button(bringNodes));

            addNodes.AddButton(new Button("DialogNode", AddDialogNode));
            addNodes.AddButton(new Button("SubDialog", AddSubDialog));


            bringNodes.AddButton(new Button("BeginNode", beginNode.SetPos));
            bringNodes.AddButton(new Button("EndNode", endNode.SetPos));


            #region CurrentDialog
            Menu currentDialog = new Menu("Current Dialog");
            manage.AddButton(new Button(currentDialog));

            currentDialog.AddButton(new Button("Save as", () =>
            {
                string tempPath = EditorUtility.SaveFilePanel("Load current dialog", savePath, "", "dialog.json");
                if (tempPath.Length != 0)
                {
                    savePath = tempPath;
                    Debug.Log("Save as:" + savePath);
                    dialogName = GetDialogNameFromPath(savePath);

                    JsonManager.SaveDataDialog(this, savePath);
                }
                clearSpawnList();
            }));
            currentDialog.AddButton(new Button("Save", () =>
            {
                if (savePath.Length != 0)
                {
                    Debug.Log("Save:" + savePath);
                    dialogName = GetDialogNameFromPath(savePath);

                    JsonManager.SaveDataDialog(this, savePath);
                }
                clearSpawnList();
            }));
            currentDialog.AddButton(new Button("Load", () =>
            {
                string tempPath = EditorUtility.OpenFilePanel("Load dialog", "", "dialog.json");
                if (tempPath.Length != 0)
                {
                    savePath = tempPath;
                    Debug.Log("Load:" + savePath);
                    dialogName = GetDialogNameFromPath(savePath);

                    UseSavedData(JsonManager.GetDataDialog(tempPath));
                }
                clearSpawnList();
            }));

            currentDialog.AddButton(new Button("Load as new", () =>
            {
                string tempPath = EditorUtility.OpenFilePanel("Load dialog as new", "", "dialog.json");
                if (tempPath.Length != 0)
                {
                    int index = DEInstance.loadedDialogs.Count;
                    DEInstance.loadedDialogs.Add(new Dialog());
                    Dialog newRef = DEInstance.loadedDialogs[index];
                    newRef.Initialize();
                    newRef.UseSavedData(JsonManager.GetDataDialog(tempPath));
                    newRef.savePath = tempPath;
                    newRef.dialogName = GetDialogNameFromPath(newRef.savePath);

                    Debug.Log("Load as new:" + tempPath);
                }
                clearSpawnList();
            }));


            Menu close = new Menu("Close");
            currentDialog.AddButton(new Button(close));

            close.AddButton(new Button("Yes", () =>
            {
                DeleteDialog(this);
                clearSpawnList();

            }));
            close.AddButton(new Button("no", () =>
            {
                clearSpawnList();
            }));
            #endregion

            #region CurrentProject
            Menu projectSaves = new Menu("Current Project");
            manage.AddButton(new Button(projectSaves));

            Menu newProject = new Menu("New");
            projectSaves.AddButton(new Button(newProject));

            newProject.AddButton(new Button("Yes", () =>
            {
                DEInstance.Initialize();

                clearSpawnList();

            }));
            newProject.AddButton(new Button("no", () =>
            {
                clearSpawnList();
            }));

            projectSaves.AddButton(new Button("Save as", () =>
            {
                string tempPath = EditorUtility.SaveFilePanel("Save dialog project", savePath, "", "json");
                if (tempPath.Length != 0)
                {
                    savePath = tempPath;
                    Debug.Log("Project save as:" + savePath);
                    DEInstance.currentDialog.dialogName = GetDialogNameFromPath(savePath);

                    JsonManager.SaveDataDialogEditor(savePath);
                }
                clearSpawnList();
            }));
            projectSaves.AddButton(new Button("Save", () =>
            {
                if (savePath.Length != 0)
                {
                    Debug.Log("Project save:" + savePath);
                    DEInstance.currentDialog.dialogName = GetDialogNameFromPath(savePath);

                    JsonManager.SaveDataDialogEditor(savePath);
                }
                clearSpawnList();
            }));
            projectSaves.AddButton(new Button("Load", () =>
            {
                string tempPath = EditorUtility.OpenFilePanel("Load dialog project", "", "json");

                if (tempPath.Length != 0)
                {
                    //user wants to load a dialog as a project, not possible, to avoid crash, 
                    //i add a new dialog and load the desired data in it ( if its not already loaded )
                    if (tempPath.LastIndexOf(".dialog") != -1)
                    {
                        if (DEInstance.loadedDialogs.Select(x => x.dialogName).Contains(GetDialogNameFromPath(tempPath)))
                        {
                            DEInstance.currentDialog = DEInstance.loadedDialogs.Where(x => x.dialogName == GetDialogNameFromPath(tempPath)).First();
                        }
                        else
                        {
                            int index = DEInstance.loadedDialogs.Count;
                            DEInstance.loadedDialogs.Add(new Dialog());
                            DEInstance.loadedDialogs[index].Initialize();
                            DEInstance.loadedDialogs[index].UseSavedData(JsonManager.GetDataDialog(tempPath));
                        }
                    }
                    else
                    {
                        savePath = tempPath;
                        Debug.Log("Project load:" + savePath);
                        DEInstance.currentDialog.dialogName = GetDialogNameFromPath(savePath);

                        DEInstance.UseSavedData(JsonManager.GetDataDialogEditor(tempPath));
                    }
                }
                clearSpawnList();
            }));
            #endregion

            ComputeBaseMenu(manage, mouseClickPos * (1 / copyZoomScale));
        }

        #region MenuGestion
        public delegate void PosDelegate(Vector2 pos);

        class Button
        {
            public enum eType
            {
                Menu,
                Action
            }
            public eType type;

            public enum eDelegType
            {
                Void,
                Pos,
            }
            public eDelegType delegType;

            public string name;
            public Menu menu;
            public PosDelegate posDelegate;
            public Action actDelegate;

            public Button(string _name, PosDelegate _posDelegate)
            {
                name = _name;
                type = eType.Action;
                menu = null;
                posDelegate = _posDelegate;
                delegType = eDelegType.Pos;
            }

            public Button(string _name, Action _actDelegate)
            {
                name = _name;
                type = eType.Action;
                menu = null;
                actDelegate = _actDelegate;
                delegType = eDelegType.Void;
            }

            public Button(Menu _menu)
            {
                name = _menu.title;
                type = eType.Menu;
                menu = _menu;
            }
        }

        class Menu
        {
            public string title;
            public List<Button> buttons = new List<Button>();
            public int index;

            public Menu(string _title)
            {
                title = _title;
                index = 0;
            }

            public void AddButton(Button _but)
            {
                buttons.Add(_but);
                index++;
            }
        }

        void ComputeBaseMenu(Menu _menu, Vector2 _baseMenuPos)
        {
            SpawnNode tempRef = AddSpawnNode(_baseMenuPos, _menu.title);

            for (int i = 0; i < _menu.buttons.Count; i++)
            {
                ComputeButton(_menu.buttons[i], tempRef, i);
            }
        }

        void ComputeButton(Button _but, SpawnNode _base, int _index = 0)
        {
            if (_but.type == Button.eType.Menu)
            {
                _base.AddAction(new SpawnAction(_but.menu.title, () =>
                {
                    if (spawnNodeList.Where(x => x.windowTitle == _but.menu.title).FirstOrDefault() == null)
                    {
                        SpawnNode baseChild = _base.AddChild(_index, _but.menu.title, this);
                        for (int i = 0; i < _but.menu.buttons.Count; i++)
                        {
                            ComputeButton(_but.menu.buttons[i], baseChild, i);
                        }
                    }
                }));
            }
            else
            {
                switch (_but.delegType)
                {
                    case Button.eDelegType.Void:
                        _base.AddAction(new SpawnAction(_but.name, _but.actDelegate));
                        break;
                    case Button.eDelegType.Pos:
                        _base.AddAction(new SpawnAction(_but.name, () => _but.posDelegate(_base.GetRightPos(_index) + copyZoomCoordsOrigin)));
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion

        #region EventDelegates
        private void MouseDoubleClick(Event e)
        {
            ChangeNodeTitle(e);
        }

        private void DraggedMouse(Event e)
        {
            if (IsNodeCLicked(e.mousePosition, out BaseNode clickedNode))
            {
                if (clickedNode is DialogNode || clickedNode is SubDialogNode || clickedNode is DialogTagNode)
                {
                    currentDraggedNode = clickedNode;
                    beginDraggedNodePos = currentDraggedNode.windowRect.position - copyZoomCoordsOrigin;
                    isDragging = true;
                }
                //if its an ActionNode, it cant be dragged, no need to handle that case
            }
        }

        private void ResetDraggedMouse()
        {
            isDragging = false;
            currentDraggedNode = null;
        }

        private void IdleMouse(Event e)
        {
            if (!isInMenu)
            {
                TestIfMouseOnBezier(e);
            }
        }

        private void QuitIdleMouse(Event e)
        {
            if (!isInBezierMenu)
            {
                ResetMouseOnBezier();
            }
        }

        private void KeyBoardStopInput(Event e)
        {
            SendTextDataToCtrlZ();
        }
        #endregion

        /// <summary>
        /// return true if the function was used
        /// </summary>
        /// <param name="e"> current event</param>
        /// <returns></returns>
        private bool ChangeNodeTitle(Event e)
        {
            for (int i = 0; i < nodeList.Count; i++)
            {
                DialogNode node = nodeList[i];

                if (DEInstance.ContainsInWorld(node.titleRect, e.mousePosition))
                {
                    RenameNode tempRNode;

                    //if already a RenameNode on this node
                    if (GetRenameNodeForNode(node, out tempRNode))
                    {
                        SetNodeToFront(tempRNode);
                    }
                    else
                    {
                        tempRNode = AddRenameNode(node);
                        //remove dialog text to not overlap
                        RemoveDialogText(node);

                        //swap focus and put window to front
                        SetNodeToFront(tempRNode);
                    }
                    return true;
                }
            }
            return false;
        }

        #region Affichage
        private void DrawWindows() //handleNode drawn with bezier, not here
        {
            BeginWindows();
            int id = 0;

            if (beginNode != null)
            {
                id = 0;
                beginNode.windowRect.position -= copyZoomCoordsOrigin;
                Vector2 dragPos = GUI.Window(id, beginNode.windowRect, DrawTagWindow, beginNode.windowTitle).position;
                beginNode.windowRect.position = dragPos + copyZoomCoordsOrigin;
            }
            if (endNode != null)
            {
                id = 1;
                endNode.windowRect.position -= copyZoomCoordsOrigin;
                Vector2 dragPos = GUI.Window(id, endNode.windowRect, DrawTagWindow, endNode.windowTitle).position;
                endNode.windowRect.position = dragPos + copyZoomCoordsOrigin;
            }

            for (int i = 0; i < nodeList.Count; i++)
            {
                id = i + 2;
                nodeList[i].windowRect.position -= copyZoomCoordsOrigin;
                Vector2 dragPos = GUI.Window(id, nodeList[i].windowRect, DrawNodeWindow, nodeList[i].windowTitle).position;
                nodeList[i].windowRect.position = dragPos + copyZoomCoordsOrigin;
            }

            for (int i = 0; i < actionNodeList.Count; i++)
            {
                id = i + 2 + nodeList.Count;
                actionNodeList[i].windowRect.position -= copyZoomCoordsOrigin;
                Vector2 dragPos = GUI.Window(id, actionNodeList[i].windowRect, DrawActionNodeWindow, actionNodeList[i].windowTitle).position;
                actionNodeList[i].windowRect.position = dragPos + copyZoomCoordsOrigin;
            }

            for (int i = 0; i < subDialogNodeList.Count; i++)
            {
                id = i + 2 + nodeList.Count + actionNodeList.Count;
                subDialogNodeList[i].windowRect.position -= copyZoomCoordsOrigin;
                Vector2 dragPos = GUI.Window(id, subDialogNodeList[i].windowRect, DrawSubDialogNodeList, subDialogNodeList[i].windowTitle).position;
                subDialogNodeList[i].windowRect.position = dragPos + copyZoomCoordsOrigin;
            }

            for (int i = 0; i < spawnNodeList.Count; i++)
            {
                id = i + 2 + nodeList.Count + actionNodeList.Count + subDialogNodeList.Count;
                GUI.Window(id, spawnNodeList[i].windowRect, DrawSpawnWindow, spawnNodeList[i].windowTitle);
            }

            EndWindows();

            //manage windows
            if (hasToPutToFront)
            {
                hasToPutToFront = false;
                foreach (BaseNode node in nodeToPutToFront)
                {
                    SetWindowToFront(node);
                }
                nodeToPutToFront.Clear();
            }
        }

        private void DrawTagWindow(int id)
        {
            if (id == 0)
            {
                beginNode.DrawWindow();
            }
            else
            {
                endNode.DrawWindow();
            }
            GUI.DragWindow();
        }

        private void DrawNodeWindow(int id)
        {
            bool doubleCheck = checkWindow<DialogNode>(id - 2, ref nodeList);

            if (doubleCheck)
            {
                nodeList[id - 2].DrawWindow();
                GUI.DragWindow();
            }
        }

        private void DrawActionNodeWindow(int id)
        {
            bool doubleCheck = checkWindow<ActionNode>(id - 2 - nodeList.Count, ref actionNodeList);

            if (doubleCheck)
            {
                actionNodeList[id - 2 - nodeList.Count].DrawWindow();
                GUI.DragWindow();
            }
        }

        private void DrawSubDialogNodeList(int id)
        {
            bool doubleCheck = checkWindow<SubDialogNode>(id - 2 - nodeList.Count - actionNodeList.Count, ref subDialogNodeList);

            if (doubleCheck)
            {
                subDialogNodeList[id - 2 - nodeList.Count - actionNodeList.Count].DrawWindow();
                GUI.DragWindow();
            }
        }

        private void DrawSpawnWindow(int id)
        {
            bool doubleCheck = checkWindow<SpawnNode>(id - 2 - nodeList.Count - actionNodeList.Count - subDialogNodeList.Count, ref spawnNodeList);

            if (doubleCheck)
            {
                spawnNodeList[id - 2 - nodeList.Count - actionNodeList.Count - subDialogNodeList.Count].DrawWindow();
            }
        }

        void DrawCurvesForLinkedNodes(BaseNode from, int answerNb, BaseNode end)
        {
            Color bezierColor = Color.gray;
            //if currently over some curves
            if ((!isInMenu || isInBezierMenu) && overlapingHitboxes.Count != 0 && currentHitbox != -1)
            {
                if (new KeyForCurve(from, end, answerNb).Compare(overlapingHitboxes[currentHitbox]))
                {
                    bezierColor = Color.red;
                }
            }
            DrawBezier(GetStartPosForBezier(from, answerNb), GetEndPosForBezier(end), bezierColor);
            if (end is HandleNode)
            {
                DrawCurvesForLinkedNodes(end, -1, (end as HandleNode).next);

                end.windowRect.position -= copyZoomCoordsOrigin;
                (end as HandleNode).DrawWindow();
                end.windowRect.position += copyZoomCoordsOrigin;
            }
            //Handles.DrawSelectionFrame
        }

        void DrawBezier(Vector3 startPosition, Vector3 endPosition, Color _bezierColor)
        {
            Vector3 startTangent = startPosition + Vector3.right * 50;
            Vector3 endTangent = endPosition + Vector3.left * 50;

            Handles.DrawBezier(startPosition, endPosition, startTangent, endTangent, _bezierColor, null, 5);
        }
        #endregion

        #region GetNode_GetID
        public BaseNode GetNodeForWindowID(int _id)
        {
            if (_id < 0)
            {
                Debug.LogError("ID is negative");
                return null;
            }
            else if (_id < 2)
            {
                if (_id == 0)
                {
                    return beginNode;
                }
                else
                {
                    return endNode;
                }
            }
            else if (_id < 2 + nodeList.Count)
            {
                return nodeList[_id - 2];
            }
            else if (_id < 2 + nodeList.Count + actionNodeList.Count)
            {
                return actionNodeList[_id - 2 - nodeList.Count];
            }
            else if (_id < 2 + nodeList.Count + actionNodeList.Count + subDialogNodeList.Count)
            {
                return subDialogNodeList[_id - 2 - nodeList.Count - actionNodeList.Count];
            }
            else
            {
                Debug.LogError("ID is too big");
                return null;
            }

        }

        //node == _node, doesnt work when maximizing then resizing window??
        public int GetWindowIDForNode(BaseNode _node)
        {
            int id = -1;
            if (_node is BeginDialogTagNode)
            {
                return 0;
            }
            if (_node is EndDialogTagNode)
            {
                return 1;
            }

            foreach (DialogNode node in nodeList)
            {
                if (node == _node)
                {
                    id = 2 + nodeList.IndexOf(node); //  bc ID given at window display
                    break;
                }
            }
            if (id != -1)
            {
                return id;
            }


            foreach (ActionNode node in actionNodeList)
            {
                if (node == _node)
                {
                    id = 2 + actionNodeList.IndexOf(node) + nodeList.Count; // +nodeList.Count bc ID given at window display
                    break;
                }
            }
            if (id != -1)
            {
                return id;
            }

            foreach (SubDialogNode node in subDialogNodeList)
            {
                if (node == _node)
                {
                    id = 2 + subDialogNodeList.IndexOf(node) + nodeList.Count + actionNodeList.Count; //  + nodeList.Count + actionNodeList.Count bc ID given at window display
                    break;
                }
            }
            if (id != -1)
            {
                return id;
            }

            foreach (SpawnNode node in spawnNodeList)
            {
                if (node == _node)
                {
                    id = 2 + spawnNodeList.IndexOf(node) + nodeList.Count + actionNodeList.Count + subDialogNodeList.Count; //  + nodeList.Count + actionNodeList.Count bc ID given at window display
                    break;
                }
            }
            if (id != -1)
            {
                return id;
            }



            if (id == -1)
            {
                //Debug.LogError("Can't find this node." + _node.windowTitle);
                //DestroyImmediate(_node);
            }

            return id;
        }

        public DialogNode GetDialogNodeForName(string _name)
        {
            List<DialogNode> temp = nodeList.Where(x => x.windowTitle == _name).ToList();
            return temp.Count > 0 ? temp[0] : null; //names are unique, so if there is a name in temp, there is only one
        }

        public SubDialogNode GetSubDialogNodeForName(string _name)
        {
            List<SubDialogNode> temp = subDialogNodeList.Where(x => x.dialog.dialogName == _name).ToList();
            return temp.Count > 0 ? temp[0] : null; //names are unique, so if there is a name in temp, there is only one
        }

        public bool GetRenameNodeForNode(DialogNode _dNode, out RenameNode renameNode)
        {
            foreach (ActionNode node in actionNodeList)
            {
                if ((node is RenameNode) && node.targetNode == _dNode)
                {
                    renameNode = node as RenameNode;
                    return true;
                }
            }
            renameNode = null;
            return false;
        }
        #endregion

        #region PutNodeToFront
        public void SetNodeToFront(BaseNode _node)
        {
            nodeToPutToFront.Add(_node);
            hasToPutToFront = true;
        }



        private void SetWindowToFront(BaseNode _node)
        {
            int id = GetWindowIDForNode(_node);

            if (id == -1)
            {
                return;
            }
            GUI.BringWindowToFront(id);

            //check if other nodes overlap
            /**foreach (DialogNode node in nodeList)
            {
                if (_node != node && _node.windowRect.Overlaps(node.windowRect))
                {
                    int id2 = GetWindowIDForNode(node);
                    GUI.BringWindowToBack(id2);
                }
            }
            foreach (ActionNode node in actionNodeList)
            {
                if (_node != node && _node.windowRect.Overlaps(node.windowRect))
                {
                    int id2 = GetWindowIDForNode(node);
                    GUI.BringWindowToBack(id2);
                }
            }*/

            if (_node is DialogNode)
            {
                //bring action nodes attached to this dialog node
                foreach (ActionNode actNode in actionNodeList)
                {
                    if (actNode.targetNode == _node)
                    {
                        GUI.BringWindowToFront(GetWindowIDForNode(actNode));
                    }
                }

                //bring spawn nodes overlaping with actions nodes attached to this dialog node
                foreach (ActionNode actNode in actionNodeList)
                {
                    if (actNode.targetNode == _node)
                    {
                        foreach (SpawnNode spNode in spawnNodeList)
                        {
                            if (actNode.windowRect.Overlaps(spNode.windowRect))
                            {
                                int id2 = GetWindowIDForNode(spNode);
                                GUI.BringWindowToFront(id2);
                            }
                        }
                    }
                }
            }
            else if (_node is ActionNode)
            {
                SetWindowToFront((_node as ActionNode).targetNode);
            }

            //bring spawn nodes overlaping with this dialog node
            foreach (SpawnNode spNode in spawnNodeList)
            {
                int id2 = GetWindowIDForNode(spNode);
                GUI.BringWindowToFront(id2);
            }

        }
        #endregion

        #region AjoutNodes
        public void AddSubDialog(Vector2 _pos)
        {
            SubDialogNode newSubDiaNode = new SubDialogNode(_pos);
            newSubDiaNode.windowTitle = "SubDialog: " + newSubDiaNode.dialog.dialogName;

            DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeInstanciated(DECtrlZInstance, newSubDiaNode, this));
            subDialogNodeList.Add(newSubDiaNode);
        }

        public void AddSubDialog(Vector2 _pos, Dialog _dialog)
        {
            SubDialogNode newSubDiaNode = new SubDialogNode(_pos, _dialog);
            newSubDiaNode.windowTitle = "SubDialog: " + newSubDiaNode.dialog.dialogName;

            DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeInstanciated(DECtrlZInstance, newSubDiaNode, this));
            subDialogNodeList.Add(newSubDiaNode);
        }

        public SubDialogNode AddSubDialogNoCtrlZ(SubDialogNode _toCopy)
        {
            SubDialogNode newSubDiaNode = new SubDialogNode(_toCopy);

            subDialogNodeList.Add(newSubDiaNode);

            return newSubDiaNode;
        }

        #region DialogNode
        public void AddDialogNode(Vector2 _pos)
        {
            DialogNode newDiaNode = new DialogNode(_pos);
            newDiaNode.windowTitle = GetNameForDialogNodeWindow("TextNode n: 1");

            DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeInstanciated(DECtrlZInstance, newDiaNode, this));
            nodeList.Add(newDiaNode);
        }

        public void AddDialogNode(Vector2 _pos, string _name)
        {
            DialogNode newDiaNode = new DialogNode(_pos);
            newDiaNode.windowTitle = _name;

            DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeInstanciated(DECtrlZInstance, newDiaNode, this));
            nodeList.Add(newDiaNode);
        }

        public void AddDialogNode(Vector2 _pos, DialogNode _toCopy)
        {
            DialogNode newDiaNode = new DialogNode(_pos, _toCopy);

            if (_toCopy.IsEmpty())
            {
                newDiaNode.windowTitle = GetNameForDialogNodeWindow("TextNode n: 1");
            }
            else if (_toCopy.windowTitle.LastIndexOf(" copy(1)") != -1)
            {
                newDiaNode.windowTitle = GetNameForDialogNodeWindow(_toCopy.windowTitle);
            }
            else
            {
                newDiaNode.windowTitle = GetNameForDialogNodeWindow(_toCopy.windowTitle + " copy(1)");
            }

            DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeInstanciated(DECtrlZInstance, newDiaNode, this));
            nodeList.Add(newDiaNode);
        }

        public void AddDialogNode(DialogNodeData _data)
        {
            DialogNode newDiaNode = new DialogNode(_data);

            DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeInstanciated(DECtrlZInstance, newDiaNode, this));
            nodeList.Add(newDiaNode);
        }

        public DialogNode AddDialogNodeNoCtrlZ(DialogNode _toCopy)
        {
            DialogNode newDiaNode = new DialogNode(_toCopy);

            nodeList.Add(newDiaNode);

            return newDiaNode;
        }
        #endregion

        #region ActionNode
        public RenameNode AddRenameNode(DialogNode _target)
        {
            RenameNode newRNode = new RenameNode(_target);

            actionNodeList.Add(newRNode);

            return newRNode;
        }

        public DialogText AddDialogTextNode(DialogNode _target)
        {
            DialogText newDTextNode = new DialogText(_target);

            actionNodeList.Add(newDTextNode);

            return newDTextNode;
        }

        public AnswerText AddAnswerText(DialogNode _target, int _answerNb)
        {
            AnswerText newAnsTextNode = new AnswerText(_target, _answerNb);

            actionNodeList.Add(newAnsTextNode);

            return newAnsTextNode;
        }
        #endregion

        #region SpawnNode
        public SpawnNode AddSpawnNode(Vector2 _pos, string _title, int _butIndex = 0)
        {
            SpawnNode newSpawNode = new SpawnNode(_pos, _title, _butIndex);

            spawnNodeList.Add(newSpawNode);

            return newSpawNode;
        }
        #endregion

        #region TagNode
        public BaseNode AddTagNode(Vector2 _pos, bool isBegin)
        {
            if (isBegin)
            {
                beginNode = new BeginDialogTagNode(_pos);
                return beginNode;
            }
            else
            {
                endNode = new EndDialogTagNode(_pos);
                return endNode;
            }
        }

        public BaseNode AddTagNode(DialogTagNodeData _data, bool isBegin)
        {
            return AddTagNode(_data.pos, isBegin);
        }
        #endregion

        public HandleNode AddHandleNode(Vector2 _pos, float _radius, BaseNode _bNodeA, BaseNode _bNodeB)
        {
            HandleNode newHandleNode = new HandleNode(_pos, _radius, _bNodeA, _bNodeB);

            DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeInstanciated(DECtrlZInstance, newHandleNode, this));
            handleNodeList.Add(newHandleNode);

            return newHandleNode;
        }
        #endregion

        void DeleteDialog(Dialog _toDel)
        {
            int previousIndex = DEInstance.loadedDialogs.IndexOf(_toDel);
            if (DEInstance.currentDialog == _toDel)
            {
                if (previousIndex > 0)
                {
                    DEInstance.currentDialog = DEInstance.loadedDialogs[previousIndex - 1];
                }
                else
                {
                    if (DEInstance.loadedDialogs.Count > 1)
                    {
                        DEInstance.currentDialog = DEInstance.loadedDialogs[1];
                    }
                    else
                    {
                        DEInstance.loadedDialogs.Add(new Dialog());
                        DEInstance.loadedDialogs[1].Initialize();
                        DEInstance.currentDialog = DEInstance.loadedDialogs[1];
                    }
                }
            }
            //if node is only in dialog and not opened, doesnt remote it
            if (previousIndex != -1)
            {
                DEInstance.loadedDialogs.RemoveAt(previousIndex);
            }
        }

        #region RemoveNodes
        public void RemoveDialogNode(DialogNode _dNode, bool _addCtrlZ = true)
        {
            RemoveAllConnectionsToThisNode(_dNode, _addCtrlZ);

            RemoveRenameNode(_dNode);
            RemoveDialogText(_dNode);
            RemoveAnswerText(_dNode);

            if (_addCtrlZ)
            {
                DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeDeleted(DECtrlZInstance, _dNode, this));
            }
            nodeList.Remove(_dNode);
            //DestroyImmediate(_dNode);
        }

        public void RemoveSubDialogNode(SubDialogNode _sdNode, bool _addCtrlZ = true)
        {
            RemoveAllConnectionsToThisNode(_sdNode, _addCtrlZ);

            //manage opened dialogs
            DeleteDialog(_sdNode.dialog);


            if (_addCtrlZ)
            {
                DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeDeleted(DECtrlZInstance, _sdNode, this));
            }
            subDialogNodeList.Remove(_sdNode);
        }

        public void RemovehandleNode(HandleNode _hNode, bool _addCtrlZ = true)
        {
            RemoveAllConnectionsToThisNode(_hNode, _addCtrlZ);
            if (_addCtrlZ)
            {
                DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeDeleted(DECtrlZInstance, _hNode, this));
            }
            RemoveHandle(_hNode);



            handleNodeList.Remove(_hNode);
        }

        /// <summary>
        /// remove this action node
        /// </summary>
        public void RemoveActionNode(ActionNode _aNode)
        {
            if (_aNode is RenameNode)
            {
                RemoveRenameNode(_aNode.targetNode);
            }
            else if (_aNode is DialogText)
            {
                RemoveDialogText(_aNode.targetNode);
            }
            else if (_aNode is AnswerText)
            {
                RemoveAnswerText(_aNode.targetNode);
            }
        }

        /// <summary>
        /// remove the rename node attached to _dNode, if it exists
        /// </summary>
        public void RemoveRenameNode(DialogNode _dNode)
        {
            RenameNode tempRNode;
            if (GetRenameNodeForNode(_dNode, out tempRNode))
            {
                actionNodeList.Remove(tempRNode);
                //DestroyImmediate(tempRNode);
            }
        }

        /// <summary>
        /// remove this rename node
        /// </summary>
        public void RemoveRenameNode(RenameNode _rNode)
        {
            actionNodeList.Remove(_rNode);
            //DestroyImmediate(_rNode);
        }

        /// <summary>
        /// remove the dialogText node attached to _dNode, if it exists
        /// </summary>
        public void RemoveDialogText(DialogNode _dNode)
        {
            foreach (ActionNode node in actionNodeList)
            {
                if (_dNode != null && node is DialogText && node.targetNode == _dNode)
                {
                    BaseNode aSupr = node;
                    actionNodeList.Remove(node);
                    //DestroyImmediate(aSupr);
                    break;
                }
            }
        }

        /// <summary>
        /// remove the Answertext attached to _dNode, if it exists
        /// </summary>
        /// <param name="_dNode"> TargetNode</param>
        /// <param name="_nb"> specify which answerText you want to delete, if left empty, will delete any</param>
        public void RemoveAnswerText(DialogNode _dNode, int _nb = -1)
        {
            foreach (ActionNode node in actionNodeList)
            {
                if (_dNode != null && node is AnswerText && node.targetNode == _dNode)
                {
                    if (_nb == -1)
                    {
                        BaseNode aSupr = node;
                        actionNodeList.Remove(node);
                        //DestroyImmediate(aSupr);
                        break;
                    }
                    else if ((node as AnswerText).targetAnswer == _nb)
                    {
                        BaseNode aSupr = node;
                        actionNodeList.Remove(node);
                        //DestroyImmediate(aSupr);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// remove this spawn node
        /// </summary>
        /// <param name="_spNode"></param>
        public void RemoveSpawnNode(SpawnNode _spNode)
        {
            spawnNodeList.Remove(_spNode);
            //DestroyImmediate(_spNode);
        }

        /// <summary>
        /// Remove every node in spawnNodeList
        /// </summary>
        public void clearSpawnList()
        {
            while (spawnNodeList.Count > 0)
            {
                //DestroyImmediate(spawnNodeList[0]);
                spawnNodeList.RemoveAt(0);
            }
            spawnNodeList.Clear();
        }
        #endregion

        void RemoveAllConnectionsToThisNode(BaseNode _bNode, bool _addCtrlZ = true)
        {
            if (!(_bNode is EndDialogTagNode) && !(_bNode is DialogNode) && !(_bNode is SubDialogNode) && !(_bNode is HandleNode))
            {
                return;
            }
            if (beginNode.firstDialogNode == _bNode)
            {
                if (_addCtrlZ)
                {
                    DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeDeLinked(beginNode, this, -1, _bNode));
                }
                RemoveHitBoxWhere(BeginNode, _bNode);
                beginNode.firstDialogNode = null;
            }
            foreach (DialogNode dNode in nodeList)
            {
                List<int> linkToDelete = dNode.connections.Where(x => x.Value == _bNode).Select(x => x.Key).ToList();
                foreach (int link in linkToDelete)
                {
                    if (_addCtrlZ)
                    {
                        DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeDeLinked(dNode, this, link, _bNode));
                    }
                    RemoveHitBoxWhere(dNode, _bNode, link);
                    dNode.connections.Remove(link);
                }
            }
            foreach (SubDialogNode sdNode in subDialogNodeList)
            {
                if (sdNode.connection == _bNode)
                {
                    if (_addCtrlZ)
                    {
                        DEInstance.dialogEditorCtrlZ.Add(new CtrlZNodeDeLinked(sdNode, this, -1, _bNode));
                    }
                    RemoveHitBoxWhere(sdNode, _bNode);
                    sdNode.connection = null;
                }
            }
        }

        void RemoveHitBoxWhere(BaseNode nodeA, BaseNode nodeB, int answer = -1)
        {
            KeyForCurve temp = curvesHitboxes.Where(x => x.Key.nodeA == nodeA && x.Key.nodeB == nodeB && x.Key.answerNb == answer).Select(x => x.Key).FirstOrDefault();
            if (temp != null)
            {
                curvesHitboxes.Remove(temp);
            }
        }

        #region NodeClicked
        bool DialogTagNodeClicked(Vector2 _mousePos, bool isBegin)
        {
            if (isBegin)
            {
                if (DEInstance.ContainsInWorld(beginNode.windowRect, _mousePos)) //on beginNode
                {
                    return true;
                }
            }
            else
            {
                if (DEInstance.ContainsInWorld(endNode.windowRect, _mousePos)) //on endNode
                {
                    return true;

                }
            }
            return false;
        }

        bool DialogNodeClicked(Vector2 _mousePos, out int index)
        {
            for (int i = 0; i < nodeList.Count; i++)
            {
                if (DEInstance.ContainsInWorld(nodeList[i].windowRect, _mousePos))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        bool ActionNodeClicked(Vector2 _mousePos, out int index)
        {
            for (int i = 0; i < actionNodeList.Count; i++)
            {
                if (DEInstance.ContainsInWorld(actionNodeList[i].windowRect, _mousePos))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        bool SubDialogNodeClicked(Vector2 _mousePos, out int index)
        {
            for (int i = 0; i < subDialogNodeList.Count; i++)
            {
                if (DEInstance.ContainsInWorld(subDialogNodeList[i].windowRect, _mousePos))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        bool HandleNodeClicked(Vector2 _mousePos, out int index)
        {
            for (int i = 0; i < handleNodeList.Count; i++)
            {
                if (DEInstance.ContainsInWorld(handleNodeList[i].windowRect, _mousePos))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        /// <summary>
        /// check all nodes to see if one is clicked,
        /// if its true, returns this node
        /// </summary>
        /// <param name="_mousePos"> </param>
        /// <param name="_bNode"> the clicked node</param>
        /// <returns></returns>
        bool IsNodeCLicked(Vector2 _mousePos, out BaseNode _bNode)
        {
            if (DialogTagNodeClicked(_mousePos, true))
            {
                _bNode = beginNode;
                return true;
            }
            if (DialogTagNodeClicked(_mousePos, false))
            {
                _bNode = endNode;
                return true;
            }
            int index;
            if (DialogNodeClicked(_mousePos, out index))
            {
                _bNode = nodeList[index];
                return true;
            }
            if (ActionNodeClicked(_mousePos, out index))
            {
                _bNode = actionNodeList[index];
                return true;
            }
            if (SubDialogNodeClicked(_mousePos, out index))
            {
                _bNode = subDialogNodeList[index];
                return true;
            }
            if (HandleNodeClicked(_mousePos, out index))
            {
                _bNode = handleNodeList[index];
                return true;
            }
            _bNode = null;
            return false;
        }
        #endregion

        #region StringsGestion
        string GetStringForBaseNode(BaseNode _bNode)
        {
            if (_bNode is RenameNode)
            {
                return "Rename node";
            }
            else if (_bNode is DialogText)
            {
                return "Dialog text";
            }
            else if (_bNode is AnswerText)
            {
                return "Answer text";
            }
            else
            {
                return "Base node";
            }
        }

        string GetNameForDialogNodeWindow(string _name)
        {
            foreach (BaseNode node in nodeList)
            {
                if (node.windowTitle == _name)
                {
                    int dialogNodeIndex = _name.LastIndexOf("TextNode n:");
                    int index = _name.LastIndexOf("copy(");

                    if (dialogNodeIndex != -1 && index == -1)
                    {
                        dialogNodeIndex += "TextNode n:".Length;
                        string tempStr = _name.Substring(dialogNodeIndex);
                        int copyNb = int.Parse(tempStr);
                        string name = _name.Substring(0, dialogNodeIndex);
                        name += (copyNb + 1) + "";

                        return GetNameForDialogNodeWindow(name);
                    }
                    else
                    {
                        if (index != -1)
                        {
                            index += "copy(".Length;
                            int index2 = _name.LastIndexOf(")");

                            string tempStr = _name.Substring(index, index2 - index);
                            int copyNb = int.Parse(tempStr);
                            string name = _name.Substring(0, index);
                            name += (copyNb + 1) + ")";
                            return GetNameForDialogNodeWindow(name);
                        }
                        else
                        {
                            return GetNameForDialogNodeWindow(_name + " copy(1)");
                        }
                    }
                }
            }
            return _name;
        }

        #endregion

        public void SetLastDrawnWindow(BaseNode _node)
        {
            if (Event.current.type == EventType.Repaint)
            {
                lastDrawnWindow = GetWindowIDForNode(_node);
                if (lastDrawnWindow < 0)
                {
                    Debug.LogError("lastDrawn Window ID wad negative.");
                    lastDrawnWindow = 0;
                }
            }
        }

        private bool checkWindow<T>(int _id, ref List<T> _list) where T : BaseNode
        {
            try
            {
                string name = _list[_id].windowTitle;
                return true;
            }
            catch (Exception)
            {
                Debug.Log("Shit Happened!");
                return false;
            }
        }

        private string GetDialogNameFromPath(string _savePath)
        {
            int posSlash = _savePath.LastIndexOf("/") + 1;
            int posDotJSon = _savePath.LastIndexOf(".json");
            return _savePath.Substring(posSlash, posDotJSon - posSlash);
        }

        #region Json
        public void UseSavedData(DialogData _save)
        {
            Initialize();

            dialogName = _save.dialogName;
            AddTagNode(_save.tagNodes[0], true);
            AddTagNode(_save.tagNodes[1], false);

            int nbNode = _save.nodesData.ToList().Count;
            for (int i = 0; i < nbNode; i++)
            {
                AddDialogNode(_save.nodesData[i]);
            }
            int nbSubNode = _save.subNodesData.ToList().Count;

            for (int i = 0; i < nbSubNode; i++)
            {
                Dialog temp = new Dialog();
                temp.UseSavedData(_save.subNodesData[i]);
                AddSubDialog(_save.posSubNodesData[i], temp);
            }

            SetNodeConnections(_save);
        }

        public void SetNodeConnections(DialogData _save)
        {
            int nbNode = _save.nodesData.ToList().Count;
            beginNode.firstDialogNode = GetDialogNodeForName(_save.tagNodes[0].connection);

            for (int i = 0; i < nbNode; i++)
            {
                for (int j = 0; j < nodeList[i].nbOfAnswers; j++)
                {
                    if (_save.nodesData[i].connections[j] != "")
                    {
                        if (_save.nodesData[i].connections[j] == "EndNode")
                        {
                            nodeList[i].connections.Add(j, endNode);
                        }
                        else if (_save.nodesData[i].connections[j].LastIndexOf("SUB") != -1)
                        {
                            string tempName = _save.nodesData[i].connections[j].Substring("SUB".Count() + 1);
                            nodeList[i].connections.Add(j, GetSubDialogNodeForName(tempName));
                        }
                        else
                        {
                            nodeList[i].connections.Add(j, GetDialogNodeForName(_save.nodesData[i].connections[j]));
                        }
                    }
                }
            }

            int nbSubNode = _save.subNodesData.ToList().Count;
            for (int i = 0; i < nbSubNode; i++)
            {
                if (_save.connections[i] != "")
                {
                    if (_save.connections[i] == "EndNode")
                    {
                        subDialogNodeList[i].connection = endNode;
                    }
                    else if (_save.connections[i].LastIndexOf("SUB") != -1)
                    {
                        string tempName = _save.connections[i].Substring("SUB".Count() + 1);
                        subDialogNodeList[i].connection = GetSubDialogNodeForName(tempName);
                    }
                    else
                    {
                        subDialogNodeList[i].connection = GetDialogNodeForName(_save.connections[i]);
                    }
                }
            }

        }
        #endregion

    }
}

#endif

