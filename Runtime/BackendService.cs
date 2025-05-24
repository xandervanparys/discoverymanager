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