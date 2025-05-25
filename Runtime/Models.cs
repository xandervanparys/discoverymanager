using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecognX
{
    
    [Serializable]
    public struct LocalizedObject
    {
        public string label;
        public int yoloId;
        public Vector3 position;
        public LocalizedObject(string name, int yoloId, Vector3 pos)
        {
            label = name;
            this.yoloId = yoloId;
            position = pos;
        }
    }
    
    [System.Serializable]
    public class SetupResponse
    {
        public TaskResponse task;
        public string acknowledgment;
        public TokenUsage token_usage;
        public float open_ai_time;
    }

    [System.Serializable]
    public class TokenUsage
    {
        public int prompt;
        public int completion;
        public int total;
    } 
    
    [Serializable]
    public class TaskResponse
    {
        public string id;
        public string title;
        public Step[] steps;
        public ObjectData[] objects;
    }

    [Serializable]
    public class Step
    {
        public int id;
        public string description;
        public ObjectData[] relevant_objects;
    }

    [Serializable]
    public class ObjectData
    {
        public int id;
        public string step_name;
        public string yolo_name;
        public int yolo_class_id;
    }

    [Serializable]
    public class TaskListWrapper
    {
        public TaskResponse[] tasks;
    }
    
    [System.Serializable]
    public class YoloDetection
    {
        public string class_name;
        public int yoloId;
        public float confidence;    
        public float[] bounding_box;
    }
    
    [System.Serializable]
    public class DetectionResponse
    {
        public List<YoloDetection> objects;
    }
}