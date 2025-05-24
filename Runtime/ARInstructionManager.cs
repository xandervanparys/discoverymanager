using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace RecognX
{
    public class ARInstructionManager : MonoBehaviour
    {
        public static ARInstructionManager Instance { get; private set; }

        [Header("AR Dependencies")] 
        [SerializeField] private ARCameraManager arCameraManager;
        [SerializeField] private ARMeshManager arMeshManager;
        [SerializeField] private Camera arCamera;
        
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

            await SelectTaskAsync("da676f65-307e-4137-9bf8-7f3179ad3743");
        }
        
        public async Task SelectTaskAsync(string taskId)
        {
            var setup = await taskController.SelectTaskAsync(taskId);
            Debug.Log($"🎯 Task selected: {setup.task.title}, GPT ack: {setup.acknowledgment}, SessionManagerCheck: {sessionManager.CurrentTask.title}");
        }
    }
}