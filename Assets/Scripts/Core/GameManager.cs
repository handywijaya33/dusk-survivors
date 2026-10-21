using System.Collections.Generic;
using UnityEngine;

public enum GameState { Menu, Playing, LevelUp, Paused, GameOver, Victory }

/// <summary>Owns the run lifecycle, camera, experience/levels and all UI (drawn with IMGUI).</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public const float RunLength = 600f; // survive 10 minutes to win

    public GameState State { get; private set; } = GameState.Menu;
    public bool Playing => State == GameState.Playing;

    public Camera Cam { get; private set; }
    public Transform RunRoot { get; private set; }
    public Player Player { get; private set; }

    public float RunTime { get; private set; }
    public int Kills { get; private set; }
    public int Level { get; private set; }
    public float Xp { get; private set; }
    public float XpToNext { get; private set; }
    public int RunGold { get; private set; }

    public float ViewRadius
    {
        get
        {
            float h = Cam.orthographicSize, w = h * Cam.aspect;
            return Mathf.Sqrt(w * w + h * h);
        }
    }

    public float DespawnDistance => ViewRadius * 2f;

    struct FloatingText
    {
        public Vector2 Pos;
        public string Text;
        public Color Color;
        public float T;
    }

    readonly List<FloatingText> texts = new List<FloatingText>();
    List<UpgradeOption> options = new List<UpgradeOption>();
    EnemySpawner spawner;
    Transform background;
    int pendingLevelUps;
    bool goldBanked;
    string banner;
    float bannerTime = -10f;

    void Awake()
    {
        I = this;
        Cam = Camera.main;
        if (Cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            Cam = go.AddComponent<Camera>();
        }
        Cam.orthographic = true;
        Cam.orthographicSize = 7.5f;
        Cam.clearFlags = CameraClearFlags.SolidColor;
        Cam.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
        Cam.transform.position = new Vector3(0f, 0f, -10f);

        background = CreateBackground();
        SetState(GameState.Menu);
    }

    Transform CreateBackground()
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        Color a = new Color(0.12f, 0.14f, 0.12f), b = new Color(0.135f, 0.155f, 0.135f);
        tex.SetPixels(new[] { a, b, b, a });
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);

        var go = new GameObject("Background");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(60f, 60f);
        sr.sortingOrder = -100;
        return go.transform;
    }

    // ------------------------------------------------------------------ run lifecycle

    void SetState(GameState s)
    {
        State = s;
        Time.timeScale = s == GameState.Playing ? 1f : 0f;
    }

    public void StartRun()
    {
        ClearRun();
        RunRoot = new GameObject("Run").transform;
        Player = Player.Create(RunRoot, SaveData.StartWeapon);
        Player.Pos = Vector2.zero;

        RunTime = 0f;
        Kills = 0;
        Level = 1;
        Xp = 0f;
        XpToNext = XpFor(1);
        RunGold = 0;
        pendingLevelUps = 0;
        goldBanked = false;
        spawner = new EnemySpawner();
        texts.Clear();
        bannerTime = -10f;
        SetState(GameState.Playing);
    }

    void ClearRun()
    {
        if (RunRoot != null) Destroy(RunRoot.gameObject);
        RunRoot = null;
        Player = null;
        Enemy.All.Clear();
        Pickup.Gems.Clear();
    }

    public void EndRun(bool victory)
    {
        if (State == GameState.GameOver || State == GameState.Victory) return;
        if (victory) RunGold += 100;
        BankGold();
        SetState(victory ? GameState.Victory : GameState.GameOver);
    }

    void BankGold()
    {
        if (goldBanked) return;
        goldBanked = true;
        SaveData.Gold += RunGold;
        if (RunTime > SaveData.BestTime) SaveData.BestTime = RunTime;
        PlayerPrefs.Save();
    }

    void QuitToMenu()
    {
        BankGold();
        ClearRun();
        SetState(GameState.Menu);
    }

    static float XpFor(int level) => level < 20 ? 5f + (level - 1) * 10f : 195f + (level - 20) * 15f;

    // ------------------------------------------------------------------ events from gameplay

    public void OnEnemyKilled(Enemy e) => Kills++;

    public void AddXp(float amount)
    {
        Xp += amount * Player.Growth;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            Level++;
            XpToNext = XpFor(Level);
            pendingLevelUps++;
        }
        if (pendingLevelUps > 0 && Playing) OpenLevelUp();
    }

    public void AddGold(int amount) => RunGold += Mathf.Max(1, Mathf.RoundToInt(amount * Player.Greed));

    public void GrantLevelUp()
    {
        pendingLevelUps++;
        if (Playing) OpenLevelUp();
    }

    void OpenLevelUp()
    {
        options = Upgrades.Roll(Player, 3);
        SetState(GameState.LevelUp);
    }

    void Choose(int i)
    {
        if (i < 0 || i >= options.Count) return;
        options[i].Apply();
        pendingLevelUps--;
        if (pendingLevelUps > 0) OpenLevelUp();
        else SetState(GameState.Playing);
    }

    public void ShowBanner(string text)
    {
        banner = text;
        bannerTime = Time.unscaledTime;
    }

    public void ShowDamage(Vector2 pos, float amount) => ShowText(pos, Mathf.RoundToInt(amount).ToString(), Color.white);

    public void ShowText(Vector2 pos, string text, Color color)
    {
        if (texts.Count >= 60) texts.RemoveAt(0);
        texts.Add(new FloatingText { Pos = pos + Random.insideUnitCircle * 0.2f, Text = text, Color = color, T = 0f });
    }

    // ------------------------------------------------------------------ frame update

    void Update()
    {
        if (!Playing) return;
        RunTime += Time.deltaTime;
        spawner.Tick(Time.deltaTime, RunTime);
        if (RunTime >= RunLength) EndRun(true);
    }

    void LateUpdate()
    {
        Vector2 focus = Player != null ? Player.Pos : Vector2.zero;
        Cam.transform.position = new Vector3(focus.x, focus.y, -10f);
        // The checker pattern repeats every 2 units, so snapping keeps it seamless.
        background.position = new Vector3(Mathf.Round(focus.x / 2f) * 2f, Mathf.Round(focus.y / 2f) * 2f, 0f);

        float dt = Time.deltaTime;
        for (int i = texts.Count - 1; i >= 0; i--)
        {
            var t = texts[i];
            t.T += dt;
            t.Pos += Vector2.up * dt * 1.2f;
            if (t.T > 0.6f) texts.RemoveAt(i);
            else texts[i] = t;
        }
    }

    // ------------------------------------------------------------------ UI

    GUIStyle title, header, label, center, small, smallCenter, button, card, dmg;
    float uiScale;

    static Texture2D Tex(Color c)
    {
        var t = new Texture2D(1, 1) { hideFlags = HideFlags.DontSave };
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    void EnsureStyles()
    {
        if (title != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true };
        label.normal.textColor = Color.white;
        title = new GUIStyle(label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        title.normal.textColor = new Color(1f, 0.82f, 0.35f);
        header = new GUIStyle(label) { fontSize = 26, fontStyle = FontStyle.Bold };
        center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
        small = new GUIStyle(label) { fontSize = 16, wordWrap = true };
        small.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
        smallCenter = new GUIStyle(small) { alignment = TextAnchor.UpperCenter };
        dmg = new GUIStyle(label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

        button = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
        button.normal.background = Tex(new Color(0.2f, 0.22f, 0.3f));
        button.hover.background = Tex(new Color(0.3f, 0.33f, 0.46f));
        button.active.background = Tex(new Color(0.42f, 0.45f, 0.62f));
        button.normal.textColor = button.hover.textColor = button.active.textColor = Color.white;

        card = new GUIStyle(button);
        card.normal.background = Tex(new Color(0.13f, 0.13f, 0.19f, 0.97f));
        card.hover.background = Tex(new Color(0.22f, 0.22f, 0.32f, 0.97f));
    }

    static void Fill(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void Bar(Rect r, float frac, Color fg, Color bg)
    {
        Fill(r, bg);
        Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(frac), r.height), fg);
    }

    static void Shadowed(Rect r, string text, GUIStyle style)
    {
        var old = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = old;
        GUI.Label(r, text, style);
    }

    Vector2 WorldToGui(Vector2 world)
    {
        Vector3 sp = Cam.WorldToScreenPoint(world);
        return new Vector2(sp.x / uiScale, (Screen.height - sp.y) / uiScale);
    }

    static string FormatTime(float t) => $"{(int)(t / 60f):00}:{(int)(t % 60f):00}";

    void OnGUI()
    {
        EnsureStyles();
        uiScale = Screen.height / 720f;
        float W = Screen.width / uiScale, H = 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));

        if (State != GameState.Menu && Player != null)
        {
            DrawWorldUI();
            DrawHud(W);
        }

        switch (State)
        {
            case GameState.Menu: DrawMenu(W, H); break;
            case GameState.LevelUp: DrawLevelUp(W, H); break;
            case GameState.Paused: DrawPause(W, H); break;
            case GameState.GameOver:
            case GameState.Victory: DrawEnd(W, H); break;
        }

        HandleKeys();
    }

    void HandleKeys()
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown) return;
        var k = e.keyCode;

        switch (State)
        {
            case GameState.Playing when k == KeyCode.Escape || k == KeyCode.P:
                SetState(GameState.Paused); e.Use(); break;
            case GameState.Paused when k == KeyCode.Escape || k == KeyCode.P:
                SetState(GameState.Playing); e.Use(); break;
            case GameState.LevelUp:
                int i = k >= KeyCode.Alpha1 && k <= KeyCode.Alpha3 ? k - KeyCode.Alpha1
                      : k >= KeyCode.Keypad1 && k <= KeyCode.Keypad3 ? k - KeyCode.Keypad1 : -1;
                if (i >= 0) { Choose(i); e.Use(); }
                break;
            case GameState.Menu when k == KeyCode.Return || k == KeyCode.Space:
                StartRun(); e.Use(); break;
            case GameState.GameOver when k == KeyCode.R:
            case GameState.Victory when k == KeyCode.R:
                StartRun(); e.Use(); break;
        }
    }

    void DrawWorldUI()
    {
        if (Event.current.type != EventType.Repaint) return;

        foreach (var t in texts)
        {
            Vector2 g = WorldToGui(t.Pos);
            dmg.normal.textColor = new Color(t.Color.r, t.Color.g, t.Color.b, 1f - t.T / 0.6f);
            GUI.Label(new Rect(g.x - 40, g.y - 12, 80, 24), t.Text, dmg);
        }

        Vector2 hp = WorldToGui(Player.Pos + Vector2.down * 0.6f);
        Bar(new Rect(hp.x - 26, hp.y, 52, 6), Player.Hp / Player.MaxHp, new Color(0.9f, 0.2f, 0.25f), new Color(0f, 0f, 0f, 0.6f));
    }

    void DrawHud(float W)
    {
        // XP bar
        Bar(new Rect(0, 0, W, 22), Xp / XpToNext, new Color(0.3f, 0.55f, 1f), new Color(0f, 0f, 0f, 0.7f));
        GUI.Label(new Rect(W - 90, -1, 85, 24), $"<b>LV {Level}</b>", label);

        Shadowed(new Rect(0, 26, W, 40), FormatTime(RunTime), new GUIStyle(center) { fontSize = 32, fontStyle = FontStyle.Bold });

        Shadowed(new Rect(W - 220, 30, 210, 26), $"Kills  {Kills}", new GUIStyle(label) { alignment = TextAnchor.UpperRight });
        Shadowed(new Rect(W - 220, 56, 210, 26), $"<color=#FFD840>Gold  {RunGold}</color>", new GUIStyle(label) { alignment = TextAnchor.UpperRight });

        float y = 30;
        foreach (var w in Player.Weapons)
        {
            Fill(new Rect(10, y + 5, 12, 12), w.Color);
            Shadowed(new Rect(28, y, 260, 22), $"{w.Name}  <b>{w.Level}</b>", small);
            y += 22;
        }
        y += 6;
        for (int i = 0; i < Passives.All.Length; i++)
        {
            if (Player.PassiveLevels[i] == 0) continue;
            Fill(new Rect(12, y + 7, 8, 8), Passives.All[i].Color);
            Shadowed(new Rect(28, y, 260, 22), $"{Passives.All[i].Name}  <b>{Player.PassiveLevels[i]}</b>", small);
            y += 20;
        }

        // Boss health bar
        foreach (var e in Enemy.All)
        {
            if (!e.IsBoss || e.Dead) continue;
            float bw = Mathf.Min(500f, W - 40f);
            Bar(new Rect((W - bw) / 2, 72, bw, 12), e.Hp / e.MaxHp, new Color(0.9f, 0.15f, 0.2f), new Color(0f, 0f, 0f, 0.7f));
            break;
        }

        float age = Time.unscaledTime - bannerTime;
        if (age < 2.5f && !string.IsNullOrEmpty(banner))
        {
            var style = new GUIStyle(center) { fontSize = 30, fontStyle = FontStyle.Bold };
            style.normal.textColor = new Color(1f, 0.85f, 0.4f, Mathf.Clamp01(2.5f - age));
            Shadowed(new Rect(0, 140, W, 40), banner, style);
        }
    }

    void DrawMenu(float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0.04f, 0.04f, 0.07f, 0.92f));
        Shadowed(new Rect(0, 30, W, 90), "DUSK SURVIVORS", title);
        GUI.Label(new Rect(0, 110, W, 30), "Survive the night for 10 minutes. Your weapons attack on their own.", new GUIStyle(center) { fontSize = 18 });

        float colW = Mathf.Min(480f, (W - 60f) / 2f);
        float lx = W / 2f - colW - 15f, rx = W / 2f + 15f, top = 175f;

        // Left column: starting weapon + start
        GUI.Label(new Rect(lx, top, colW, 34), "Starting weapon", header);
        int n = WeaponCatalog.Count, sel = SaveData.StartWeapon;
        var proto = WeaponCatalog.Prototype(sel);
        if (GUI.Button(new Rect(lx, top + 44, 50, 50), "<", button)) SaveData.StartWeapon = (sel + n - 1) % n;
        if (GUI.Button(new Rect(lx + colW - 50, top + 44, 50, 50), ">", button)) SaveData.StartWeapon = (sel + 1) % n;
        var nameStyle = new GUIStyle(center) { fontSize = 26, fontStyle = FontStyle.Bold };
        nameStyle.normal.textColor = proto.Color;
        GUI.Label(new Rect(lx + 55, top + 44, colW - 110, 50), proto.Name, nameStyle);
        GUI.Label(new Rect(lx, top + 104, colW, 50), proto.Description, smallCenter);

        if (GUI.Button(new Rect(lx, top + 170, colW, 72), "START RUN", new GUIStyle(button) { fontSize = 30 }))
            StartRun();

        GUI.Label(new Rect(lx, top + 260, colW, 140),
            "Move: WASD / Arrow keys / Gamepad\nPause: Esc\nLevel up: click a card or press 1-3\n\n" +
            $"Best time: {FormatTime(SaveData.BestTime)}", small);

        // Right column: permanent upgrades shop
        GUI.Label(new Rect(rx, top, colW, 34), "Permanent upgrades", header);
        GUI.Label(new Rect(rx, top, colW, 34), $"<color=#FFD840><b>{SaveData.Gold} gold</b></color>", new GUIStyle(label) { alignment = TextAnchor.MiddleRight, fontSize = 22 });

        float y = top + 44;
        foreach (var m in MetaUpgrades.All)
        {
            int lvl = SaveData.GetLevel(m.Key);
            bool maxed = lvl >= m.MaxLevel;
            int cost = m.Cost(lvl);
            Fill(new Rect(rx, y, colW, 46), new Color(1f, 1f, 1f, 0.05f));
            GUI.Label(new Rect(rx + 10, y + 1, colW - 140, 24), $"<b>{m.Name}</b>  <size=14>{lvl}/{m.MaxLevel}</size>", label);
            GUI.Label(new Rect(rx + 10, y + 24, colW - 140, 22), m.Desc, small);

            GUI.enabled = !maxed && SaveData.Gold >= cost;
            if (GUI.Button(new Rect(rx + colW - 120, y + 6, 110, 34), maxed ? "MAX" : $"{cost} g", new GUIStyle(button) { fontSize = 17 }))
                SaveData.TryBuy(m);
            GUI.enabled = true;
            y += 52;
        }
        if (GUI.Button(new Rect(rx + colW - 170, y + 6, 170, 30), "Refund all", new GUIStyle(button) { fontSize = 15 }))
            SaveData.Refund();
    }

    void DrawLevelUp(float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.6f));
        Shadowed(new Rect(0, 120, W, 70), "LEVEL UP!", new GUIStyle(title) { fontSize = 52 });

        int n = options.Count;
        float gap = 20f;
        float cw = Mathf.Min(300f, (W - 40f - gap * (n - 1)) / n), ch = 260f;
        float x = (W - (cw * n + gap * (n - 1))) / 2f, y = H / 2f - ch / 2f + 30f;

        for (int i = 0; i < n; i++)
        {
            var o = options[i];
            var r = new Rect(x + i * (cw + gap), y, cw, ch);
            if (GUI.Button(r, GUIContent.none, card)) Choose(i);
            if (i >= options.Count || options[i] != o) break; // options were rerolled by the click

            Fill(new Rect(r.x, r.y, r.width, 8), o.Color);
            var t = new GUIStyle(center) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
            t.normal.textColor = o.Color;
            GUI.Label(new Rect(r.x + 12, r.y + 30, r.width - 24, 60), o.Title, t);
            GUI.Label(new Rect(r.x + 16, r.y + 105, r.width - 32, 110), o.Desc, new GUIStyle(smallCenter) { fontSize = 18 });
            GUI.Label(new Rect(r.x, r.yMax - 36, r.width, 30), $"[{i + 1}]", new GUIStyle(center) { fontSize = 16 });
        }
    }

    void DrawPause(float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.6f));
        Shadowed(new Rect(0, 180, W, 80), "PAUSED", title);
        float bw = 300f, x = (W - bw) / 2f;
        if (GUI.Button(new Rect(x, 320, bw, 60), "Resume", button)) SetState(GameState.Playing);
        if (GUI.Button(new Rect(x, 395, bw, 60), "Give up (keep gold)", button)) QuitToMenu();
    }

    void DrawEnd(float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.7f));
        bool won = State == GameState.Victory;
        var t = new GUIStyle(title);
        t.normal.textColor = won ? new Color(1f, 0.85f, 0.35f) : new Color(0.95f, 0.3f, 0.3f);
        Shadowed(new Rect(0, 130, W, 90), won ? "YOU SURVIVED!" : "YOU DIED", t);

        string stats = $"Time  {FormatTime(RunTime)}\nLevel  {Level}\nKills  {Kills}\n<color=#FFD840>Gold earned  {RunGold}</color>";
        if (won) stats += "  (includes +100 survival bonus)";
        GUI.Label(new Rect(0, 240, W, 140), stats, new GUIStyle(center) { fontSize = 24 });

        float bw = 300f, x = (W - bw) / 2f;
        if (GUI.Button(new Rect(x, 410, bw, 60), "Play again  [R]", button)) StartRun();
        if (GUI.Button(new Rect(x, 485, bw, 60), "Main menu / Shop", button)) QuitToMenu();
    }
}
