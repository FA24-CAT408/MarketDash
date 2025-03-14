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
            // Create a new entry with default values
            entry = new LevelTimeEntry(levelId);
            _saveData.LevelEntries.Add(entry);
        }

        // Only update if it's a better time or first completion
        if (!entry.IsCompleted || time < entry.CompletionTime)
        {
            // If already completed, subtract old time and add new time
            if (entry.IsCompleted)
            {
                _saveData.TotalTime -= entry.CompletionTime;
                _saveData.TotalTime += time;
            }
            else
            {
                // First completion, just add the time
                _saveData.TotalTime += time;
            }

            entry.CompletionTime = time;
            entry.IsCompleted = true;
            Save();
        }
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
                sb.AppendLine($"Level {levelId}: {FormatTime(entry.CompletionTime)}");
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
    
    public Dictionary<int, LevelTimeEntry> GetLevelTimesAsDictionary()
    {
        Dictionary<int, LevelTimeEntry> result = new Dictionary<int, LevelTimeEntry>();
        
        foreach (var entry in _saveData.LevelEntries)
        {
            result[entry.LevelId] = entry;
        }
        
        return result;
    }
}