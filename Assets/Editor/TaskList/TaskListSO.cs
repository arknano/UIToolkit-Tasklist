using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TaskListSO : ScriptableObject
{
    public List<Task> tasks = new List<Task>();

    [System.Serializable]
    public class Task
    {
        public string text;
        public bool complete;

        public Task(string text, bool complete)
        {
            this.text = text;
            this.complete = complete;
        }
    }
}
