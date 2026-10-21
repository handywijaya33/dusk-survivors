using System;
using System.Collections.Generic;
using UnityEngine;

public enum Shape { Circle, Square, Diamond, Triangle, Ring }

/// <summary>
/// Generates simple white sprites at runtime so the project needs no art assets.
/// Tint them with SpriteRenderer.color.
/// </summary>
public static class SpriteFactory
{
    static readonly Dictionary<Shape, Sprite> cache = new Dictionary<Shape, Sprite>();

    public static Sprite Circle => Get(Shape.Circle);
    public static Sprite Square => Get(Shape.Square);
    public static Sprite Diamond => Get(Shape.Diamond);
    public static Sprite Triangle => Get(Shape.Triangle);
    public static Sprite Ring => Get(Shape.Ring);

    public static Sprite Get(Shape shape)
    {
        if (cache.TryGetValue(shape, out var s) && s != null) return s;
        switch (shape)
        {
            case Shape.Circle:
                s = Build(64, (x, y, n) =>
                {
                    float r = n * 0.5f;
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    return r - d;
                });
                break;
            case Shape.Ring:
                s = Build(128, (x, y, n) =>
                {
                    float r = n * 0.5f;
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    return Mathf.Min(r - d, d - (r - 6f));
                });
                break;
            case Shape.Square:
                s = Build(16, (x, y, n) => 1f);
                break;
            case Shape.Diamond:
                s = Build(64, (x, y, n) =>
                {
                    float c = n * 0.5f;
                    return c - (Mathf.Abs(x + 0.5f - c) + Mathf.Abs(y + 0.5f - c));
                });
                break;
            case Shape.Triangle: // points to +X
                s = Build(64, (x, y, n) =>
                {
                    float c = n * 0.5f;
                    float halfWidth = (n - (x + 0.5f)) * 0.5f;
                    return Mathf.Min(halfWidth - Mathf.Abs(y + 0.5f - c), x + 0.5f);
                });
                break;
        }
        cache[shape] = s;
        return s;
    }

    static Sprite Build(int size, Func<int, int, int, float> coverage)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                byte a = (byte)(Mathf.Clamp01(coverage(x, y, size)) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    public static GameObject MakeSprite(string name, Sprite sprite, Color color, float scale, int order, Transform parent)
    {
        return MakeSprite(name, sprite, color, new Vector2(scale, scale), order, parent);
    }

    public static GameObject MakeSprite(string name, Sprite sprite, Color color, Vector2 scale, int order, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    /// <summary>Adds a pair of eyes to a 1-unit body. Returns the container so it can be shifted toward the look direction.</summary>
    public static Transform AddEyes(Transform body, int order)
    {
        var eyes = new GameObject("Eyes").transform;
        eyes.SetParent(body, false);
        var c = new Color(0.08f, 0.06f, 0.1f);
        MakeSprite("EyeL", Circle, c, 0.2f, order + 1, eyes).transform.localPosition = new Vector3(-0.17f, 0.1f, 0f);
        MakeSprite("EyeR", Circle, c, 0.2f, order + 1, eyes).transform.localPosition = new Vector3(0.17f, 0.1f, 0f);
        return eyes;
    }
}
