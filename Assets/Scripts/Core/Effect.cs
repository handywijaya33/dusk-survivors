using UnityEngine;

/// <summary>A sprite that fades out (and optionally grows) and then destroys itself.</summary>
public class Effect : MonoBehaviour
{
    SpriteRenderer sr;
    Color color;
    Vector3 startScale, endScale;
    float life, t;

    public static Effect Spawn(Sprite sprite, Vector2 pos, Color color, Vector2 scale, float life, float growTo = 1f, int order = 20)
    {
        var go = SpriteFactory.MakeSprite("Effect", sprite, color, scale, order, GameManager.I.RunRoot);
        go.transform.position = pos;
        var e = go.AddComponent<Effect>();
        e.sr = go.GetComponent<SpriteRenderer>();
        e.color = color;
        e.life = life;
        e.startScale = go.transform.localScale;
        e.endScale = e.startScale * growTo;
        return e;
    }

    public static Effect Spawn(Sprite sprite, Vector2 pos, Color color, float scale, float life, float growTo = 1f, int order = 20)
    {
        return Spawn(sprite, pos, color, new Vector2(scale, scale), life, growTo, order);
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = t / life;
        if (k >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        sr.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
        transform.localScale = Vector3.Lerp(startScale, endScale, k);
    }
}
