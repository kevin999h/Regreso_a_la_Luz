using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int levelUnlocked = 1;
    public int totalScore = 0;
}

public static class SaveSystem
{
    private const string Key = "regreso_a_la_luz_save";

    public static void Save(SaveData data)
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static SaveData Load()
    {
        return PlayerPrefs.HasKey(Key)
            ? JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key))
            : new SaveData();
    }

    public static void UnlockLevel(int level)
    {
        SaveData data = Load();
        if (level > data.levelUnlocked)
        {
            data.levelUnlocked = level;
            Save(data);
        }
    }

    public static void AddScore(int points)
    {
        SaveData data = Load();
        data.totalScore += points;
        Save(data);
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(Key);
    }
}
