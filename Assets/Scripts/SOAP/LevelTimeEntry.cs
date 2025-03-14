using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelTimeEntry
{
    public int LevelId;
    public float CompletionTime;
    public bool IsCompleted;

    public LevelTimeEntry(int levelId)
    {
        LevelId = levelId;
        CompletionTime = 0f;
        IsCompleted = false;
    }

    public LevelTimeEntry(int levelId, float completionTime, bool isCompleted)
    {
        LevelId = levelId;
        CompletionTime = completionTime;
        IsCompleted = isCompleted;
    }
}
