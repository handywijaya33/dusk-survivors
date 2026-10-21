using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player I { get; private set; }

    public const int MaxWeapons = 6;
    public const int MaxPassives = 6;
    static readonly Color BodyColor = new Color(1f, 0.85f, 0.45f);

    // Stats (modified by meta upgrades and passives)
    public float MaxHp = 100f;
    public float Hp = 100f;
    public float Speed = 4f;
    public float Regen;
    public float Armor;
    public float Might = 1f;
    public float Area = 1f;
    public float CooldownMul = 1f;
    public float Magnet = 2f;
    public float Greed = 1f;
    public float Growth = 1f;
    public int Amount;
    public int Revives;

    public readonly float Radius = 0.35f;
    public Vector2 Pos;
    public Vector2 Facing = Vector2.right;

    public readonly List<Weapon> Weapons = new List<Weapon>();
    public readonly int[] PassiveLevels = new int[Passives.All.Length];

    SpriteRenderer sr;
    Transform eyes;
    float invuln;

    public static Player Create(Transform parent, int startWeapon)
    {
        var go = SpriteFactory.MakeSprite("Player", SpriteFactory.Circle, BodyColor, 0.8f, 10, parent);
        var p = go.AddComponent<Player>();
        p.sr = go.GetComponent<SpriteRenderer>();
        p.eyes = SpriteFactory.AddEyes(go.transform, 10);
        foreach (var m in MetaUpgrades.All) m.ApplyTo(p, SaveData.GetLevel(m.Key));
        p.Hp = p.MaxHp;
        p.AddWeapon(WeaponCatalog.Create(startWeapon));
        return p;
    }

    void Awake() => I = this;

    void OnDestroy()
    {
        if (I == this) I = null;
    }

    void Update()
    {
        if (!GameManager.I.Playing) return;
        float dt = Time.deltaTime;

        Vector2 move = GameInput.Move();
        if (move.sqrMagnitude > 0.01f) Facing = move.normalized;
        Pos += move * Speed * dt;
        transform.position = Pos;
        eyes.localPosition = Facing * 0.12f;

        if (Regen > 0f) Heal(Regen * dt);

        if (invuln > 0f)
        {
            invuln -= dt;
            bool blink = invuln > 0f && Mathf.Repeat(invuln, 0.12f) < 0.06f;
            sr.color = blink ? new Color(1f, 0.3f, 0.3f) : BodyColor;
        }

        for (int i = 0; i < Weapons.Count; i++) Weapons[i].Tick(dt);
    }

    public void TakeDamage(float dmg)
    {
        if (invuln > 0f || Hp <= 0f) return;
        Hp -= Mathf.Max(1f, dmg - Armor);
        invuln = 0.3f;
        if (Hp > 0f) return;

        if (Revives > 0)
        {
            Revives--;
            Hp = MaxHp * 0.5f;
            invuln = 2f;
            foreach (var e in Enemy.All)
            {
                Vector2 d = e.Pos - Pos;
                if (d.sqrMagnitude < 49f) e.Hit(60f * Might, d.normalized, 12f);
            }
            Effect.Spawn(SpriteFactory.Ring, Pos, new Color(1f, 0.9f, 0.5f), 1f, 0.6f, 14f);
            GameManager.I.ShowBanner("REVIVED!");
        }
        else
        {
            GameManager.I.EndRun(false);
        }
    }

    public void Heal(float amount) => Hp = Mathf.Min(MaxHp, Hp + amount);

    public Weapon FindWeapon(Type type) => Weapons.Find(w => w.GetType() == type);

    public void AddWeapon(Weapon w)
    {
        w.Init(this);
        Weapons.Add(w);
    }

    public int PassiveCount()
    {
        int n = 0;
        foreach (int l in PassiveLevels) if (l > 0) n++;
        return n;
    }
}
