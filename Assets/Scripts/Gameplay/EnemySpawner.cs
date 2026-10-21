using UnityEngine;

/// <summary>Spawns enemies just outside the camera, ramping up difficulty over the run.</summary>
public class EnemySpawner
{
    const int MaxEnemies = 350;

    float timer = 0.5f;
    float nextBoss = 120f;
    float nextRing = 90f;

    public void Tick(float dt, float t)
    {
        var gm = GameManager.I;

        timer -= dt;
        if (timer <= 0f)
        {
            timer = Mathf.Lerp(0.9f, 0.12f, Mathf.Clamp01(t / 480f));
            int batch = 1 + Mathf.FloorToInt(t / 75f);
            for (int i = 0; i < batch && Enemy.All.Count < MaxEnemies; i++)
                Spawn(Pick(t), gm.RandomSpawnPoint(), t);
        }

        if (t >= nextRing)
        {
            nextRing += 180f;
            Ring(t);
        }

        if (t >= nextBoss)
        {
            nextBoss += 120f;
            Spawn(EnemyTypes.Reaper, gm.RandomSpawnPoint(), t);
            gm.ShowBanner("A Reaper approaches!");
        }
    }

    static float HpMul(float t) => 1f + t / 80f;
    static float DmgMul(float t) => 1f + t / 300f;

    static void Spawn(EnemyType type, Vector2 pos, float t) => Enemy.Spawn(type, pos, HpMul(t), DmgMul(t));

    static EnemyType Pick(float t)
    {
        float bat = 1f;
        float zombie = t >= 40f ? 1f : 0f;
        float ghost = t >= 120f ? 0.8f : 0f;
        float brute = t >= 210f ? 0.35f + t / 1200f : 0f;

        float r = Random.value * (bat + zombie + ghost + brute);
        if ((r -= bat) < 0f) return EnemyTypes.Bat;
        if ((r -= zombie) < 0f) return EnemyTypes.Zombie;
        if ((r -= ghost) < 0f) return EnemyTypes.Ghost;
        return EnemyTypes.Brute;
    }

    /// <summary>Surrounds the player with a closing circle of enemies.</summary>
    static void Ring(float t)
    {
        var gm = GameManager.I;
        var type = t < 200f ? EnemyTypes.Bat : EnemyTypes.Ghost;
        const int count = 36;
        float radius = gm.ViewRadius + 0.5f;
        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.PI * 2f / count;
            Spawn(type, Player.I.Pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, t);
        }
        gm.ShowBanner("They're surrounding you!");
    }
}
