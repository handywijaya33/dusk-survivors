using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public abstract class Weapon
{
    public const int MaxLevel = 6;

    public int Level { get; private set; } = 1;
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract Color Color { get; }
    /// <summary>Upgrade text for levels 2..MaxLevel.</summary>
    protected abstract string[] LevelDescs { get; }

    protected Player P;
    protected float Timer;

    public string LevelDesc(int level) => LevelDescs[Mathf.Clamp(level - 2, 0, LevelDescs.Length - 1)];
    public virtual void Init(Player p) => P = p;
    public void LevelUp() { if (Level < MaxLevel) Level++; }
    public abstract void Tick(float dt);

    protected float Dmg(float d) => d * P.Might;
    protected float Cd(float c) => Mathf.Max(0.05f, c * P.CooldownMul);
    protected static int B(bool b) => b ? 1 : 0;
    protected static Vector2 Rotate(Vector2 v, float degrees) => Quaternion.Euler(0f, 0f, degrees) * v;
}

public static class WeaponCatalog
{
    static readonly Func<Weapon>[] factories =
    {
        () => new ArcaneBolt(),
        () => new ThrowingKnives(),
        () => new HolyAura(),
        () => new SpiritOrbs(),
        () => new StormCall(),
        () => new HandAxe(),
    };

    static Weapon[] prototypes;

    public static int Count => factories.Length;

    public static Weapon Create(int index) => factories[Mathf.Clamp(index, 0, Count - 1)]();

    /// <summary>A shared, uninitialised instance used only to read names and descriptions.</summary>
    public static Weapon Prototype(int index)
    {
        if (prototypes == null)
        {
            prototypes = new Weapon[Count];
            for (int i = 0; i < Count; i++) prototypes[i] = factories[i]();
        }
        return prototypes[Mathf.Clamp(index, 0, Count - 1)];
    }
}

public static class Targeting
{
    static readonly List<Enemy> buffer = new List<Enemy>();
    static Vector2 origin;
    static readonly Comparison<Enemy> byDistance =
        (a, b) => (a.Pos - origin).sqrMagnitude.CompareTo((b.Pos - origin).sqrMagnitude);

    /// <summary>Living enemies within range. The returned list is reused; consume it immediately.</summary>
    public static List<Enemy> InRange(Vector2 from, float range)
    {
        buffer.Clear();
        float sq = range * range;
        foreach (var e in Enemy.All)
            if (!e.Dead && (e.Pos - from).sqrMagnitude <= sq) buffer.Add(e);
        return buffer;
    }

    public static List<Enemy> Nearest(Vector2 from, int count, float range)
    {
        InRange(from, range);
        origin = from;
        buffer.Sort(byDistance);
        if (buffer.Count > count) buffer.RemoveRange(count, buffer.Count - count);
        return buffer;
    }
}

// ---------------------------------------------------------------------------

public class ArcaneBolt : Weapon
{
    static readonly string[] descs =
    {
        "+1 bolt, +3 damage", "-20% cooldown, +3 damage", "Bolts pierce 2 enemies, +3 damage",
        "+1 bolt, +3 damage", "-20% cooldown, +3 damage",
    };

    public override string Name => "Arcane Bolt";
    public override string Description => "Fires magic bolts at the nearest enemies.";
    public override Color Color => new Color(0.45f, 0.7f, 1f);
    protected override string[] LevelDescs => descs;

    float Damage => 10f + 3f * (Level - 1);
    int Count => 1 + B(Level >= 2) + B(Level >= 5) + P.Amount;
    float Cooldown => 1.2f * (Level >= 3 ? 0.8f : 1f) * (Level >= 6 ? 0.8f : 1f);
    int Pierce => Level >= 4 ? 2 : 0;

    public override void Tick(float dt)
    {
        Timer -= dt;
        if (Timer > 0f) return;

        int count = Count;
        var targets = Targeting.Nearest(P.Pos, count, 13f);
        if (targets.Count == 0) { Timer = 0.15f; return; }
        Timer = Cd(Cooldown);

        for (int i = 0; i < count; i++)
        {
            Vector2 dir = (targets[i % targets.Count].Pos - P.Pos).normalized;
            if (i >= targets.Count) dir = Rotate(dir, (i % 2 == 0 ? 1f : -1f) * 12f * (i - targets.Count + 1));
            Projectile.Spawn(SpriteFactory.Circle, Color, P.Pos, dir * 11f, 0.35f * P.Area, Dmg(Damage), Pierce, 2f, 1.5f);
        }
    }
}

public class ThrowingKnives : Weapon
{
    static readonly string[] descs =
    {
        "+1 knife, +2 damage", "-20% cooldown, +2 damage", "+1 knife, +2 damage",
        "Knives pierce 2 enemies, +2 damage", "+1 knife, +2 damage",
    };

    public override string Name => "Throwing Knives";
    public override string Description => "Rapidly throws knives in the direction you are moving.";
    public override Color Color => new Color(0.85f, 0.9f, 1f);
    protected override string[] LevelDescs => descs;

    float Damage => 7f + 2f * (Level - 1);
    int Count => 1 + B(Level >= 2) + B(Level >= 4) + B(Level >= 6) + P.Amount;
    float Cooldown => 0.8f * (Level >= 3 ? 0.8f : 1f);
    int Pierce => Level >= 5 ? 2 : 0;

    public override void Tick(float dt)
    {
        Timer -= dt;
        if (Timer > 0f) return;
        Timer = Cd(Cooldown);

        int count = Count;
        Vector2 dir = P.Facing;
        Vector2 perp = new Vector2(-dir.y, dir.x);
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) * 0.5f) * 0.3f;
            var p = Projectile.Spawn(SpriteFactory.Triangle, Color, P.Pos + perp * offset, dir * 15f,
                new Vector2(0.6f, 0.22f) * P.Area, Dmg(Damage), Pierce, 1.2f, 1f);
            p.AlignToVelocity = true;
        }
    }
}

public class HolyAura : Weapon
{
    static readonly string[] descs =
    {
        "+Area, +2 damage", "+Area, +2 damage", "Pulses 25% faster, +2 damage",
        "+Area, +2 damage", "+Area, +2 damage, stronger knockback",
    };

    public override string Name => "Holy Aura";
    public override string Description => "Damages and pushes back nearby enemies.";
    public override Color Color => new Color(1f, 0.95f, 0.6f);
    protected override string[] LevelDescs => descs;

    float Damage => 4f + 2f * (Level - 1);
    float Radius => (1.5f + 0.2f * (Level - 1)) * P.Area;
    float Interval => Level >= 4 ? 0.45f : 0.6f;
    float Knockback => Level >= 6 ? 4f : 2f;

    SpriteRenderer visual;
    float pulse;

    public override void Init(Player p)
    {
        base.Init(p);
        visual = SpriteFactory.MakeSprite("Aura", SpriteFactory.Circle, Color, 1f, 1, GameManager.I.RunRoot).GetComponent<SpriteRenderer>();
    }

    public override void Tick(float dt)
    {
        float radius = Radius;
        visual.transform.position = P.Pos;
        visual.transform.localScale = Vector3.one * radius * 2f;
        pulse = Mathf.Max(0f, pulse - dt * 2f);
        visual.color = new Color(Color.r, Color.g, Color.b, 0.12f + pulse * 0.25f);

        Timer -= dt;
        if (Timer > 0f) return;
        Timer = Cd(Interval);
        pulse = 1f;

        foreach (var e in Enemy.All)
        {
            if (e.Dead) continue;
            float r = radius + e.Radius;
            Vector2 d = e.Pos - P.Pos;
            if (d.sqrMagnitude < r * r) e.Hit(Dmg(Damage), d.normalized, Knockback);
        }
    }
}

public class SpiritOrbs : Weapon
{
    static readonly string[] descs =
    {
        "+1 orb, +3 damage", "+20% rotation speed, +3 damage", "+1 orb, +3 damage",
        "+25% orbit size, +3 damage", "+1 orb, +3 damage",
    };

    public override string Name => "Spirit Orbs";
    public override string Description => "Orbs circle around you, hitting anything they touch.";
    public override Color Color => new Color(0.5f, 1f, 0.8f);
    protected override string[] LevelDescs => descs;

    float Damage => 8f + 3f * (Level - 1);
    int Count => 2 + B(Level >= 2) + B(Level >= 4) + B(Level >= 6) + P.Amount;
    float RotSpeed => 3f * (Level >= 3 ? 1.2f : 1f);
    float OrbitRadius => 1.8f * (Level >= 5 ? 1.25f : 1f) * P.Area;
    float OrbSize => 0.5f * P.Area;

    readonly List<Transform> orbs = new List<Transform>();
    readonly Dictionary<Enemy, float> lastHit = new Dictionary<Enemy, float>();
    float angle, clock;

    public override void Tick(float dt)
    {
        clock += dt;
        int count = Count;
        while (orbs.Count < count)
            orbs.Add(SpriteFactory.MakeSprite("Orb", SpriteFactory.Circle, Color, OrbSize, 12, GameManager.I.RunRoot).transform);
        while (orbs.Count > count)
        {
            Object.Destroy(orbs[orbs.Count - 1].gameObject);
            orbs.RemoveAt(orbs.Count - 1);
        }

        angle += RotSpeed * dt;
        float radius = OrbitRadius, size = OrbSize;
        for (int i = 0; i < count; i++)
        {
            float a = angle + i * Mathf.PI * 2f / count;
            Vector2 op = P.Pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            orbs[i].position = op;
            orbs[i].localScale = new Vector3(size, size, 1f);

            foreach (var e in Enemy.All)
            {
                if (e.Dead) continue;
                float r = size * 0.5f + e.Radius;
                if ((e.Pos - op).sqrMagnitude > r * r) continue;
                if (lastHit.TryGetValue(e, out float t) && clock - t < 0.5f) continue;
                lastHit[e] = clock;
                e.Hit(Dmg(Damage), (e.Pos - P.Pos).normalized, 3f);
            }
        }

        if (lastHit.Count > 600) lastHit.Clear();
    }
}

public class StormCall : Weapon
{
    static readonly string[] descs =
    {
        "+1 strike, +6 damage", "-20% cooldown, +6 damage", "+1 strike, +6 damage",
        "+40% blast radius, +6 damage", "+1 strike, +6 damage",
    };

    public override string Name => "Storm Call";
    public override string Description => "Lightning strikes random enemies on screen.";
    public override Color Color => new Color(0.75f, 0.6f, 1f);
    protected override string[] LevelDescs => descs;

    float Damage => 20f + 6f * (Level - 1);
    int Count => 1 + B(Level >= 2) + B(Level >= 4) + B(Level >= 6) + P.Amount;
    float Cooldown => 2.6f * (Level >= 3 ? 0.8f : 1f);
    float BlastRadius => 1f * (Level >= 5 ? 1.4f : 1f) * P.Area;

    readonly List<Vector2> strikes = new List<Vector2>();

    public override void Tick(float dt)
    {
        Timer -= dt;
        if (Timer > 0f) return;

        var candidates = Targeting.InRange(P.Pos, GameManager.I.ViewRadius * 0.85f);
        if (candidates.Count == 0) { Timer = 0.2f; return; }
        Timer = Cd(Cooldown);

        // Copy positions first: striking damages enemies, and the candidate list is a shared buffer.
        strikes.Clear();
        for (int i = 0; i < Count; i++) strikes.Add(candidates[Random.Range(0, candidates.Count)].Pos);
        foreach (var s in strikes) Strike(s);
    }

    void Strike(Vector2 pos)
    {
        float radius = BlastRadius;
        Effect.Spawn(SpriteFactory.Square, pos + Vector2.up * 6f, Color, new Vector2(0.18f, 12f), 0.25f);
        Effect.Spawn(SpriteFactory.Circle, pos, new Color(Color.r, Color.g, Color.b, 0.7f), radius, 0.3f, 2f);

        foreach (var e in Enemy.All)
        {
            if (e.Dead) continue;
            float r = radius + e.Radius;
            Vector2 d = e.Pos - pos;
            if (d.sqrMagnitude < r * r) e.Hit(Dmg(Damage), d.normalized, 2f);
        }
    }
}

public class HandAxe : Weapon
{
    static readonly string[] descs =
    {
        "+1 axe, +7 damage", "+30% size, +7 damage", "+1 axe, +7 damage",
        "-20% cooldown, +7 damage", "+1 axe, +7 damage",
    };

    public override string Name => "Hand Axe";
    public override string Description => "Hurls heavy axes in an arc. High damage, pierces.";
    public override Color Color => new Color(1f, 0.6f, 0.25f);
    protected override string[] LevelDescs => descs;

    float Damage => 22f + 7f * (Level - 1);
    int Count => 1 + B(Level >= 2) + B(Level >= 4) + B(Level >= 6) + P.Amount;
    float Cooldown => 2.2f * (Level >= 5 ? 0.8f : 1f);
    float Size => 0.7f * (Level >= 3 ? 1.3f : 1f) * P.Area;

    public override void Tick(float dt)
    {
        Timer -= dt;
        if (Timer > 0f) return;
        Timer = Cd(Cooldown);

        int count = Count;
        for (int i = 0; i < count; i++)
        {
            float vx = (i - (count - 1) * 0.5f) * 2.2f + P.Facing.x * 1.5f + Random.Range(-0.5f, 0.5f);
            float vy = Random.Range(10f, 12.5f);
            var p = Projectile.Spawn(SpriteFactory.Diamond, Color, P.Pos, new Vector2(vx, vy), Size, Dmg(Damage), 5, 3f, 2f);
            p.Gravity = -20f;
            p.Spin = -540f;
        }
    }
}
