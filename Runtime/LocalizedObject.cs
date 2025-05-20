using System;
using UnityEngine;

namespace RecognX
{
    [Serializable]
    public struct LocalizedObject
    {
        public string className;
        public Vector3 position;
        public LocalizedObject(string name, Vector3 pos)
        {
                className = name;
                position = pos;
        }
    }
}