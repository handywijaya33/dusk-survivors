using System;
using UnityEngine;

/// <summary>
/// Smoke test: run a build with "-autoplay" to have a bot play at 4x speed for 90 seconds of real time,
/// always taking the first upgrade, then log a summary and quit.
/// </summary>
public class AutoPlay : MonoBehaviour
{
    float realTime;
    int exceptions;
    bool shotTaken;

    public static void TryStart()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-autoplay") >= 0)
            new GameObject("AutoPlay").AddComponent<AutoPlay>();
    }

    void Start()
    {
        Application.runInBackground = true;
        Application.logMessageReceived += (msg, stack, type) =>
        {
            if (type == LogType.Exception || type == LogType.Error) exceptions++;
        };
        GameManager.I.StartRun();
        var player = GameManager.I.Player;
        for (int i = 0; i < WeaponCatalog.Count; i++)
            if (player.FindWeapon(WeaponCatalog.Prototype(i).GetType()) == null) player.AddWeapon(WeaponCatalog.Create(i));
        player.MaxHp = player.Hp = 1000f; // survive long enough to exercise late-game spawns
    }

    void Update()
    {
        var gm = GameManager.I;
        realTime += Time.unscaledDeltaTime;
        GameInput.Override = new Vector2(Mathf.Cos(realTime * 0.7f), Mathf.Sin(realTime * 0.5f));

        if (gm.State == GameState.Playing) Time.timeScale = 4f;
        if (!shotTaken && realTime > 25f)
        {
            shotTaken = true;
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.persistentDataPath, "autoplay.png"));
        }
        if (gm.State == GameState.LevelUp) gm.Choose(0);
        if (gm.State == GameState.GameOver || gm.State == GameState.Victory || realTime > 90f)
        {
            Debug.Log($"AUTOPLAY RESULT state={gm.State} time={gm.RunTime:F0}s level={gm.Level} kills={gm.Kills} " +
                      $"enemies={Enemy.All.Count} weapons={(gm.Player != null ? gm.Player.Weapons.Count : 0)} exceptions={exceptions}");
            Application.Quit(exceptions == 0 ? 0 : 2);
            enabled = false;
        }
    }
}
