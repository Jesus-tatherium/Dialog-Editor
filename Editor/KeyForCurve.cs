using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogEditor
{
    public class KeyForCurve
    {
        public BaseNode nodeA;
        public BaseNode nodeB;
        public int answerNb;


        public KeyForCurve(BaseNode _nodeA, BaseNode _nodeB)
        {
            nodeA = _nodeA;
            nodeB = _nodeB;
            answerNb = -1;
        }

        public KeyForCurve(BaseNode _nodeA, BaseNode _nodeB, int _ansNb)
        {
            nodeA = _nodeA;
            nodeB = _nodeB;
            answerNb = _ansNb;
        }

        public bool Compare(KeyForCurve other)
        {
            if (answerNb != other.answerNb)
            {
                return false;
            }
            if (nodeA != other.nodeA)
            {
                return false;
            }
            if (nodeB != other.nodeB)
            {
                return false;
            }
            return true;
        }

        public KeyForCurve Copy()
        {
            return new KeyForCurve(nodeA, nodeB, answerNb);
        }
    }
}
