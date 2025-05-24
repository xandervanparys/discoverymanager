using UnityEngine;

namespace RecognX
{
    public class SessionManager
    {
        public TaskResponse CurrentTask { get; private set; }

        public void StartSession(TaskResponse task)
        {
            CurrentTask = task;
        }

        public void Reset()
        {
            CurrentTask = null;
        }
    }
}
