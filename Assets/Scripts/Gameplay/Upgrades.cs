using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Passive
{
    public readonly string Name, Desc;
    public readonly Color Color;
    public readonly int MaxLevel;
    public readonly Action<Player> Apply;

    public Passive(string name, string desc, Color color, int maxLevel, Action<Player> apply)
    {
        Name = name; Desc = desc; Color = color; MaxLevel = maxLevel; Apply = apply;
    }
}

public static class Passives
{
    public static readonly Passive[] All =
    {
        new Passive("Vital Heart", "+20 max HP and heal 20", new Color(1f, 0.35f, 0.4f), 5, p => { p.MaxHp += 20f; p.Heal(20f); }),
        new Passive("Iron Gauntlet", "+10% damage", new Color(0.8f, 0.8f, 0.85f), 5, p => p.Might += 0.1f),
        new Passive("Swift Boots", "+10% move speed", new Color(0.5f, 0.9f, 1f), 5, p => p.Speed *= 1.1f),
        new Passive("Hourglass", "-8% weapon cooldown", new Color(1f, 0.85f, 0.4f), 5, p => p.CooldownMul *= 0.92f),
        new Passive("Wide Lens", "+10% area", new Color(0.7f, 0.6f, 1f), 5, p => p.Area += 0.1f),
        new Passive("Lodestone", "+30% pickup range", new Color(0.4f, 1f, 1f), 5, p => p.Magnet *= 1.3f),
        new Passive("Troll Blood", "+0.4 HP regeneration per second", new Color(0.4f, 1f, 0.5f), 5, p => p.Regen += 0.4f),
        new Passive("Plate Armor", "+1 armor (reduces damage taken)", new Color(0.6f, 0.65f, 0.7f), 5, p => p.Armor += 1f),
        new Passive("Twin Charm", "+1 projectile / orb / strike for all weapons", new Color(1f, 0.5f, 0.9f), 2, p => p.Amount += 1),
    };
}

public class UpgradeOption
{
    public string Title, Desc;
    public Color Color;
    public Action Apply;
}

public static class Upgrades
{
    public static List<UpgradeOption> Roll(Player p, int count)
    {
        var pool = new List<UpgradeOption>();

        for (int i = 0; i < WeaponCatalog.Count; i++)
        {
            int index = i;
            var proto = WeaponCatalog.Prototype(i);
            var owned = p.FindWeapon(proto.GetType());
            if (owned != null)
            {
                if (owned.Level < Weapon.MaxLevel)
                    pool.Add(new UpgradeOption
                    {
                        Title = $"{owned.Name}  Lv {owned.Level + 1}",
                        Desc = owned.LevelDesc(owned.Level + 1),
                        Color = owned.Color,
                        Apply = owned.LevelUp,
                    });
            }
            else if (p.Weapons.Count < Player.MaxWeapons)
            {
                pool.Add(new UpgradeOption
                {
                    Title = "NEW  " + proto.Name,
                    Desc = proto.Description,
                    Color = proto.Color,
                    Apply = () => p.AddWeapon(WeaponCatalog.Create(index)),
                });
            }
        }

        bool canTakeNewPassive = p.PassiveCount() < Player.MaxPassives;
        for (int i = 0; i < Passives.All.Length; i++)
        {
            int index = i;
            var passive = Passives.All[i];
            int level = p.PassiveLevels[i];
            if (level >= passive.MaxLevel || (level == 0 && !canTakeNewPassive)) continue;
            pool.Add(new UpgradeOption
            {
                Title = level == 0 ? "NEW  " + passive.Name : $"{passive.Name}  Lv {level + 1}",
                Desc = passive.Desc,
                Color = passive.Color,
                Apply = () => { p.PassiveLevels[index]++; passive.Apply(p); },
            });
        }

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        if (pool.Count > count) pool.RemoveRange(count, pool.Count - count);

        if (pool.Count == 0)
        {
            pool.Add(new UpgradeOption { Title = "Roast Chicken", Desc = "Heal 40 HP", Color = new Color(1f, 0.6f, 0.4f), Apply = () => p.Heal(40f) });
            pool.Add(new UpgradeOption { Title = "Gold Pouch", Desc = "+25 gold", Color = new Color(1f, 0.85f, 0.2f), Apply = () => GameManager.I.AddGold(25) });
        }
        return pool;
    }
}
