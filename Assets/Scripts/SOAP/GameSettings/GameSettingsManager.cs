using System.Collections;
using System.Collections.Generic;
using Obvious.Soap;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettingsData.asset", menuName = "CrazyMarket/GameSettingsData")]
public class GameSettingsManager : ScriptableSave<GameSettingsData>
{
    public float Sensitivity
    {
        get => _saveData.Sensitivity;
        set { _saveData.Sensitivity = value; Save(); }
    }

    public float Volume
    {
        get => _saveData.Volume;
        set { _saveData.Volume = value; Save(); }
    }

    public bool InvertCamera
    {
        get => _saveData.InvertCamera;
        set { _saveData.InvertCamera = value; Save(); }
    }
    
    public void SetVolume(float volume)
    {
        Volume = volume;
    
        // Update AudioManager if it exists
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(volume);
        }
    }

    protected override void UpgradeData(GameSettingsData oldData)
    {
        oldData.SaveDataVersion = _saveData.SaveDataVersion;
    }

    protected override bool NeedsUpgrade(GameSettingsData saveData)
    {
        return saveData.SaveDataVersion < _saveData.SaveDataVersion;
    }
}
