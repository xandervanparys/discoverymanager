using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RecognX
{
    public class SessionManager
    {
        public TaskResponse CurrentTask { get; private set; }

        private readonly Dictionary<int, int> requiredYoloCounts = new();
        private readonly Dictionary<int, List<LocalizedObject>> locatedObjects = new();

        private int currentStepIndex = 0;
        private readonly HashSet<int> completedSteps = new();

        public int CurrentStepIndex => currentStepIndex;

        public IReadOnlyCollection<int> CompletedSteps => completedSteps;

        public void StartSession(TaskResponse task)
        {
            CurrentTask = task;

            requiredYoloCounts.Clear();
            locatedObjects.Clear();
            currentStepIndex = 0;
            completedSteps.Clear();

            foreach (var obj in task.objects)
            {
                if (!requiredYoloCounts.ContainsKey(obj.yolo_class_id))
                    requiredYoloCounts[obj.yolo_class_id] = 0;

                requiredYoloCounts[obj.yolo_class_id]++;
            }
        }

        public void Reset()
        {
            CurrentTask = null;
            requiredYoloCounts.Clear();
            locatedObjects.Clear();
        }

        public bool MarkYoloObjectFound(LocalizedObject obj)
        {
            int yoloId = obj.yoloId;

            if (!requiredYoloCounts.ContainsKey(yoloId))
                return false;

            if (!locatedObjects.ContainsKey(yoloId))
                locatedObjects[yoloId] = new List<LocalizedObject>();

            if (locatedObjects[yoloId].Count >= requiredYoloCounts[yoloId])
                return false;

            locatedObjects[yoloId].Add(obj);

            return true;
        }

        public List<int> GetYoloIdsToFind()
        {
            HashSet<int> toFind = new();

            foreach (KeyValuePair<int, int> kv in requiredYoloCounts)
            {
                int yoloId = kv.Key;
                int requiredCount = kv.Value;

                int foundCount = locatedObjects.ContainsKey(yoloId) ? locatedObjects[yoloId].Count : 0;
                if (requiredCount > foundCount)
                {
                    toFind.Add(yoloId);
                }
            }

            return toFind.ToList();
        }

        public IEnumerable<LocalizedObject> GetAllActiveLocalizedObjects()
        {
            foreach (var kvp in locatedObjects)
            {
                foreach (var obj in kvp.Value)
                    yield return obj;
            }
        }

        public Dictionary<int, List<LocalizedObject>> getFoundObjects()
        {
            return locatedObjects;
        }

        public List<LocalizedObject> getFoundObjectsAsList()
        {
            return locatedObjects.Values.SelectMany(list => list).ToList();
        }

        public void AdvanceStep()
        {
            completedSteps.Add(currentStepIndex);
            currentStepIndex++;
        }

        public Step GetCurrentStep()
        {
            if (CurrentTask != null && currentStepIndex < CurrentTask.steps.Length)
                return CurrentTask.steps[currentStepIndex];

            return null;
        }
        
        public List<LocalizedObject> GetLocalizedObjectsForCurrentStep()
        {
            var relevantIds = GetCurrentStep().relevant_objects.Select(o => o.yolo_class_id).ToHashSet();
            return locatedObjects
                .Where(kvp => relevantIds.Contains(kvp.Key))
                .SelectMany(kvp => kvp.Value)
                .ToList();
        }
    }
}