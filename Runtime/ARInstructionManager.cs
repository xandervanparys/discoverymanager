using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace RecognX
{
    public class ARInstructionManager : MonoBehaviour
    {
        public static ARInstructionManager Instance { get; private set; }

        [Header("AR Dependencies")] [SerializeField]
        private ARCameraManager arCameraManager;

        [SerializeField] private ARMeshManager arMeshManager;
        [SerializeField] private Camera arCamera;

        [Header("Visualizations")] [SerializeField]
        private bool showRays = false;

        [SerializeField] private bool showLabels = false;
        [SerializeField] private float rayLength = 8f;

        [Header("Object Labeling")] [SerializeField]
        private GameObject labelPrefab;

        [SerializeField] private GameObject labelContainer;

        private BackendService backendService;
        private SessionManager sessionManager;
        private TaskController taskController;
        private DiscoveryManager discoveryManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // You’ll wire up the rest of the logic here later
        }

        private async void Start()
        {
            Debug.Log("ARInstructionManager running.");
            backendService = new BackendService();
            var tasks = await backendService.FetchAllTasksAsync();
            foreach (var task in tasks)
                Debug.Log($"✅ Task: {task.title} ({task.id})");

            sessionManager = new SessionManager();
            taskController = new TaskController(backendService, sessionManager);

            discoveryManager = new DiscoveryManager();
            discoveryManager.Initialize(arCameraManager, arMeshManager, arCamera, backendService);
            discoveryManager.OnObjectsLocalized += HandleObjectsLocalized;

            if (showLabels)
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

        public async Task SelectTaskAsync(string taskId)
        {
            var setup = await taskController.SelectTaskAsync(taskId);
            Debug.Log(
                $"🎯 Task selected: {setup.task.title}, GPT ack: {setup.acknowledgment}, SessionManagerCheck: {sessionManager.CurrentTask.title}");
        }

        public void CaptureAndLocalize()
        {
            var activeIds = sessionManager.GetYoloIdsToFind();
            discoveryManager.LocateObjects(activeIds);
        }

        private void HandleObjectsLocalized(List<LocalizedObject> objects)
        {
            foreach (var obj in objects)
            {
                bool shouldShow = sessionManager.MarkYoloObjectFound(obj);

                if (!showLabels || !shouldShow) continue;
                GameObject label = Instantiate(labelPrefab, obj.position, Quaternion.identity);
                var text = label.GetComponentInChildren<TMPro.TextMeshPro>();
                if (text != null) text.text = obj.label;
                label.transform.SetParent(labelContainer.transform, false);
            }
        }

        public void ClearLabels()
        {
            if (labelContainer != null)
                Destroy(labelContainer);

            labelContainer = new GameObject("RecognX_Labels");
        }
    }
}