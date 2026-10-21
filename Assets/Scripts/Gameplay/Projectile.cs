using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector2 Vel;
    public float Damage, Radius, Life, Knockback;
    public float Gravity, Spin;
    public bool AlignToVelocity;
    public int Pierce;

    Vector2 pos;
    readonly HashSet<Enemy> alreadyHit = new HashSet<Enemy>();

    public static Projectile Spawn(Sprite sprite, Color color, Vector2 pos, Vector2 vel, Vector2 scale,
        float damage, int pierce, float life, float knockback)
    {
        var go = SpriteFactory.MakeSprite("Projectile", sprite, color, scale, 15, GameManager.I.RunRoot);
        go.transform.position = pos;
        var p = go.AddComponent<Projectile>();
        p.pos = pos;
        p.Vel = vel;
        p.Damage = damage;
        p.Pierce = pierce;
        p.Life = life;
        p.Knockback = knockback;
        p.Radius = Mathf.Max(scale.x, scale.y) * 0.4f;
        return p;
    }

    public static Projectile Spawn(Sprite sprite, Color color, Vector2 pos, Vector2 vel, float scale,
        float damage, int pierce, float life, float knockback)
    {
        return Spawn(sprite, color, pos, vel, new Vector2(scale, scale), damage, pierce, life, knockback);
    }

    void Update()
    {
        if (!GameManager.I.Playing) return;
        float dt = Time.deltaTime;

        Vel.y += Gravity * dt;
        pos += Vel * dt;
        transform.position = pos;
        if (Spin != 0f) transform.Rotate(0f, 0f, Spin * dt);
        else if (AlignToVelocity) transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(Vel.y, Vel.x) * Mathf.Rad2Deg);

        Life -= dt;
        if (Life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        var all = Enemy.All;
        for (int i = 0; i < all.Count; i++)
        {
            var e = all[i];
            if (e.Dead || alreadyHit.Contains(e)) continue;
            float r = Radius + e.Radius;
            if ((e.Pos - pos).sqrMagnitude > r * r) continue;

            alreadyHit.Add(e);
            e.Hit(Damage, Vel.normalized, Knockback);
            if (--Pierce < 0)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
