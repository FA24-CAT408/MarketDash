using System;

[Serializable]
public class LevelSaveData
{
        public float CompletionTime;
        public bool IsCompleted;

        public LevelSaveData(float completionTime = 0f)
        {
                CompletionTime = completionTime;
                IsCompleted = false;
        }
}
