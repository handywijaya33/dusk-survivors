using System.Collections.Generic;
using UnityEngine;

public enum PickupKind { Xp, Heal, Gold, Vacuum, Chest }

public class Pickup : MonoBehaviour
{
    const int MaxGems = 250;
    public static readonly List<Pickup> Gems = new List<Pickup>();

    public PickupKind Kind;
    public int Value;
    public bool Magnetized;

    SpriteRenderer sr;
    float speed;
    bool collected;

    public static void Spawn(PickupKind kind, Vector2 pos, int value)
    {
        // Too many gems on the ground: fold the value into an existing one instead.
        if (kind == PickupKind.Xp && Gems.Count >= MaxGems)
        {
            var g = Gems[Random.Range(0, Gems.Count)];
            g.Value += value;
            g.RefreshLook();
            return;
        }

        var go = new GameObject(kind.ToString());
        go.transform.SetParent(GameManager.I.RunRoot, false);
        go.transform.position = pos;
        var p = go.AddComponent<Pickup>();
        p.Kind = kind;
        p.Value = value;
        p.sr = go.AddComponent<SpriteRenderer>();
        p.sr.sortingOrder = 2;
        p.RefreshLook();
        if (kind == PickupKind.Xp) Gems.Add(p);
    }

    void OnDestroy()
    {
        if (Kind == PickupKind.Xp) Gems.Remove(this);
    }

    void RefreshLook()
    {
        Sprite sprite;
        Color color;
        float size;
        switch (Kind)
        {
            case PickupKind.Xp:
                sprite = SpriteFactory.Diamond;
                if (Value >= 20) { color = new Color(1f, 0.35f, 0.35f); size = 0.5f; }
                else if (Value >= 5) { color = new Color(0.35f, 1f, 0.45f); size = 0.4f; }
                else { color = new Color(0.35f, 0.65f, 1f); size = 0.3f; }
                break;
            case PickupKind.Heal:
                sprite = SpriteFactory.Circle; color = new Color(1f, 0.4f, 0.5f); size = 0.45f;
                break;
            case PickupKind.Gold:
                sprite = SpriteFactory.Circle; color = new Color(1f, 0.85f, 0.2f); size = Value >= 10 ? 0.5f : 0.3f;
                break;
            case PickupKind.Vacuum:
                sprite = SpriteFactory.Ring; color = new Color(0.4f, 1f, 1f); size = 0.55f;
                break;
            default:
                sprite = SpriteFactory.Square; color = new Color(0.85f, 0.6f, 0.15f); size = 0.7f;
                break;
        }
        sr.sprite = sprite;
        sr.color = color;
        transform.localScale = new Vector3(size, size, 1f);
    }

    void Update()
    {
        if (collected || !GameManager.I.Playing) return;
        var p = Player.I;
        if (p == null) return;

        Vector2 pos = transform.position;
        Vector2 d = p.Pos - pos;
        float dist = d.magnitude;

        if (!Magnetized && dist < p.Magnet)
        {
            Magnetized = true;
            speed = -3f; // small hop away before flying in
        }
        if (Magnetized)
        {
            speed += 40f * Time.deltaTime;
            transform.position = pos + d / Mathf.Max(dist, 0.001f) * speed * Time.deltaTime;
        }
        if (dist < 0.5f) Collect(p);
    }

    void Collect(Player p)
    {
        collected = true;
        var gm = GameManager.I;
        switch (Kind)
        {
            case PickupKind.Xp:
                gm.AddXp(Value);
                break;
            case PickupKind.Heal:
                p.Heal(Value);
                gm.ShowText(p.Pos, "+" + Value, new Color(0.4f, 1f, 0.5f));
                break;
            case PickupKind.Gold:
                gm.AddGold(Value);
                break;
            case PickupKind.Vacuum:
                foreach (var g in Gems) g.Magnetized = true;
                break;
            case PickupKind.Chest:
                gm.ShowBanner("Treasure!");
                gm.GrantLevelUp();
                break;
        }
        Destroy(gameObject);
    }
}
