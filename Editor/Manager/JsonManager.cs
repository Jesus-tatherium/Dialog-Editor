
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace DialogEditor
{
    [System.Serializable]
    public class DialogEditorData
    {
        public float zoomScale;
        public Vector2 zoomCoordOrigin;

        public List<DialogData> loadedDialogData = new List<DialogData>();
        public int currentDialogData;


        public DialogEditorData(DialogEditor _editor)
        {
            zoomScale = _editor.zoomScale;
            zoomCoordOrigin = _editor.zoomCoordsOrigin;

            for (int i = 0; i < _editor.loadedDialogs.Count; i++)
            {
                loadedDialogData.Add(new DialogData(_editor.loadedDialogs[i]));
                if (_editor.loadedDialogs[i] == _editor.currentDialog)
                {
                    currentDialogData = i;

                }
            }
        }

    }

    [System.Serializable]
    public class DialogData
    {
        public string dialogName;
        public DialogTagNodeData[] tagNodes = new DialogTagNodeData[2];
        public DialogNodeData[] nodesData;
        public DialogData[] subNodesData;

        public Vector2[] posSubNodesData;
        public string[] connections;

        public DialogData(Dialog _editor)
        {
            dialogName = _editor.dialogName;
            tagNodes[0] = new DialogTagNodeData(_editor.BeginNode);
            tagNodes[1] = new DialogTagNodeData(_editor.EndNode);

            nodesData = new DialogNodeData[_editor.NodeList.Count];
            for (int i = 0; i < _editor.NodeList.Count; i++)
            {
                nodesData[i] = new DialogNodeData(_editor.NodeList[i]);
            }

            subNodesData = new DialogData[_editor.SubDialogNodeList.Count];
            posSubNodesData = new Vector2[_editor.SubDialogNodeList.Count];
            connections = new string[_editor.SubDialogNodeList.Count];

            for (int i = 0; i < _editor.SubDialogNodeList.Count; i++)
            {
                subNodesData[i] = new DialogData(_editor.SubDialogNodeList[i].dialog);
            }
            for (int i = 0; i < _editor.SubDialogNodeList.Count; i++)
            {
                posSubNodesData[i] = _editor.SubDialogNodeList[i].windowRect.position;

                if (_editor.SubDialogNodeList[i].connection is EndDialogTagNode)
                {
                    connections[i] = "EndNode";
                }
                else if (_editor.SubDialogNodeList[i].connection is SubDialogNode)
                {
                    connections[i] = "SUB " + (_editor.SubDialogNodeList[i].connection as SubDialogNode).dialog.dialogName;
                }
                else
                {
                    connections[i] = _editor.SubDialogNodeList[i].connection.windowTitle;
                }
            }
        }

    }

    [System.Serializable]
    public class DialogTagNodeData
    {
        public Vector2 pos;
        public string connection;

        public DialogTagNodeData(DialogTagNode _dtNode)
        {
            pos = _dtNode.windowRect.position;

            if (_dtNode is BeginDialogTagNode)
            {
                if ((_dtNode as BeginDialogTagNode).firstDialogNode != null)
                {
                    connection = (_dtNode as BeginDialogTagNode).firstDialogNode.windowTitle;
                }
            }
        }
    }

    [System.Serializable]
    public class DialogNodeData
    {
        public string name;
        public Vector2 pos;

        public string text;
        public int nbAnswers;
        public string[] answers;
        public string[] connections;

        public DialogNodeData(DialogNode _dNode)
        {
            name = _dNode.windowTitle;
            pos = _dNode.windowRect.position;

            text = _dNode.text;
            nbAnswers = _dNode.nbOfAnswers;
            answers = _dNode.answers.ToArray();

            connections = new string[_dNode.nbOfAnswers];
            for (int i = 0; i < _dNode.nbOfAnswers; i++)
            {
                if (_dNode.connections.ContainsKey(i))
                {
                    if (_dNode.connections[i] is EndDialogTagNode)
                    {
                        connections[i] = "EndNode";
                    }
                    else if (_dNode.connections[i] is SubDialogNode)
                    {
                        connections[i] = "SUB " + (_dNode.connections[i] as SubDialogNode).dialog.dialogName;
                    }
                    else
                    {
                        connections[i] = _dNode.connections[i].windowTitle;
                    }
                }
            }
        }
    }

    public class JsonManager
    {
        //static string path = "Assets/Editor/Saves/";
        static StreamWriter writer;
        static StreamReader reader;

        public static void SaveDataDialogEditor(string _path)
        {
            DialogEditorData data = new DialogEditorData(DialogEditor.Instance);

            string completePath = _path;

            if (File.Exists(completePath))
            {
                File.Delete(_path);
                AssetDatabase.Refresh();
            }
            writer = new StreamWriter(_path, false);
            writer.Write(JsonUtility.ToJson(data));
            writer.Close();
        }

        public static DialogEditorData GetDataDialogEditor(string _path)
        {
            //reader = new StreamReader(path + _fileName);
            reader = new StreamReader(_path);
            string JsonString = reader.ReadToEnd();
            DialogEditorData data = JsonUtility.FromJson<DialogEditorData>(JsonString);
            reader.Close();
            return data;
        }

        public static void SaveDataDialog(Dialog _dialog, string _path)
        {
            DialogData data = new DialogData(_dialog);

            string completePath = _path;

            if (File.Exists(completePath))
            {
                File.Delete(_path);
                AssetDatabase.Refresh();
            }
            writer = new StreamWriter(_path, false);
            writer.Write(JsonUtility.ToJson(data));
            writer.Close();
        }

        public static DialogData GetDataDialog(string _path)
        {
            //reader = new StreamReader(path + _fileName);
            reader = new StreamReader(_path);
            string JsonString = reader.ReadToEnd();
            DialogData data = JsonUtility.FromJson<DialogData>(JsonString);
            reader.Close();
            return data;
        }

        public static void JsonReshape(string _jsonData) // todo if has time ( enhances the .json file to be more readable )
        {
            int brackNb = 0;
            bool inArray = false;

            for (int i = 0; i < _jsonData.Length; i++)
            {
                if (!inArray)
                {
                }
                if (i < _jsonData.Length && _jsonData[i] == '{')
                {
                    brackNb++;
                    _jsonData.Insert(i, "\n");
                    i += 2;
                    _jsonData.Insert(i, "\n");
                    i++;
                    InsertTab(_jsonData, i, brackNb);
                }
            }
        }

        static void InsertTab(string _string, int _index, int _nb)
        {
            for (int i = 0; i < _nb; i++)
            {
                _string.Insert(_index, "\t");
            }
        }
    }
}