using ChebsNecromancy.Items.Armor.Player;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Skeletons;

namespace ChebsNecromancy.Options;

using Logger = Jotunn.Logger;

public class Options
{
    private const string BoneColorKey = "BoneColor";
    private const string EyeColorKey = "EyeColor";
    private const string EmblemKey = "Emblem";
    
    public static SkeletonMinion.BoneColor BoneColor
    {
        get => (SkeletonMinion.BoneColor)OptionsDict[BoneColorKey];
        set => OptionsDict[BoneColorKey] = (int)value;
    }
    
    public static UndeadMinion.EyeColor EyeColor
    {
        get => (UndeadMinion.EyeColor)OptionsDict[EyeColorKey];
        set => OptionsDict[EyeColorKey] = (int)value;
    }
    
    public static NecromancerCape.Emblem Emblem
    {
        get => (NecromancerCape.Emblem)OptionsDict[EmblemKey];
        set => OptionsDict[EmblemKey] = (int)value;
    }

    private static Dictionary<string, int> OptionsDict => _optionsDict ??= ReadOptionsFile();
    private static Dictionary<string, int> _optionsDict;
    
    private static string OptionsFileName => $"ChebsNecromancy.{Player.m_localPlayer?.GetPlayerName()}.Options.json";

    public static void SaveOptions()
    {
        var serializedDict = DictionaryToJson(OptionsDict); 
        Logger.LogInfo($"serializedDict={serializedDict}");
        UpdateOptionsFile(serializedDict);
    }
    
    private static void UpdateOptionsFile(string content)
    {
        var filePath = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData), OptionsFileName);

        if (!File.Exists(filePath))
        {
            try
            {
                using var fs = File.Create(filePath);
                fs.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating {filePath}: {ex.Message}");
            }
        }

        try
        {
            using var writer = new StreamWriter(filePath, false);
            writer.Write(content);
            writer.Close();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error writing to {filePath}: {ex.Message}");
        }
    }
    
    private static Dictionary<string, int> ReadOptionsFile()
    {
        var empty = new Dictionary<string, int>() { { BoneColorKey, 0 }, { EyeColorKey, 0 }, { EmblemKey, 0 } };
        
        var filePath = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData), OptionsFileName);

        if (!File.Exists(filePath))
        {
            try
            {
                using var fs = File.Create(filePath);
                fs.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating {filePath}: {ex.Message}");
            }
        }

        string content = null;
        try
        {
            using var reader = new StreamReader(filePath);
            content = reader.ReadToEnd();
            reader.Close();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error reading from {filePath}: {ex.Message}");
        }

        if (string.IsNullOrEmpty(content))
        {
            Logger.LogInfo($"Content is empty or null; create fresh options.");
            return empty;
        }

        // Logger.LogInfo($"Attempting to parse {content}");
        var parsed = SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, int>>(content);
        // Logger.LogInfo($"parsed={parsed}");
        if (parsed == null)
        {
            Logger.LogError("Failed to parse options.");
            return empty;
        }

        return parsed;
    }
    
    private static string DictionaryToJson(Dictionary<string, int> dictionary)
    {
        // because simplejson is being a PITA and saving a list of keyvalue pairs that it can't parse back in, let's
        // do it ourselves
        var entries = new List<string>();
        foreach (var kvp in dictionary)
        {
            var entry = $"\"{kvp.Key}\": {kvp.Value}";
            entries.Add(entry);
        }
        return "{" + string.Join(", ", entries) + "}";
    }
}