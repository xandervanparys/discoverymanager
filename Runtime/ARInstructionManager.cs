using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace RecognX
{
    public enum TaskState
    {
        Idle,
        TaskSummary,
        Tracking,
        Completed
    }

    public class ARInstructionManager : MonoBehaviour
    {
        public static ARInstructionManager Instance { get; private set; }

        public event Action<List<TaskResponse>> OnTasksLoaded;
        public event Action<InstructionTrackingResponse> OnInstructionFeedback;
        public event Action<Dictionary<int, (string label, int requiredCount, int foundCount)>> 
            OnRelevantObjectsUpdated;

        public event Action<Dictionary<int, (string label, int requiredCount, int foundCount)>>
            OnLocalizationProgressUpdated;

        public event Action<List<(int stepId, string description, bool completed)>> OnStepProgressUpdated;

        public TaskState CurrentState => currentState;
        public event Action<TaskState> OnTaskStateChanged;

        [Header("AR Dependencies")] [SerializeField]
        private ARCameraManager arCameraManager;

        [SerializeField] private ARMeshManager arMeshManager;
        [SerializeField] private Camera arCamera;

        [Header("Visualizations")] [SerializeField]
        private bool useBuiltInLabelRenderer = false;

        [Header("Object Labeling")] [SerializeField]
        private GameObject labelPrefab;

        private GameObject labelContainer;

        private BackendService backendService;
        private SessionManager sessionManager;
        private DiscoveryManager discoveryManager;

        private TaskState currentState = TaskState.Idle;

        private void SetState(TaskState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            Debug.Log($"[RecognX] State changed to: {newState}");
            OnTaskStateChanged?.Invoke(currentState);
        }

        public void StartTracking()
        {
            SetState(TaskState.Tracking);
            Debug.Log($"[RecognX] State changed to: {CurrentState}");

            UpdateLabelsForCurrentStep();
            OnStepProgressUpdated?.Invoke(sessionManager.GetStepProgress());
            OnLocalizationProgressUpdated?.Invoke(sessionManager.GetCurrentStepObjects());
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            Debug.Log("ARInstructionManager running.");
            SetState(TaskState.Idle);
            backendService = new BackendService();
            LoadTasks();

            sessionManager = new SessionManager();
            discoveryManager = new DiscoveryManager(arCameraManager, arMeshManager, arCamera, backendService);

            if (useBuiltInLabelRenderer)
            {
                labelContainer = new GameObject("RecognX_Labels");
                labelPrefab = Resources.Load<GameObject>("Label3D");
                if (labelPrefab == null)
                {
                    Debug.LogWarning("Could not load LabelPrefab from Resources");
                }
            }

            // await SelectTaskAsync("da676f65-307e-4137-9bf8-7f3179ad3743");
        }

        private async void LoadTasks()
        {
            var tasks = await backendService.FetchAllTasksAsync();
            foreach (var task in tasks)
                Debug.Log($"✅ Task: {task.title} ({task.id})");
            OnTasksLoaded?.Invoke(tasks);
        }

        public async Task SelectTaskAsync(string taskId)
        {
            var setup = await backendService.SubmitTaskAsync(taskId);
            sessionManager.StartSession(setup.task);
            Debug.Log(
                $"🎯 Task selected: {setup.task.title}, GPT ack: {setup.acknowledgment}, SessionManagerCheck: {sessionManager.CurrentTask.title}");

            SetState(TaskState.TaskSummary);
            EmitLocalizationProgress();
        }

        public Dictionary<int, (string label, int required, int found)> GetAllObjectsForCurrentTask()
        {
            return sessionManager.GetAllObjectsForCurrentTask();
        }

        private void EmitLocalizationProgress()
        {
            var summary = sessionManager.GetCurrentStepObjects();
            OnLocalizationProgressUpdated?.Invoke(summary);
        }

        public async Task CaptureAndLocalize()
        {
            var activeIds = sessionManager.GetYoloIdsToFind();
            var objects = await discoveryManager.LocateObjectsAsync(activeIds);
            HandleObjectsLocalized(objects);
        }

        private void HandleObjectsLocalized(List<LocalizedObject> objects)
        {
            Debug.Log($"I got here 0");
            foreach (var obj in objects)
            {
                Debug.Log($"🔍 Evaluating object: {obj.label} (YOLO ID: {obj.yoloId})");

                bool isRelevantForStep = sessionManager.GetAllRelevantYoloIdsForCurrentStep().Contains(obj.yoloId);
                bool shouldShow = sessionManager.MarkYoloObjectFound(obj) && isRelevantForStep;                Debug.Log($"✅ Marked object found: {shouldShow}");

                if (!useBuiltInLabelRenderer)
                {
                    Debug.Log("⚠️ Label rendering is disabled (useBuiltInLabelRenderer == false)");
                }

                if (!shouldShow)
                {
                    Debug.Log("⚠️ Object already fully found, skipping label placement");
                }

                if (!useBuiltInLabelRenderer || !shouldShow) continue;

                Debug.Log("🏷 Placing label...");
                placeLabel(obj);
                EmitLocalizationProgress();
            }
        }
        
        public List<(int stepId, string description, bool completed)> GetAllStepsForCurrentTask()
        {
            return sessionManager.GetStepProgress();
        }

        public void ClearLabels()
        {
            if (labelContainer != null)
                Destroy(labelContainer);

            labelContainer = new GameObject("RecognX_Labels");
        }

        public async Task TrackStepAsync()
        {
            var cameraTexture = DiscoveryManager.CaptureCameraTextureAsync();
            var response = await backendService.SubmitLiveFrameAsync(cameraTexture);

            Debug.Log($"📩 GPT Feedback: {response.response}");
            Debug.Log(
                $"🧭 Step: {response.step_number} | ✅ Completed: {response.step_completed} | 🎯 Task Done: {response.task_completed}");

            if (response.step_completed)
            {
                sessionManager.AdvanceStep();
                UpdateLabelsForCurrentStep();
                EmitLocalizationProgress();
            }

            if (response.task_completed)
            {
                SetState(TaskState.Completed);
            }
            else if (response.step_completed)
            {
                SetState(TaskState.Tracking);
            }

            OnInstructionFeedback?.Invoke(response);
            OnRelevantObjectsUpdated?.Invoke(sessionManager.GetCurrentStepObjects());
            OnStepProgressUpdated?.Invoke(sessionManager.GetStepProgress());
        }

        public List<LocalizedObject> GetCurrentRelevantLocalizedObjects()
        {
            return sessionManager.GetLocalizedObjectsForCurrentStep();
        }

        private void placeLabel(LocalizedObject obj)
        {
            GameObject label = Instantiate(labelPrefab, obj.position, Quaternion.identity);
            var text = label.GetComponentInChildren<TMPro.TextMeshPro>();
            if (text != null) text.text = obj.label;
            label.transform.SetParent(labelContainer.transform, true);
        }

        private void UpdateLabelsForCurrentStep()
        {
            if (!useBuiltInLabelRenderer) return;

            ClearLabels();
            var relevantObjects = sessionManager.GetLocalizedObjectsForCurrentStep();
            foreach (var obj in relevantObjects)
            {
                placeLabel(obj);
            }
        }

        public void ResetToTaskSelection()
        {
            sessionManager.Reset();
            ClearLabels();
            SetState(TaskState.Idle);
        }
    }
}