using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem
{
    GameState data;
    string saveFileName = "save";

    GameManager gameManager;

    List<MechanismSaveData> mechanismSaves;
    List<CollectableSaveData> collectableSaves;
    List<ResourceSaveData> resourceSaves;
    List<SaveFlagSaveData> saveFlagSaves;
    List<LogSaveData> logSaves;

    List<string> completedAchievements;
    List<string> achievementIDs;

    Dictionary<string, SaveFlag> saveFlags;
    LogComputerMechanism logComputer;

    public System.Action OnLoad;
    public System.Action OnSave;

    List<Blueprint> mechanismBlueprints;

    public static SaveSystem Instance;


    public SaveSystem(GameManager _gameManager)
    {
        gameManager = _gameManager;

        mechanismSaves = new();
        collectableSaves = new();
        resourceSaves = new();
        saveFlags = new();
        saveFlagSaves = new();
        logSaves = new();

        Instance = this;
    }

    public void Save()
    {
        mechanismSaves.Clear();
        collectableSaves.Clear();
        resourceSaves.Clear();
        saveFlagSaves.Clear();
        logSaves.Clear();

        completedAchievements = gameManager.AchievementManager.GetCompletedAchievements();

        List<Mechanism> mechanisms = gameManager.Mechanisms;
        List<Collectable> collectables = gameManager.Collectables;

        foreach (KeyValuePair<ResourceType, MachineResource> resourceByType in gameManager.MachineReference.Resources)
        {
            resourceSaves.Add(new ResourceSaveData((int)resourceByType.Key, resourceByType.Value.CurrentAmount));
        }

        for (int i = 0; i < collectables.Count; i++)
        {
            collectableSaves.Add(collectables[i].Save());
        }

        for (int i = 0; i < mechanisms.Count; i++)
        {
            mechanismSaves.Add(mechanisms[i].Save());
        }

        foreach (SaveFlag flag in saveFlags.Values)
        {
            saveFlagSaves.Add(new SaveFlagSaveData(flag.ID, flag.IsSet));
        }

        logSaves = logComputer.GetLogSaves();

        data = new GameState(mechanismSaves,
            collectableSaves,
            gameManager.StatSaves,
            completedAchievements,
            gameManager.BlueprintManager.GetReplacedNodeSaves(),
            resourceSaves,
            saveFlagSaves,
            logSaves
            );

        string json = JsonUtility.ToJson(data, true);
        //string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        string fileName = $"{Application.persistentDataPath}/{saveFileName}.JSON";
        SaveToFile(fileName, json);

        PlayerPrefs.SetString("lastSave", System.DateTime.Now.Ticks.ToString());

        OnSave?.Invoke();
    }

    public void SaveToFile(string fileName, string data)
    {
        FileStream file = File.Create(fileName);
        file.Write(System.Text.Encoding.UTF8.GetBytes(data));
        file.Close();
    }

    public void Load(string _fileName, int stagger = 0)
    {
        gameManager.StartCoroutine(LoadCoroutine( _fileName, stagger));
    }

    public IEnumerator LoadCoroutine(string _fileName, int stagger)
    {
        if(stagger > 0)
        {
            for (int i = 0; i< stagger; i++)
            {
                yield return 0;
            }
        }

        saveFileName = _fileName;
        //check if file exists -> create
        string path = $"{Application.persistentDataPath}/{saveFileName}.JSON";
        if (!File.Exists(path)) NewGame(saveFileName);

        //FileStream stream = File.Open(saveFileName);

        StreamReader reader = new StreamReader(path);
        data = JsonUtility.FromJson<GameState>(reader.ReadToEnd());
        //data = JsonConvert.DeserializeObject<GameData> (reader.ReadToEnd());

        reader.Close();

        //put up loading screen
        //simulate ticks?

        //TODO compare names/ids here instead of iterating
        Dictionary<string, MechanismSaveData> mechanismSaves = new Dictionary<string, MechanismSaveData>();
        Dictionary<string, CollectableSaveData> collectableSaves = new Dictionary<string, CollectableSaveData>();

        foreach (MechanismSaveData save in data.mechanismSaves)
        {
            mechanismSaves.TryAdd(save.id, save);
        }

        foreach (CollectableSaveData save in data.collectableSaves)
        {
            collectableSaves.TryAdd(save.id, save);
        }

        foreach (Mechanism mechanism in gameManager.Mechanisms)
        {
            if (mechanismSaves.TryGetValue(mechanism.Data.id, out MechanismSaveData data))
            {
                mechanism.Load(data);
            }
            else
            {
                mechanismSaves.Add(mechanism.Data.id, mechanism.Save());
            }
        }

        foreach (Collectable collectable in gameManager.Collectables)
        {
            if (collectableSaves.TryGetValue(collectable.ID, out CollectableSaveData data))
            {
                collectable.Load(collectableSaves[collectable.ID]);
            }
            else
            {
                collectableSaves.Add(collectable.ID, collectable.Save());
            }

        }

        foreach (ResourceSaveData resourceSave in data.resourceSaveData)
        {
            gameManager.MachineReference.SetResource((ResourceType)resourceSave.resource, resourceSave.currentAmount, false);
        }

        foreach (SaveFlagSaveData flagSave in data.saveFlagSaves)
        {
            if(saveFlags.TryGetValue(flagSave.FlagID, out SaveFlag flag)) flag.SetFlag(flagSave.IsSet, false);
        }

        logComputer.Load(data.logSaves);

        gameManager.BlueprintManager.RestoreReplacedNodes(data.replacedNodes);

        gameManager.AchievementManager.SetCompletedAchievements(data.achievements);
        gameManager.Stats.LoadSaveData(data.stats);

        OnLoad?.Invoke();

        yield return null;
    }

    //returns save
    public void NewGame(string _fileName)
    {
        saveFileName = _fileName;
        Save();
    }

    public void RegisterSaveFlag(SaveFlag flag)
    {
        if(saveFlags.ContainsKey(flag.ID))
        {
            Debug.LogError($"duplicate saveflag id between [{saveFlags[flag.ID].gameObject.name}] and [{flag.gameObject.name}]");
        }
        else
        {
            saveFlags.Add(flag.ID, flag);
            flag.OnFlagChanged += FlagChanged;
            //on flag set unset - remember the flags, force save
            //tie events here?
        }
    }

    public void RegisterLogComputer(LogComputerMechanism _logComputer)
    {
        logComputer = _logComputer;
    }


    public void FlagChanged(SaveFlag flag)
    {
        Save();
    }

    public void SetFlag(string id, bool state)
    {
        if (saveFlags.TryGetValue(id, out SaveFlag flag)) flag.SetFlag(state, true);
    }
}

[System.Serializable]
public class ReplacedNodeSaveData
{
    public string mechanismID;
    public string achievementID;
    public int x;
    public int y;

    public ReplacedNodeSaveData(string _mechanismID, string _achievementID, int _x, int _y)
    {
        mechanismID = _mechanismID;
        achievementID = _achievementID;
        x = _x;
        y = _y;
    }
}
[System.Serializable]
public class ResourceSaveData
{
    public int resource;
    public float currentAmount;

    public ResourceSaveData(int _resource, float _currentAmount)
    {
        resource = _resource;
        currentAmount = _currentAmount;
    }
}

[System.Serializable]
public class SaveFlagSaveData
{
    public string FlagID;
    public bool IsSet;

    public SaveFlagSaveData(string _FlagID, bool _IsSet)
    {
        FlagID = _FlagID;
        IsSet = _IsSet;
    }
}

[System.Serializable]
public class LogSaveData
{
    public string LogID;
    public bool IsComplete;
    public bool IsRead;

    public long TimeCompletedAt;

    public LogSaveData(string _LogID, bool _IsComplete, bool _IsRead, long _TimeCompleteAt)
    {
        LogID = _LogID;
        IsComplete = _IsComplete;
        IsRead = _IsRead;
        TimeCompletedAt = _TimeCompleteAt;

    }
}
