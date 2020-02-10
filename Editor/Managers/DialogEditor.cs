#if UNITY_EDITOR

using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.Events;
using UnityEngine;
using UnityEditor.ShortcutManagement;
using UnityEditor;
using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

//int fCount = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Length; get number of files in a directory
namespace DialogEditor
{
    public class DialogEditor : EditorWindow
    {
        static DialogEditor instance;
        public static DialogEditor Instance
        {
            get => instance;
        }

        public DialogEditorCtrlZ dialogEditorCtrlZ;

        static bool isVisible = false;

        private Texture gridTex;
        private Texture uniTex;

        public delegate void EventDelegate(Event e);

        #region DoubleClick
        event EventDelegate onDoubleClick;

        static float doubleClickThreshold = 0.2f;
        float doubleClickTimer = 0.0f;
        int doubleClick = 0;
        #endregion

        #region MouseHold
        event EventDelegate onMouseHold;

        static float mouseHoldThreshold = 0.1f;
        bool mouseHold = false;
        float mouseHoldTimer = 0.0f;
        #endregion

        #region MouseIdle
        event EventDelegate onMouseIdle;
        event EventDelegate onMouseQuitIdle;

        static float mouseIdleThreshold = 0.1f;
        Vector2? mouseIdlePos = null;
        bool idleUsed = false;
        bool mouseIdle = false;
        public bool MouseIdle { get => mouseIdle; set => mouseIdle = value; }
        float mouseIdleTimer = 0.0f;
        #endregion

        #region KeyBoardStopInputs
        event EventDelegate onKeyBoardStopInputs;

        static float KeyBoardStopInputsThreshold = 0.5f;
        bool keyBoardInputs = false;
        float keyBoardInputsTimer = 0.0f;
        #endregion

        #region ExecuteLast
        Action execLastFrame;
        bool hasToExecLastFrame = false;
        #endregion

        #region WorldRelated
        float zoomMin = 0.25f;
        float zoomMax = 4f;

        public float zoomScale;
        Rect zoomArea;
        public Vector2 zoomCoordsOrigin;
        Vector2 prevZoomCoordOrigin;

        Vector2 ConvertScreenCoordsToZoomCoords(Vector2 screenCoords)
        {
            return (screenCoords - zoomArea.TopLeft()) / zoomScale + zoomCoordsOrigin;
        }

        public Vector2 GetWorldPos(Vector2 screenPos)
        {
            return screenPos * (1 / zoomScale) + zoomCoordsOrigin;
        }

        public bool ContainsInWorld(Rect _baseRect, Vector2 _mousePos)
        {
            Rect worldRect = _baseRect.ScaleNodeWindow(zoomScale, _baseRect.TopLeft());
            worldRect.position -= zoomCoordsOrigin * zoomScale;


            if (worldRect.Contains(_mousePos))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Shortcuts
        //https://issuetracker.unity3d.com/issues/disable-editor-keyboard-shortcuts-while-playing NSXDAVID
        bool alreadySet = false;


        private const string emptyProfile = "An Empty Profile";
        private static string previousProfile = "";

        private static void CreateEmptyProfile()
        {
            try
            {
                previousProfile = ShortcutManager.instance.activeProfileId;
                ShortcutManager.instance.CreateProfile(emptyProfile);
            }
            catch (Exception)
            {
                return;
            }

            ShortcutManager.instance.activeProfileId = emptyProfile;
            foreach (var pid in ShortcutManager.instance.GetAvailableShortcutIds())
            {
                ShortcutManager.instance.RebindShortcut(pid, ShortcutBinding.empty);
            }
        }
        #endregion

        public List<Dialog> loadedDialogs = new List<Dialog>();
        public Dialog currentDialog;

        public string savePath = "";

        [MenuItem("DialogEditor/DialogWindow")]
        static void ShowEditor()
        {
            DialogEditor editor = (DialogEditor)EditorWindow.GetWindow(typeof(DialogEditor), false, "Dialog Editor");
            editor.minSize = new Vector2(300, 300);
            editor.autoRepaintOnSceneChange = true;
        }


        public void Initialize(bool isSave = false)
        {
            if (gridTex == null)
            {
                gridTex = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/Texture/GridTexture.png");
            }
            if (uniTex == null)
            {
                uniTex = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/Texture/UniTexture.png");
            }

            zoomScale = 1.0f;
            zoomCoordsOrigin = Vector2.zero;

            dialogEditorCtrlZ = new DialogEditorCtrlZ();

            loadedDialogs.Clear();
            currentDialog = null;
            if (!isSave)
            {
                loadedDialogs.Add(new Dialog());
                currentDialog = loadedDialogs[0];
                currentDialog.Initialize();
            }

        }

        public void OnEnable()
        {
            if (instance == null)
            {
                instance = this;
                Initialize();
            }
            CreateEmptyProfile();

            isVisible = true;
        }

        public void OnDisable()
        {
            isVisible = false;
            ShortcutManager.instance.activeProfileId = previousProfile;
        }

        public void OnDestroy()
        {
            ShortcutManager.instance.activeProfileId = previousProfile;
        }

        public void OnLostFocus()
        {
            ShortcutManager.instance.activeProfileId = previousProfile;
        }

        private void OnGUI()
        {
            #region ShortCutGestion
            if (EditorWindow.focusedWindow != this)
            {
                if (!alreadySet)
                {
                    alreadySet = true;
                    ShortcutManager.instance.activeProfileId = previousProfile;
                    String timeStamp = GetTimestamp(DateTime.Now);
                }
            }
            else
            {
                if (!isVisible)
                {
                    isVisible = true;
                }
                if (alreadySet)
                {
                    alreadySet = false;
                    ShortcutManager.instance.activeProfileId = emptyProfile;
                }
            }
            #endregion

            if (isVisible)
            {
                GUI.DrawTexture(new Rect(0, 0, 1920, 1080), uniTex, ScaleMode.StretchToFill);

                Instance.zoomArea = new Rect(0, 0, Instance.position.width, Instance.position.height);

                Event e = Event.current;

                if (mouseOverWindow == this)
                {
                    Inputs(e);
                    ProcessDoubleClick(e);
                    ProcessMouseDown(e);
                    ProcessMouseIdle(e);
                    ProcesskeyBoardInputs(e);
                }
                else
                {
                    ResetDoubleClick();
                    ResetMouseHold();
                    ResetMouseIdle();
                }


                EditorZoomArea.Begin(zoomScale, zoomArea);
                /** //test asset cycliques
                Vector2 topLeftPoint = ConvertScreenCoordsToZoomCoords(new Vector2(0, 0));
                Vector2 botRightPoint = ConvertScreenCoordsToZoomCoords(new Vector2(Instance.position.width, Instance.position.height));

                //background
                int xMinTextures = GetNextIntegerForRenderTexture(topLeftPoint.x / zoomScale / 1920, true);
                int yMinTextures = GetNextIntegerForRenderTexture(topLeftPoint.y / zoomScale / 1080, true);

                int xMaxTextures = GetNextIntegerForRenderTexture(botRightPoint.x / zoomScale / 1920);
                int yMaxTextures = GetNextIntegerForRenderTexture(botRightPoint.y / zoomScale / 1080);

                for (int i = xMinTextures; i < xMaxTextures + 1; i++)
                {
                    for (int j = yMinTextures; j < yMaxTextures + 1; j++)
                    {
                        Vector2 position = new Vector2(1920 * i, 1080 * j);
                        position -= zoomCoordsOrigin;
                        GUI.DrawTexture(new Rect(position.x, position.y, 1920, 1080), gridTex, ScaleMode.StretchToFill);

                    }
                }


                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
                {
                    Debug.Log("topleft:" + topLeftPoint);
                    Debug.Log("botRight:" + botRightPoint);

                    Debug.Log("min size: x: " + xMinTextures + "  y: " + yMinTextures);
                    Debug.Log("max size: x: " + xMaxTextures + "  y: " + yMaxTextures);

                    e.Use();
                }*/

                currentDialog.UpdateAndDraw(e);
                EditorZoomArea.End();

                GUIStyle style = new GUIStyle(GUI.skin.button);
                GUILayout.BeginHorizontal();
                for (int i = 0; i < loadedDialogs.Count; i++)
                {
                    int tempWidth = (int)WidthOfText(loadedDialogs[i].dialogName) + 20;
                    bool isCurrent = loadedDialogs[i] == currentDialog;

                    if (isCurrent)
                    {
                        GUILayout.Toggle(isCurrent, loadedDialogs[i].dialogName, style, GUILayout.Width(tempWidth));
                    }
                    else
                    {
                        if (GUILayout.Toggle(false, loadedDialogs[i].dialogName, style, GUILayout.Width(tempWidth)))
                        {
                            currentDialog = loadedDialogs[i];
                        }
                    }

                }
                GUILayout.EndHorizontal();
            }
            else
            {
                ResetDoubleClick();
                ResetMouseHold();
                ResetMouseIdle();
            }

        }

        private void Update()
        {
            Repaint(); // if not called, OnGUI refresh too low
            if (hasToExecLastFrame)
            {
                hasToExecLastFrame = false;
                execLastFrame();
                execLastFrame = () => { };
            }

        }

        #region Inputs
        #region ProcessDoubleClick
        private void ProcessDoubleClick(Event e)
        {
            if (doubleClick != 0 && doubleClickTimer + doubleClickThreshold < Time.realtimeSinceStartup)
            {
                ResetDoubleClick();
            }
            if (doubleClick >= 2)
            {
                //delegate
                onDoubleClick(e);
                ResetDoubleClick();
            }
        }

        private void ResetDoubleClick()
        {
            doubleClickTimer = 0.0f;
            doubleClick = 0;
        }
        #endregion

        #region ProcessMouseDown
        private void ProcessMouseDown(Event e)
        {
            if (!mouseHold)
            {
                ResetMouseHold();
            }
            if (mouseHold && mouseHoldTimer + mouseHoldThreshold > Time.realtimeSinceStartup)
            {
                //delegate
                onMouseHold(e);
                ResetMouseHold();
            }
        }

        private void ResetMouseHold()
        {
            mouseHold = false;
            mouseHoldTimer = 0.0f;
        }
        #endregion

        #region ProcessMouseIdle
        private void ProcessMouseIdle(Event e)
        {
            if (mouseIdlePos == null)
            {
                mouseIdlePos = e.mousePosition;
                mouseIdleTimer = Time.realtimeSinceStartup;
            }
            else
            {
                if (mouseIdlePos == e.mousePosition)
                {
                    if (!idleUsed && mouseIdleTimer + mouseIdleThreshold < Time.realtimeSinceStartup)
                    {
                        mouseIdle = true;
                        idleUsed = true;
                        onMouseIdle(e);
                    }
                }
                else
                {
                    if (idleUsed)
                    {
                        onMouseQuitIdle(e);
                    }
                    mouseIdle = false;
                    ResetMouseIdle();
                }
            }
        }

        private void ResetMouseIdle()
        {
            mouseIdlePos = null;
            mouseIdleTimer = 0;
            idleUsed = false;
        }
        #endregion

        #region ProcessKeyboardInput
        private void ProcesskeyBoardInputs(Event e)
        {
            if (!keyBoardInputs && keyBoardInputsTimer != 0.0f || keyBoardInputs && keyBoardInputsTimer + KeyBoardStopInputsThreshold < Time.realtimeSinceStartup)
            {
                //Debug.Log("OnKeyboard " + keyBoardInputsTimer);
                onKeyBoardStopInputs(e);
                ResetkeyBoardInputs();
            }

        }

        private void ResetkeyBoardInputs()
        {
            keyBoardInputs = false;
            keyBoardInputsTimer = 0.0f;
        }
        #endregion

        public static void KeyboardInput(Event e, KeyCode _key, Action _action)
        {
            if (e.keyCode == _key)
            {
                _action();

                e.Use();
            }
        }

        void TestJson() // todo if has time ( enhances the .json file to be more readable )
        {
            string a = "{\"zoomScale\":0.7599999904632568,\"zoomCoordOrigin\":{\"x\":-109.05267333984375,\"y\":-161.26324462890626},\"currentDialogData\":{\"tagNodes\":[{\"pos\":{";
            JsonManager.JsonReshape(a);
        }

        void TestA()
        {
            BlackBoard a = new BlackBoard();
            a.GetAllTypes();
        }

        private void Inputs(Event e)
        {
            if (e.type == EventType.KeyDown)
            {
                //if (e.keyCode == KeyCode.Delete)
                //{
                //    //recup dialog name focused //DestroyImmediate(getN)

                //    e.Use();
                //}
                KeyboardInput(e, KeyCode.A, TestA);

                //shortcut with SHIFT
                if (e.modifiers == EventModifiers.Control)
                {
                    KeyboardInput(e, KeyCode.Z, () =>
                    {
                        dialogEditorCtrlZ.Backward();
                        GUI.FocusControl(null); // if dont focus null, doesnt get the changes of string 
                        string name = GUI.GetNameOfFocusedControl(); //=> no control named, cannot refocus after CtrlZ
                        e.Use();
                    });
                    KeyboardInput(e, KeyCode.Y, () =>
                    {
                        dialogEditorCtrlZ.Forward();
                        GUI.FocusControl(null);
                        e.Use();
                    });

                    KeyboardInput(e, KeyCode.Space, () => { FocusOnNode(currentDialog.BeginNode); e.Use(); });
                    /*KeyboardInput(e, KeyCode.R, () => { Initialize(); e.Use(); });
                    KeyboardInput(e, KeyCode.S, () => { JsonManager.SaveDataDialogEditor("test.json"); e.Use(); });
                    KeyboardInput(e, KeyCode.L, () =>
                    {
                        DialogEditorData data = JsonManager.GetDataDialogEditor("test.json");
                        UseSavedData(data);
                        e.Use();
                    });*/

                }
                else if (e.modifiers == EventModifiers.None)
                {
                    currentDialog.KeyUse(e);
                    //manageKeyboard
                    keyBoardInputs = true;
                    keyBoardInputsTimer = Time.realtimeSinceStartup;
                }
            }

            #region MovementAndScroll
            //drag window with scrollClick
            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                prevZoomCoordOrigin = zoomCoordsOrigin;

                Vector2 delta = Event.current.delta;
                delta /= zoomScale;
                zoomCoordsOrigin = prevZoomCoordOrigin - delta;


                e.Use();
            }

            // allow to zoom with mouse as center
            // source: http://martinecker.com/martincodes/unity-editor-window-zooming/
            if (e.type == EventType.ScrollWheel)
            {
                Vector2 screenCoordsMousePos = Event.current.mousePosition;
                Vector2 delta = Event.current.delta;
                Vector2 zoomCoordsMousePos = ConvertScreenCoordsToZoomCoords(screenCoordsMousePos);
                float zoomDelta = -delta.y / 50.0f;
                float oldZoom = zoomScale;
                zoomScale += zoomDelta;
                zoomScale = Mathf.Clamp(zoomScale, zoomMin, zoomMax);
                zoomCoordsOrigin += (zoomCoordsMousePos - zoomCoordsOrigin) - (oldZoom / zoomScale) * (zoomCoordsMousePos - zoomCoordsOrigin);
                e.Use();
            }
            #endregion

            if (e.type == EventType.MouseDown || e.type == EventType.MouseUp)
            {
                //Debug.Log("Input false");
                keyBoardInputs = false;
            }
            //mouse click
            if (e.type == EventType.MouseDown)
            {
                Vector2 mouseClickPos = e.mousePosition;
                GUI.FocusControl(null);

                //left click
                if (e.button == 0)
                {
                    mouseHold = true;
                    mouseHoldTimer = Time.realtimeSinceStartup;
                    //double click
                    if (doubleClick == 0)
                    {
                        doubleClickTimer = Time.realtimeSinceStartup;
                        doubleClick++;
                    }
                    else
                    {
                        doubleClick++;
                    }

                    currentDialog.LeftClick(e);
                }
                //right click
                else if (e.button == 1)
                {
                    currentDialog.RightClick(e);
                }
            }
            //mouse release
            else if (e.type == EventType.MouseUp)
            {
                //left click
                if (e.button == 0)
                {
                    ResetMouseHold();
                    currentDialog.LeftRelease(e);
                }
                //right click
                else if (e.button == 1)
                {
                    currentDialog.RightRelease(e);
                }
            }
        }
        #endregion

        #region Focus
        public void FocusOnPos(Vector2 _pos)
        {
            zoomCoordsOrigin = _pos - (Instance.position.size / 2) / zoomScale;
        }

        public void FocusOnNode(BaseNode _node)
        {
            Vector2 pos = _node.windowRect.position + (_node.windowRect.size / 2);
            FocusOnPos(pos);
        }

        public void FocusOnDialogue(Dialog _dialog)
        {
            if (!loadedDialogs.Contains(_dialog))
            {
                loadedDialogs.Add(_dialog);
            }
            currentDialog = _dialog;
        }

        public void FocusOnCtrlZAction(CtrlZAction _action)
        {
            FocusOnDialogue(_action.targetDialog);
            Vector2 targetPos = _action.targetNode.windowRect.position + _action.targetNode.windowRect.size / 2;
            Rect view = new Rect(zoomCoordsOrigin, Instance.position.size);
            if (!view.Contains(targetPos))
            {
                FocusOnPos(targetPos);
            }
        }
        #endregion

        #region AddToEvent
        public void AddToExecuteLast(Action _act)
        {
            hasToExecLastFrame = true;
            execLastFrame += _act;
        }

        public void AddToOnDoubleClick(EventDelegate _act)
        {
            onDoubleClick += _act;
        }

        public void AddToOnMouseHold(EventDelegate _act)
        {
            onMouseHold += _act;
        }

        public void AddToOnMouseIdle(EventDelegate _act)
        {
            onMouseIdle += _act;
        }

        public void AddToOnMouseQuitIdle(EventDelegate _act)
        {
            onMouseQuitIdle += _act;
        }

        public void AddToOnKeyBoardStopInputs(EventDelegate _act)
        {
            onKeyBoardStopInputs += _act;
        }
        #endregion

        #region Json
        public void UseSavedData(DialogEditorData _save)
        {
            Initialize(true);
            zoomScale = _save.zoomScale;
            zoomCoordsOrigin = _save.zoomCoordOrigin;


            for (int i = 0; i < _save.loadedDialogData.Count; i++)
            {
                loadedDialogs.Add(new Dialog());
                loadedDialogs[i].UseSavedData(_save.loadedDialogData[i]);
            }
            currentDialog = loadedDialogs[_save.currentDialogData];

            //relink subdialog nodes to ope ndialogs
            foreach (Dialog dialog in loadedDialogs)
            {
                foreach (SubDialogNode sdDialog in dialog.SubDialogNodeList)
                {
                    sdDialog.dialog = loadedDialogs.Where(x => x.dialogName == sdDialog.dialog.dialogName).First();
                }
            }


        }
        #endregion

        public static float WidthOfText(string _text)
        {
            GUIContent content = new GUIContent(_text);
            GUIStyle style = GUI.skin.box;
            return style.CalcSize(content).x;
        }

        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyy/MM/dd_HH:mm:ss:ffff");
        }


        /**
        public static int GetNextIntegerForRenderTexture(float _f, bool isMin = false)
        {
            bool isPositive = true;
            int outPut;
            if (_f < 0.0f)
            {
                isPositive = false;
            }
            //round to next int
            outPut = Mathf.Abs((int)_f);
            if (Mathf.Abs(_f) - Mathf.Abs((int)_f) > 0.000001f)
            {
                outPut += 1;
            }

            if (!isPositive)
            {
                outPut = -outPut;
            }

            return outPut;
        }
            */
    }
}
#endif
