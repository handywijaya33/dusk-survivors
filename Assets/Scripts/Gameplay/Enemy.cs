using System.Collections.Generic;
using UnityEngine;

public class EnemyType
{
    public string Name;
    public float Hp, Speed, Damage, Size;
    public int Xp;
    public Color Color;
    public Shape Shape;
    public bool Boss;
}

public static class EnemyTypes
{
    public static readonly EnemyType Bat = new EnemyType
        { Name = "Bat", Hp = 5f, Speed = 2.5f, Damage = 5f, Size = 0.55f, Xp = 1, Color = new Color(0.62f, 0.42f, 0.88f), Shape = Shape.Circle };
    public static readonly EnemyType Zombie = new EnemyType
        { Name = "Zombie", Hp = 14f, Speed = 1.5f, Damage = 8f, Size = 0.85f, Xp = 2, Color = new Color(0.42f, 0.72f, 0.36f), Shape = Shape.Square };
    public static readonly EnemyType Ghost = new EnemyType
        { Name = "Ghost", Hp = 10f, Speed = 3.3f, Damage = 6f, Size = 0.65f, Xp = 2, Color = new Color(0.6f, 0.95f, 1f, 0.75f), Shape = Shape.Circle };
    public static readonly EnemyType Brute = new EnemyType
        { Name = "Brute", Hp = 55f, Speed = 1.15f, Damage = 14f, Size = 1.35f, Xp = 6, Color = new Color(0.62f, 0.4f, 0.3f), Shape = Shape.Square };
    public static readonly EnemyType Reaper = new EnemyType
        { Name = "Reaper", Hp = 700f, Speed = 2.1f, Damage = 25f, Size = 2.4f, Xp = 40, Color = new Color(0.9f, 0.18f, 0.22f), Shape = Shape.Diamond, Boss = true };
}

public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();

    public float Hp, MaxHp, Speed, Damage, Radius;
    public int Xp;
    public bool IsBoss, Dead;
    public Vector2 Pos;
    public string DisplayName;

    SpriteRenderer sr;
    Transform eyes;
    Color baseColor;
    float flash;
    Vector2 knock;

    public static Enemy Spawn(EnemyType type, Vector2 pos, float hpMul, float dmgMul)
    {
        var go = SpriteFactory.MakeSprite(type.Name, SpriteFactory.Get(type.Shape), type.Color, type.Size, type.Boss ? 6 : 5, GameManager.I.RunRoot);
        go.transform.position = pos;
        var e = go.AddComponent<Enemy>();
        e.sr = go.GetComponent<SpriteRenderer>();
        e.eyes = SpriteFactory.AddEyes(go.transform, e.sr.sortingOrder);
        e.baseColor = type.Color;
        e.MaxHp = e.Hp = type.Hp * hpMul;
        e.Speed = type.Speed;
        e.Damage = type.Damage * dmgMul;
        e.Radius = type.Size * 0.45f;
        e.Xp = type.Xp;
        e.IsBoss = type.Boss;
        e.Pos = pos;
        e.DisplayName = type.Name;
        return e;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Update()
    {
        if (Dead || !GameManager.I.Playing) return;
        var p = Player.I;
        if (p == null) return;
        float dt = Time.deltaTime;

        Vector2 to = p.Pos - Pos;
        float dist = to.magnitude;

        // Enemies left far behind are moved back near the player, like in Vampire Survivors.
        if (dist > GameManager.I.DespawnDistance && !IsBoss)
        {
            Pos = GameManager.I.RandomSpawnPoint();
            transform.position = Pos;
            return;
        }

        Vector2 dir = dist > 0.001f ? to / dist : Vector2.zero;

        // Soft separation so the horde doesn't collapse into one blob.
        Vector2 push = Vector2.zero;
        if (!IsBoss)
        {
            for (int i = 0; i < All.Count; i++)
            {
                var o = All[i];
                if (o == this) continue;
                Vector2 d = Pos - o.Pos;
                float min = Radius + o.Radius;
                if (d.x > min || d.x < -min || d.y > min || d.y < -min) continue;
                float sq = d.sqrMagnitude;
                if (sq < min * min && sq > 0.00001f)
                {
                    float len = Mathf.Sqrt(sq);
                    push += d / len * (min - len);
                }
            }
        }

        Pos += (dir * Speed + knock) * dt + push * 0.5f;
        knock = Vector2.MoveTowards(knock, Vector2.zero, 25f * dt);
        transform.position = Pos;
        eyes.localPosition = dir * 0.12f;

        if (dist < Radius + p.Radius) p.TakeDamage(Damage);

        if (flash > 0f)
        {
            flash -= dt;
            if (flash <= 0f) sr.color = baseColor;
        }
    }

    public void Hit(float dmg, Vector2 knockDir, float knockback)
    {
        if (Dead) return;
        Hp -= dmg;
        flash = 0.08f;
        sr.color = Color.white;
        if (!IsBoss) knock += knockDir * knockback;
        GameManager.I.ShowDamage(Pos, dmg);
        if (Hp <= 0f) Die();
    }

    void Die()
    {
        Dead = true;
        GameManager.I.OnEnemyKilled(this);
        Effect.Spawn(SpriteFactory.Circle, Pos, baseColor, transform.localScale.x, 0.25f, 1.6f, 4);

        Pickup.Spawn(PickupKind.Xp, Pos, Xp);
        if (IsBoss)
        {
            Pickup.Spawn(PickupKind.Chest, Pos + new Vector2(0.8f, 0f), 1);
            Pickup.Spawn(PickupKind.Gold, Pos + new Vector2(-0.8f, 0f), 25);
        }
        else
        {
            float r = Random.value;
            Vector2 off = Random.insideUnitCircle * 0.3f;
            if (r < 0.012f) Pickup.Spawn(PickupKind.Heal, Pos + off, 30);
            else if (r < 0.016f) Pickup.Spawn(PickupKind.Vacuum, Pos + off, 1);
            else if (r < 0.07f) Pickup.Spawn(PickupKind.Gold, Pos + off, Random.Range(1, 4));
        }
        Destroy(gameObject);
    }
}
