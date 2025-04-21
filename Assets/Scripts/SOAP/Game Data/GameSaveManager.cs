using System.Collections;
using System.Collections.Generic;
using System.Text;
using Obvious.Soap;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSaveData.asset", menuName = "CrazyMarket/GameSaveData")]
public class GameSaveManager : ScriptableSave<GameSaveData>
{
    // Getters
    public float TotalTime => _saveData.TotalTime;
    public IReadOnlyList<LevelTimeEntry> LevelTimeEntries => _saveData.LevelEntries.AsReadOnly();
    
    // Methods
    public void SetLevelTime(int levelId, float time)
    {
        LevelTimeEntry entry = _saveData.GetLevelEntry(levelId);
        
        if (entry == null)
        {
            entry = new LevelTimeEntry(levelId);
            entry.CompletionTime = time;
            entry.BestCompletionTime = time; // First completion is also best time
            entry.IsCompleted = true;
            _saveData.LevelEntries.Add(entry);
            
            // First completion, add to total time
            _saveData.TotalTime += time;
        }
        else
        {
            // Level was already completed before
            if (entry.IsCompleted)
            {
                // Update total time by removing old time and adding new time
                _saveData.TotalTime -= entry.CompletionTime;
                _saveData.TotalTime += time;

                // Update completion time
                entry.CompletionTime = time;

                // Update best time if this run was better
                if (time < entry.BestCompletionTime)
                {
                    entry.BestCompletionTime = time;
                }
            }
            else
            {
                // First completion of this level
                entry.CompletionTime = time;
                entry.BestCompletionTime = time; // First completion is also best time
                entry.IsCompleted = true;
                _saveData.TotalTime += time;
            }
        }
        
        Save();
    }

    public float GetLevelTime(int levelId)
    {
        LevelTimeEntry entry = _saveData.GetLevelEntry(levelId);
        
        if (entry != null && entry.IsCompleted)
        {
            return entry.CompletionTime;
        }

        return 0f;
    }

    public bool IsLevelCompleted(int levelId)
    {
        LevelTimeEntry entry = _saveData.GetLevelEntry(levelId);
        return entry != null && entry.IsCompleted;
    }

    public void ResetAllTimes()
    {
        _saveData.LevelEntries.Clear();
        _saveData.TotalTime = 0f;
        Save();
    }
    
    // Print all level times in a neat format
    public void PrintAllLevelTimes()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===== LEVEL COMPLETION TIMES =====");
        
        // Get sorted list of level IDs
        List<int> levelIds = new List<int>();
        foreach (var entry in _saveData.LevelEntries)
        {
            levelIds.Add(entry.LevelId);
        }
        levelIds.Sort();
        
        int completedLevels = 0;
        
        foreach (int levelId in levelIds)
        {
            LevelTimeEntry entry = _saveData.GetLevelEntry(levelId);
            if (entry.IsCompleted)
            {
                completedLevels++;
                sb.AppendLine($"Level {levelId}: Current: {FormatTime(entry.CompletionTime)} | Best: {FormatTime(entry.BestCompletionTime)}");
            }
            else
            {
                sb.AppendLine($"Level {levelId}: Not completed");
            }
        }
        
        sb.AppendLine("--------------------------------");
        sb.AppendLine($"Completed Levels: {completedLevels}/{levelIds.Count}");
        sb.AppendLine($"Total Time: {FormatTime(_saveData.TotalTime)}");
        sb.AppendLine("=================================");
        
        Debug.Log(sb.ToString());
    }
    
    // Helper method to format time consistently
    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        int centiseconds = (int)((time * 100) % 100);
        
        if (minutes > 0)
        {
            return string.Format("{0}:{1:00}.{2:00}", minutes, seconds, centiseconds);
        }
        else
        {
            return string.Format("{0}.{1:00}", seconds, centiseconds);
        }
    }

    protected override void UpgradeData(GameSaveData oldData)
    {
        if (_debugLogEnabled)
            Debug.Log("Upgrading data from version " + oldData.SaveDataVersion + " to " + _saveData.SaveDataVersion);

        oldData.SaveDataVersion = _saveData.SaveDataVersion;
    }

    protected override bool NeedsUpgrade(GameSaveData saveData)
    {
        return saveData.SaveDataVersion < _saveData.SaveDataVersion;
    }
}