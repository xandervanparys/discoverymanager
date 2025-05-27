using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RecognX
{
    public class BackendService
    {
        private const string BaseUrl = "https://api.web-present.be";

        public async Task<List<TaskResponse>> FetchAllTasksAsync()
        {
            using UnityWebRequest www = UnityWebRequest.Get($"{BaseUrl}/instruction/tasks/");
            www.downloadHandler = new DownloadHandlerBuffer();

            var operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ FetchAllTasks failed: {www.error}");
                return new List<TaskResponse>();
            }

            string json = "{\"tasks\":" + www.downloadHandler.text + "}";
            var wrapped = JsonUtility.FromJson<TaskListWrapper>(json);
            return new List<TaskResponse>(wrapped.tasks);
        }

        public async Task<SetupResponse> SubmitTaskAsync(string taskId)
        {
            WWWForm form = new WWWForm();
            form.AddField("user_id", GetOrCreateUserId());

            if (!string.IsNullOrEmpty(taskId))
            {
                form.AddField("task_id", taskId);
            }

            using UnityWebRequest www = UnityWebRequest.Post($"{BaseUrl}/instruction/setup/", form);
            www.downloadHandler = new DownloadHandlerBuffer();

            var operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ SubmitTask failed: {www.error}");
                return null;
            }

            return JsonUtility.FromJson<SetupResponse>(www.downloadHandler.text);
        }

        public async Task<List<YoloDetection>> DetectObjectsAsync(Texture2D image, List<int> yoloClassIds)
        {
            byte[] imageBytes = image.EncodeToJPG();

            WWWForm form = new WWWForm();
            form.AddBinaryData("file", imageBytes, "capture.jpg", "image/jpeg");

            foreach (int id in yoloClassIds)
            {
                form.AddField("yolo_ids", id);
            }

            using UnityWebRequest www = UnityWebRequest.Post($"{BaseUrl}/yolo/detect_filtered/", form);
            www.downloadHandler = new DownloadHandlerBuffer();

            var operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ DetectObjects failed: {www.error}");
                return new List<YoloDetection>();
            }

            string json = "{\"objects\":" + www.downloadHandler.text + "}";
            var wrapped = JsonUtility.FromJson<DetectionResponse>(json);
            Debug.Log($"raw response: {json}");
            return wrapped.objects;
        }

        public async Task<InstructionTrackingResponse> SubmitLiveFrameAsync(Texture2D image)
        {
            byte[] imageBytes = image.EncodeToJPG();
            WWWForm form = new();
            form.AddBinaryData("frame", imageBytes, "track.jpg", "image/jpeg");
            form.AddField("user_id", GetOrCreateUserId());
            using UnityWebRequest www = UnityWebRequest.Post($"{BaseUrl}/instruction/track/", form);
            www.downloadHandler = new DownloadHandlerBuffer();

            var operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Live tracking + feedback failed: {www.error}");
                return new InstructionTrackingResponse();
            }

            return JsonUtility.FromJson<InstructionTrackingResponse>(www.downloadHandler.text);
        }

        private static string GetOrCreateUserId()
        {
            const string key = "user_id";
            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetString(key, System.Guid.NewGuid().ToString());
            }

            return PlayerPrefs.GetString(key);
        }
    }
}