using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int SaveDataVersion = 1;
    public List<LevelTimeEntry> LevelEntries = new List<LevelTimeEntry>();
    public float TotalTime = 0f;
    
    // Helper methods to work with the list
    public LevelTimeEntry GetLevelEntry(int levelId)
    {
        foreach (var entry in LevelEntries)
        {
            if (entry.LevelId == levelId)
                return entry;
        }
        return null;
    }
    
    public void SetLevelEntry(int levelId, float completionTime, bool isCompleted)
    {
        for (int i = 0; i < LevelEntries.Count; i++)
        {
            if (LevelEntries[i].LevelId == levelId)
            {
                LevelEntries[i].CompletionTime = completionTime;
                LevelEntries[i].IsCompleted = isCompleted;
                return;
            }
        }
        
        // If not found, add new entry
        LevelEntries.Add(new LevelTimeEntry(levelId, completionTime, isCompleted));
    }
    
    public bool ContainsLevel(int levelId)
    {
        foreach (var entry in LevelEntries)
        {
            if (entry.LevelId == levelId)
                return true;
        }
        return false;
    }
}
