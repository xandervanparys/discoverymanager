using System.Threading.Tasks;
using UnityEngine;

namespace RecognX
{
    public class TaskController
    {
        private readonly BackendService backend;
        private readonly SessionManager session;

        public TaskController(BackendService backendService, SessionManager sessionManager)
        {
            this.backend = backendService;
            this.session = sessionManager;
        }

        public async Task<SetupResponse> SelectTaskAsync(string taskId)
        {
            var setup = await backend.SubmitTaskAsync(taskId);
            session.StartSession(setup.task);
            return setup;
        }
    }
}
