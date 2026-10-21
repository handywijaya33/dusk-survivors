using System;
using UnityEngine;

/// <summary>A permanent upgrade bought with gold between runs (the "roguelite" part).</summary>
public class MetaUpgrade
{
    public string Key, Name, Desc;
    public int MaxLevel, BaseCost;
    public Action<Player, int> ApplyTo;

    public int Cost(int currentLevel) => BaseCost * (currentLevel + 1);
}

public static class MetaUpgrades
{
    public static readonly MetaUpgrade[] All =
    {
        new MetaUpgrade { Key = "vitality", Name = "Vitality", Desc = "+10 max HP", MaxLevel = 5, BaseCost = 40, ApplyTo = (p, l) => p.MaxHp += 10f * l },
        new MetaUpgrade { Key = "might", Name = "Might", Desc = "+5% damage", MaxLevel = 5, BaseCost = 60, ApplyTo = (p, l) => p.Might += 0.05f * l },
        new MetaUpgrade { Key = "swift", Name = "Swiftness", Desc = "+5% move speed", MaxLevel = 5, BaseCost = 40, ApplyTo = (p, l) => p.Speed *= 1f + 0.05f * l },
        new MetaUpgrade { Key = "haste", Name = "Haste", Desc = "-3% weapon cooldown", MaxLevel = 5, BaseCost = 60, ApplyTo = (p, l) => p.CooldownMul *= 1f - 0.03f * l },
        new MetaUpgrade { Key = "magnet", Name = "Magnetism", Desc = "+15% pickup range", MaxLevel = 5, BaseCost = 30, ApplyTo = (p, l) => p.Magnet *= 1f + 0.15f * l },
        new MetaUpgrade { Key = "growth", Name = "Growth", Desc = "+8% experience", MaxLevel = 5, BaseCost = 50, ApplyTo = (p, l) => p.Growth += 0.08f * l },
        new MetaUpgrade { Key = "greed", Name = "Greed", Desc = "+10% gold found", MaxLevel = 5, BaseCost = 40, ApplyTo = (p, l) => p.Greed += 0.1f * l },
        new MetaUpgrade { Key = "revival", Name = "Revival", Desc = "Revive once per run", MaxLevel = 1, BaseCost = 300, ApplyTo = (p, l) => p.Revives += l },
    };
}

public static class SaveData
{
    public static int Gold
    {
        get => PlayerPrefs.GetInt("meta_gold", 0);
        set => PlayerPrefs.SetInt("meta_gold", value);
    }

    public static float BestTime
    {
        get => PlayerPrefs.GetFloat("best_time", 0f);
        set => PlayerPrefs.SetFloat("best_time", value);
    }

    public static int StartWeapon
    {
        get => PlayerPrefs.GetInt("start_weapon", 0);
        set => PlayerPrefs.SetInt("start_weapon", value);
    }

    public static int GetLevel(string key) => PlayerPrefs.GetInt("meta_" + key, 0);

    public static bool TryBuy(MetaUpgrade m)
    {
        int level = GetLevel(m.Key);
        int cost = m.Cost(level);
        if (level >= m.MaxLevel || Gold < cost) return false;
        Gold -= cost;
        PlayerPrefs.SetInt("meta_" + m.Key, level + 1);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Refunds all purchased upgrades back into gold.</summary>
    public static void Refund()
    {
        int refund = 0;
        foreach (var m in MetaUpgrades.All)
        {
            int level = GetLevel(m.Key);
            for (int l = 0; l < level; l++) refund += m.Cost(l);
            PlayerPrefs.SetInt("meta_" + m.Key, 0);
        }
        Gold += refund;
        PlayerPrefs.Save();
    }
}
