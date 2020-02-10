using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DialogEditor
{
    public class HandleNode : BaseNode
    {
        public BaseNode next;
        public List<BaseNode> prev = new List<BaseNode>();

        float radius;

        public HandleNode()
        {
        }

        public HandleNode(Vector2 _pos, float _radius) : base(_pos, new Vector2(_radius * 2, _radius * 2), false)
        {
            radius = _radius;
        }

        public HandleNode(Vector2 _pos, float _radius, BaseNode _prev, BaseNode _next) : base(_pos, new Vector2(_radius * 2, _radius * 2), false)
        {
            radius = _radius;
            prev.Add(_prev);
            next = _next;
        }

        public override void DrawWindow()
        {
            Handles.DrawSolidDisc(windowRect.position + windowRect.size / 2, Vector3.forward * -1, radius);
        }

        public Vector2 GetCenterLeft()
        {
            return windowRect.position + windowRect.size / 2 - new Vector2(windowRect.size.x / 2, 0) - DialogEditor.Instance.zoomCoordsOrigin;
        }

        public Vector2 GetCenterRight()
        {
            return windowRect.position + windowRect.size / 2 + new Vector2(windowRect.size.x / 2, 0) - DialogEditor.Instance.zoomCoordsOrigin;
        }
    }
}
