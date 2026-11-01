using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace NightRun
{
    /// <summary>Opt-in development-build acceptance runner. Never enabled during normal play.</summary>
    public sealed class RunAutomation : MonoBehaviour
    {
        GameManager game; string folder;
        readonly List<string> passed = new List<string>(), failed = new List<string>(), errors = new List<string>();
        public void Initialize(GameManager owner)
        {
            game = owner; var args = Environment.GetCommandLineArgs();
            // Exercise normal action bindings with isolated input devices even
            // when another desktop window has focus during an unattended run.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var device in InputSystem.devices)
                if (device is Keyboard || device is Mouse) InputSystem.DisableDevice(device);
            InputSystem.AddDevice<Keyboard>("Acceptance keyboard").MakeCurrent();
            InputSystem.AddDevice<Mouse>("Acceptance mouse").MakeCurrent();
            int index = Array.IndexOf(args, "-capture-dir"); folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.persistentDataPath, "QA");
            Directory.CreateDirectory(folder); Application.logMessageReceived += Log;
            StartCoroutine(Run());
        }
        void Log(string text, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(text); }
        void Check(bool condition, string name) { (condition ? passed : failed).Add(name); Debug.Log("QA " + (condition ? "PASS " : "FAIL ") + name); }
        IEnumerator Await(Func<bool> ready, string name)
        {
            float deadline = Time.realtimeSinceStartup + 30;
            while (!ready() && Time.realtimeSinceStartup < deadline) yield return null;
            if (!ready()) Check(false, name + " timed out");
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            yield return new WaitForSecondsRealtime(.25f);
        }
        IEnumerator Key(Key key)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            yield return null;
        }
        IEnumerator MouseClick(string buttonName)
        {
            UnityEngine.UI.Button target = null;
            foreach (var button in game.UI.GetComponentsInChildren<UnityEngine.UI.Button>())
                if (button.gameObject.name == buttonName) { target = button; break; }
            if (!target) { Check(false, "Mouse target exists: " + buttonName); yield break; }
            var rect = (RectTransform)target.transform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); yield return null; yield return null;
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = point };
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
            Debug.Log("QA MOUSE " + buttonName + " at " + point + " hit " + (hits.Count > 0 ? hits[0].gameObject.name : "nothing") + " click enabled " + game.Controls.FindAction("UI/Click").enabled);
            var pressed = new MouseState { position = point }; pressed = pressed.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(Mouse.current, pressed); yield return null; yield return null;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); yield return null; yield return null;
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(2);
            Screen.SetResolution(1280, 720, FullScreenMode.FullScreenWindow);
            yield return new WaitForSecondsRealtime(1);
            Check(Screen.width == 1280 && Screen.height == 720, "Exact 1280x720 HUD viewport");
            yield return MouseClick("SETTINGS");
            Check(game.UI.SubmenuOpen, "New Input System mouse opens settings");
            yield return MouseClick("BACK");
            Check(!game.UI.SubmenuOpen, "New Input System mouse returns from settings");
            yield return Capture("01-menu-720p");
            yield return Key(UnityEngine.InputSystem.Key.Enter);
            Check(game.State == GameState.Playing && game.CountingDown, "UI keyboard submit starts countdown");
            yield return Await(() => !game.CountingDown, "Initial countdown");
            yield return Key(UnityEngine.InputSystem.Key.D);
            yield return new WaitForSecondsRealtime(.25f);
            Check(game.Car.TargetLane == 2 && game.Car.transform.position.x > 3.2f, "New Input System D changes one lane");
            yield return Key(UnityEngine.InputSystem.Key.LeftArrow);
            yield return new WaitForSecondsRealtime(.25f);
            Check(game.Car.TargetLane == 1, "New Input System arrow returns to middle lane");
            Check(game.World.BatCount == 7, "Seven articulated 3D bats initialized");
            Check(game.Chase.Camera.ViewportPointToRay(new Vector3(.5f, .75f, 0)).direction.y > .1f, "Chase camera exposes the upper sky");
            yield return new WaitForSecondsRealtime(5.2f);
            yield return Capture("02-driving-720p");
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(UnityEngine.InputSystem.Key.LeftShift));
            yield return new WaitForSecondsRealtime(1);
            Check(game.Run.Boosting && game.Run.Nitro < 90, "New Input System Shift consumes nitro");
            Check(game.Car.JetsVisible, "Both compact exhaust flames visible during nitro");
            yield return Capture("03-nitro-720p");
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            yield return Key(UnityEngine.InputSystem.Key.Escape);
            Check(game.State == GameState.Paused, "Escape opens pause screen");
            float f = game.Run.Fuel, n = game.Run.Nitro, t = game.Run.Elapsed;
            float z = game.World.Active.Count > 0 ? game.World.Active[0].transform.position.z : 0;
            yield return new WaitForSecondsRealtime(1);
            Check(Mathf.Approximately(f, game.Run.Fuel) && Mathf.Approximately(n, game.Run.Nitro) && Mathf.Approximately(t, game.Run.Elapsed), "Pause freezes fuel, nitro and difficulty clock");
            Check(game.World.Active.Count == 0 || Mathf.Approximately(z, game.World.Active[0].transform.position.z), "Pause freezes pooled traffic");
            yield return Capture("04-pause-720p");
            yield return Key(UnityEngine.InputSystem.Key.Escape);
            Check(game.State == GameState.Playing, "Escape resumes gameplay");
            yield return MouseClick("II");
            Check(game.State == GameState.Paused, "New Input System mouse pauses through HUD");
            yield return MouseClick("RESUME");
            Check(game.State == GameState.Playing, "New Input System mouse resumes gameplay");
            game.GoToMenu(); yield return null;
            game.UI.OpenSettingsForCapture(); yield return Capture("05-settings-720p");
            game.UI.CloseSubmenu(); game.UI.OpenHowForCapture(); yield return Capture("06-controls-720p");
            game.UI.CloseSubmenu();
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow); yield return new WaitForSecondsRealtime(1);
            Check(Screen.width == 1920 && Screen.height == 1080, "Exact 1920x1080 HUD viewport");
            yield return Capture("07-menu-1080p");
            game.StartRun(); yield return Await(() => !game.CountingDown, "Measurement countdown");
            var frameTimes = new List<float>();
            for (int i = 0; i < 180; i++) { frameTimes.Add(Time.unscaledDeltaTime * 1000); yield return null; }
            frameTimes.Sort(); float p95 = frameTimes[Mathf.FloorToInt(frameTimes.Count * .95f)];
            yield return Capture("08-driving-1080p");
            // Simulate thirty minutes with the actual world, collision, pool, and run systems.
            int objectsBefore = FindObjectsOfType<Transform>(true).Length, restarts = 0, peakActive = 0;
            int seed = 300;
            for (int tick = 0; tick < 108000; tick++)
            {
                if (game.Run.Ended) { game.Run.Reset(); game.Car.ResetCar(); game.World.ResetWorld(seed++); restarts++; }
                if (tick % 12 == 0) AutoLane();
                game.Run.SetBoost(tick % 900 < 180);
                game.Run.Tick(1f / 60); game.Car.Tick(1f / 60); game.World.Tick(1f / 60);
                peakActive = Mathf.Max(peakActive, game.World.PooledActive);
            }
            int objectsAfter = FindObjectsOfType<Transform>(true).Length;
            Check(objectsAfter == objectsBefore, "30-minute simulation creates no additional scene objects");
            Check(peakActive <= 156, "30-minute simulation stays within fixed traffic/item pools");
            game.StartRun(); yield return Await(() => !game.CountingDown, "Collision countdown");
            for (int i = 0; i < 3; i++)
            {
                yield return Await(() => !game.Run.Invulnerable, "Collision cooldown");
                game.Damage(Vector3.zero); yield return null;
            }
            yield return Await(() => game.State == GameState.GameOver, "Collision game over");
            Check(game.State == GameState.GameOver && game.Run.Reason == EndReason.Wrecked, "Third collision ends run with WRECKED");
            yield return Capture("09-wrecked-1080p");
            game.StartRun();
            Check(game.Run.Lives == 3 && game.Run.Fuel == 100 && game.Run.Nitro == 100 && game.Run.Coins == 0, "Restart resets run resources");
            float oldDrain = game.Assets.config.fuelDrain; game.Assets.config.fuelDrain = 200;
            yield return Await(() => game.State == GameState.GameOver, "Fuel game over");
            game.Assets.config.fuelDrain = oldDrain;
            Check(game.State == GameState.GameOver && game.Run.Reason == EndReason.OutOfFuel, "Empty tank ends run with OUT OF FUEL");
            yield return Capture("10-out-of-fuel-1080p");
            game.SaveBest(); Check(PlayerPrefs.GetInt("nightrun.best.v1") == game.Best, "High score persisted through PlayerPrefs");
            Check(errors.Count == 0, "No runtime exceptions or errors");
            string json = "{\n  \"passed\": " + JsonList(passed) + ",\n  \"failed\": " + JsonList(failed) + ",\n  \"errors\": " + JsonList(errors) + ",\n  \"frameTimeP95Ms\": " + p95.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ",\n  \"simulatedSeconds\": 1800,\n  \"simulatedRestarts\": " + restarts + ",\n  \"peakActiveInteractables\": " + peakActive + ",\n  \"objectsBefore\": " + objectsBefore + ",\n  \"objectsAfter\": " + objectsAfter + "\n}";
            File.WriteAllText(Path.Combine(folder, "acceptance.json"), json);
            Debug.Log("NIGHTRUN QA COMPLETE " + folder);
            Application.Quit(failed.Count == 0 ? 0 : 1);
        }
        static string JsonList(List<string> values) => "[" + string.Join(",", values.ConvertAll(x => "\"" + x.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"")) + "]";
        void AutoLane()
        {
            float best = float.NegativeInfinity; int choice = game.Car.TargetLane;
            for (int lane = 0; lane < 3; lane++)
            {
                float score = -Mathf.Abs(lane - game.Car.TargetLane) * 2;
                foreach (var item in game.World.Active)
                {
                    if (item.Lane != lane || item.Consumed || item.transform.position.z < -5) continue;
                    float z = item.transform.position.z;
                    if (item.IsObstacle && z < 95) score -= 1000 / Mathf.Max(1, z);
                    else if (item is FuelPickup && z < 110 && game.Run.Fuel < 75) score += 120 / Mathf.Max(5, z);
                }
                if (score > best) { best = score; choice = lane; }
            }
            game.Car.ChangeLane(Math.Sign(choice - game.Car.TargetLane));
        }
        void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
