using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelTimeEntry
{
    public int LevelId;
    public float CompletionTime;
    public float BestCompletionTime;
    public bool IsCompleted;

    public LevelTimeEntry(int levelId)
    {
        LevelId = levelId;
        CompletionTime = 0f;
        BestCompletionTime = 0f;
        IsCompleted = false;
    }

    public LevelTimeEntry(int levelId, float completionTime, float bestCompletionTime, bool isCompleted)
    {
        LevelId = levelId;
        CompletionTime = completionTime;
        BestCompletionTime = bestCompletionTime;
        IsCompleted = isCompleted;
    }
}
