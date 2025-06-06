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
            var step = GetCurrentStep();
            if (step == null) return new List<LocalizedObject>();

            var relevantIds = step.relevant_objects.Select(o => o.yolo_class_id).ToHashSet();
            return locatedObjects
                .Where(kvp => relevantIds.Contains(kvp.Key))
                .SelectMany(kvp => kvp.Value)
                .ToList();
        }

        public int GetRequiredCount(int yoloId)
        {
            return requiredYoloCounts.TryGetValue(yoloId, out var count) ? count : 0;
        }

        public int GetFoundCount(int yoloId)
        {
            return locatedObjects.TryGetValue(yoloId, out var list) ? list.Count : 0;
        }

        public List<(int stepId, string description, bool completed)> GetStepProgress()
        {
            return CurrentTask.steps
                .Select(step => (
                    step.id,
                    step.description,
                    completed: completedSteps.Contains(step.id)
                ))
                .ToList();
        }
        
        public Dictionary<int, (string label, int required, int found)> GetCurrentStepObjects()
        {
            var summary = new Dictionary<int, (string label, int required, int found)>();
            var step = GetCurrentStep();
            if (step == null || step.relevant_objects == null)
                return summary;
            
            foreach (var obj in step.relevant_objects)
            {
                int yoloId = obj.yolo_class_id;
                string label = obj.step_name;
                int required = GetRequiredCount(yoloId);
                int found = GetFoundCount(yoloId);
                summary[yoloId] = (label, required, found);
            }

            return summary;
        }
        
        public Dictionary<int, (string label, int required, int found)> GetAllObjectsForCurrentTask()
        {
            var summary = new Dictionary<int, (string label, int required, int found)>();
            foreach (var obj in this.CurrentTask.objects)
            {
                int yoloId = obj.yolo_class_id;
                if (!summary.ContainsKey(yoloId))
                {
                    string label = obj.step_name;
                    int required = this.GetRequiredCount(yoloId);
                    int found = this.GetFoundCount(yoloId);
                    summary[yoloId] = (label, required, found);
                }
            }
            return summary;
        }
        
        
        public List<int> GetAllRelevantYoloIdsForCurrentStep()
        {
            var result = new List<int>();
            var step = GetCurrentStep();
            if (step == null) return result;

            foreach (var obj in step.relevant_objects)
            {
                result.Add(obj.yolo_class_id);
            }

            return result;
        }
    }
}
