using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NightRun
{
    public sealed class NightRunUI : MonoBehaviour
    {
        public static readonly Color Amber = new Color(.9f, .65f, .27f);
        public static readonly Color Mint = new Color(.36f, .7f, .47f);
        public static readonly Color NitroColor = new Color(.94f, .68f, .29f);
        static readonly Color White = new Color(.92f, .93f, .92f);
        static readonly Color Muted = new Color(.57f, .61f, .64f);
        static readonly Color Panel = new Color(.025f, .03f, .035f, .95f);
        GameManager game; RectTransform canvas;
        GameObject menu, hud, pause, over, how, settings, boostLines;
        TMP_Text score, best, coins, speed, fuelPercent, fuelWarning, countdown, resultScore, resultBest, resultDetail, endReason, newBest;
        Image fuelFill, nitroFill, damage;
        readonly Image[] hearts = new Image[3];
        SpeedometerGraphic gauge;
        TMP_Text qualityLabel, fullLabel, blurLabel;
        float flash;
        public bool SubmenuOpen => how.activeSelf || settings.activeSelf;

        public void Initialize(GameManager owner)
        {
            game = owner;
            var c = gameObject.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>(); canvas = GetComponent<RectTransform>();
            var es = new GameObject("New Input System EventSystem").AddComponent<EventSystem>();
            var module = es.gameObject.AddComponent<InputSystemUIInputModule>();
            module.actionsAsset = game.Controls;
            module.point = Ref("Point"); module.leftClick = Ref("Click"); module.scrollWheel = Ref("ScrollWheel");
            module.move = Ref("Navigate"); module.submit = Ref("Submit"); module.cancel = Ref("Cancel");
            module.deselectOnBackgroundClick = false;
            BuildHud(); BuildMenu(); BuildPause(); BuildOver(); BuildHow(); BuildSettings();
            var countRoot = Rect(canvas, "Countdown", -150, -110, 300, 220, new Vector2(.5f, .5f));
            countdown = Text(countRoot, "", 0, 0, 300, 220, 144, White, TextAlignmentOptions.Center);
            damage = Image(Stretch(canvas, "Damage flash"), new Color(1, .08f, .04f, 0)); damage.raycastTarget = false;
            damage.transform.SetSiblingIndex(1);
            game.StateChanged += OnState;
            how.SetActive(false); settings.SetActive(false);
        }
        UnityEngine.InputSystem.InputActionReference Ref(string name) => UnityEngine.InputSystem.InputActionReference.Create(game.Controls.FindAction("UI/" + name));
        RectTransform Rect(Transform parent, string name, float x, float y, float w, float h, Vector2 anchor = default)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = go.GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = anchor; r.pivot = Vector2.zero; r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); return r;
        }
        RectTransform Stretch(Transform parent, string name)
        {
            var r = Rect(parent, name, 0, 0, 0, 0); r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        Image Image(RectTransform r, Color color)
        {
            var im = r.gameObject.AddComponent<Image>(); im.color = color; im.raycastTarget = false; return im;
        }
        void Line(Transform p, float x, float y, float w, Color color) { Image(Rect(p, "Divider", x, y, w, 1), color); }
        TMP_Text Text(Transform parent, string value, float x, float y, float w, float h, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var r = Rect(parent, value.Length > 20 ? value.Substring(0, 20) : value, x, y, w, h);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.font = game.Assets.font;
            t.text = value; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.enableWordWrapping = false; t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }
        Button Button(Transform parent, string label, float x, float y, float width, Action action, bool primary = false, float height = 64)
        {
            var r = Rect(parent, label, x, y, width, height);
            var image = Image(r, primary ? Amber : new Color(.09f, .12f, .17f, .87f)); image.raycastTarget = true;
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = image;
            var colors = b.colors;
            colors.normalColor = Color.white; colors.highlightedColor = primary ? new Color(1, .96f, .82f) : new Color(1.7f, 1.9f, 2.2f);
            colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.65f, .76f, .8f); colors.fadeDuration = .09f; b.colors = colors;
            Text(r, label, 24, 0, width - 40, height, primary ? 31 : 25, primary ? new Color(.03f, .05f, .035f) : White);
            b.onClick.AddListener(() => { game.Audio.Click(); action(); }); return b;
        }
        Image Bar(Transform parent, string label, float x, float y, float width, Color color)
        {
            Image(Rect(parent, label + " track", x, y, width, 11), new Color(1, 1, 1, .12f));
            var fill = Image(Rect(parent, label + " fill", x, y, width, 11), color);
            fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            // Filled Image requires a sprite; a shared white texture sprite is generated once by Unity UI.
            fill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            return fill;
        }
        void BuildHud()
        {
            hud = Stretch(canvas, "Driving HUD").gameObject;
            boostLines = Stretch(hud.transform, "Nitro speed lines").gameObject; boostLines.AddComponent<BoostLinesGraphic>().raycastTarget = false;
            var left = Rect(hud.transform, "Score cluster", 68, -190, 480, 135, Vector2.up);
            Text(left, "LIVE RUN", 0, 105, 200, 25, 19, Amber);
            score = Text(left, "000000", 0, 41, 420, 77, 72, White);
            best = Text(left, "BEST  000000", 0, 9, 250, 29, 22, Muted);
            coins = Text(left, "$  000", 270, 9, 150, 29, 24, Amber);
            Line(left, 0, 0, 380, new Color(1, 1, 1, .18f));
            var right = Rect(hud.transform, "Integrity cluster", -330, -150, 265, 90, Vector2.one);
            Text(right, "INTEGRITY", 0, 47, 200, 30, 21, Muted);
            for (int i = 0; i < 3; i++) hearts[i] = Image(Rect(right, "Integrity " + i, i * 58, 23, 48, 12), White);
            Button(right, "II", 195, 20, 54, () => game.Pause(), false, 55);
            var fuel = Rect(hud.transform, "Fuel cluster", 68, 60, 390, 125);
            Text(fuel, "FUEL / RESERVE", 0, 80, 275, 30, 22, Muted);
            fuelFill = Bar(fuel, "Fuel", 0, 55, 275, Amber);
            fuelPercent = Text(fuel, "100%", 294, 38, 100, 44, 34, White);
            fuelWarning = Text(fuel, "KEEP IT RUNNING", 0, 14, 390, 26, 18, Muted);
            var g = Rect(hud.transform, "Speedometer", -425, 50, 360, 310, Vector2.right);
            gauge = g.gameObject.AddComponent<SpeedometerGraphic>(); gauge.raycastTarget = false;
            speed = Text(g, "140", 80, 69, 200, 81, 72, White, TextAlignmentOptions.Center);
            Text(g, "KM/H", 100, 48, 160, 26, 19, Muted, TextAlignmentOptions.Center);
            for (int tick = 0; tick <= 6; tick++)
            {
                float angle = (210 - tick * 40) * Mathf.Deg2Rad;
                float x = 180 + Mathf.Cos(angle) * 111, y = 155 + Mathf.Sin(angle) * 111;
                Text(g, (tick * 50).ToString(), x - 22, y - 12, 44, 24, 16, White, TextAlignmentOptions.Center);
            }
            Text(g, "NITRO", 35, 17, 110, 22, 17, NitroColor);
            Text(g, "SHIFT", 247, 17, 80, 22, 17, Muted, TextAlignmentOptions.Right);
            nitroFill = Bar(g, "Nitro", 35, 0, 290, NitroColor);
            var hint = Rect(hud.transform, "Control hint", -300, 23, 600, 28, new Vector2(.5f, 0));
            Text(hint, "A / D   CHANGE LANE       SHIFT   NITRO       ESC   PAUSE", 0, 0, 600, 28, 18, new Color(.6f, .7f, .8f, .7f), TextAlignmentOptions.Center);
        }
        void BuildMenu()
        {
            menu = Stretch(canvas, "Main Menu").gameObject;
            var gradient = Stretch(menu.transform, "Left cinematic shade").gameObject.AddComponent<PanelGraphic>();
            gradient.color = new Color(.008f, .015f, .03f, .97f); gradient.rightColor = new Color(.008f, .015f, .03f, 0); gradient.raycastTarget = false;
            var p = Rect(menu.transform, "Menu left column", 105, 0, 640, 1080);
            Image(Rect(p, "Brand stripe", 0, 951, 6, 29), Amber);
            Text(p, "NR / NIGHT DIVISION", 22, 946, 500, 40, 25, White);
            Text(p, "ENDLESS HIGHWAY  /  VOL. 01", 0, 825, 600, 40, 21, Muted);
            var title = Text(p, "NIGHT\nRUN", -8, 467, 700, 360, 163, White);
            title.enableWordWrapping = true; title.lineSpacing = -30; title.fontStyle = FontStyles.Italic;
            Text(p, "CHASE THE NIGHT.", 0, 449, 600, 48, 31, Amber);
            Button(p, "PLAY", 0, 330, 375, () => game.StartRun(), true, 74);
            Button(p, "HOW TO PLAY", 0, 250, 375, () => ShowSubmenu(how));
            Button(p, "SETTINGS", 0, 175, 375, () => ShowSubmenu(settings));
            Button(p, "QUIT", 0, 100, 375, () => game.Quit());
            Text(p, "BUILT FOR THE NIGHT  /  SINGLE PLAYER", 0, 34, 650, 25, 17, Muted);
            var tag = Rect(menu.transform, "Showroom caption", -505, 76, 420, 100, Vector2.right);
            Text(tag, "CONCEPT / 01", 0, 49, 420, 40, 30, White, TextAlignmentOptions.Right);
            Text(tag, "140 > 300 KM/H    /    NO FINISH LINE", 0, 15, 420, 30, 18, Muted, TextAlignmentOptions.Right);
            Line(tag, 40, 0, 380, new Color(1, 1, 1, .2f));
        }
        RectTransform Modal(out GameObject page, string name, string eyebrow, string title, float width = 720, float height = 700)
        {
            page = Stretch(canvas, name).gameObject; Image(page.GetComponent<RectTransform>(), new Color(.005f, .012f, .025f, .79f));
            var p = Rect(page.transform, name + " panel", -width / 2, -height / 2, width, height, new Vector2(.5f, .5f));
            Image(p, Panel); Image(Rect(p, "Accent", 0, height - 5, width, 5), Amber);
            Text(p, eyebrow, 48, height - 72, width - 96, 30, 20, Amber);
            Text(p, title, 46, height - 160, width - 92, 86, 66, White);
            Line(p, 48, height - 190, width - 96, new Color(1, 1, 1, .15f)); return p;
        }
        void BuildPause()
        {
            var p = Modal(out pause, "Pause Menu", "TAKE A BREATH", "RUN PAUSED", 680, 610);
            Button(p, "RESUME", 48, 282, 584, () => game.Resume(), true);
            Button(p, "RESTART", 48, 199, 584, () => game.StartRun());
            Button(p, "MAIN MENU", 48, 116, 584, () => game.GoToMenu());
            Text(p, "ESC TO RETURN TO THE ROAD", 48, 42, 584, 30, 19, Muted);
        }
        void BuildOver()
        {
            var p = Modal(out over, "Game Over", "THE NIGHT ISN'T OVER", "RUN ENDED", 790, 820);
            endReason = Text(p, "WRECKED", 48, 570, 690, 40, 25, new Color(1, .4f, .28f));
            Text(p, "FINAL SCORE", 48, 509, 400, 30, 21, Muted);
            resultScore = Text(p, "000000", 44, 407, 690, 110, 100, White);
            resultBest = Text(p, "BEST  000000", 48, 361, 690, 40, 30, Amber);
            newBest = Text(p, "NEW BEST", 500, 365, 240, 36, 25, Amber, TextAlignmentOptions.Right);
            Line(p, 48, 337, 694, new Color(1, 1, 1, .15f));
            resultDetail = Text(p, "0.00 KM  /  0 COINS", 48, 281, 694, 38, 27, Muted);
            Button(p, "RETRY", 48, 176, 694, () => game.StartRun(), true);
            Button(p, "MAIN MENU", 48, 93, 694, () => game.GoToMenu());
        }
        void BuildHow()
        {
            var p = Modal(out how, "How to play", "KNOW YOUR MACHINE", "OWN THE HIGHWAY", 1000, 820);
            string[] labels = { "A / D  OR  LEFT / RIGHT", "HOLD SHIFT", "ESC", "GOLD COINS", game.Assets.localPack ? "FUEL BOTTLES" : "GREEN FUEL CANS", "THREE HITS. ONE RUN." };
            string[] details = { "Change lanes. Keep your eyes on traffic.", "Nitro boosts speed by 25%. Fuel burns faster.", "Pause the run. Everything waits for you.", "+100 points each. Distance also adds points.", "Restore 35% fuel. Don't run the tank dry.", "Avoid stopped and moving cars. Beat your best." };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = 554 - i * 68;
                Text(p, labels[i], 48, y, 410, 34, 25, i < 3 ? White : (i == 4 ? Mint : Amber));
                var detail = Text(p, details[i], 460, y, 490, 34, 21, Muted);
                detail.enableAutoSizing = true; detail.fontSizeMin = 16; detail.fontSizeMax = 21;
            }
            Text(p, "Nitro recharges automatically after 1.5s. Empty? Release SHIFT and recharge to 25%.", 48, 116, 900, 30, 20, Muted);
            Button(p, "BACK", 48, 35, 904, () => CloseSubmenu(), true);
        }
        void Slider(Transform parent, string label, float y, string pref, float defaultValue)
        {
            Text(parent, label, 48, y, 320, 48, 26, White);
            var r = Rect(parent, label + " slider", 385, y + 5, 365, 36);
            Image(Rect(r, "Track", 0, 15, 365, 6), new Color(1, 1, 1, .18f));
            var fillArea = Rect(r, "Fill area", 0, 15, 365, 6);
            var fillRect = Stretch(fillArea, "Fill"); var fill = Image(fillRect, Amber);
            var handleArea = Rect(r, "Handle area", 0, 0, 365, 36);
            var handle = Rect(handleArea, "Handle", 0, 7, 13, 22); var handleImage = Image(handle, White); handleImage.raycastTarget = true;
            var hit = Image(r, new Color(0, 0, 0, .001f)); hit.raycastTarget = true;
            var s = r.gameObject.AddComponent<UnityEngine.UI.Slider>(); s.fillRect = fillRect; s.handleRect = handle;
            s.targetGraphic = handleImage; s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.value = PlayerPrefs.GetFloat(pref, defaultValue);
            s.onValueChanged.AddListener(value => { PlayerPrefs.SetFloat(pref, value); game.Audio.ApplyVolumes(); });
        }
        void BuildSettings()
        {
            var p = Modal(out settings, "Settings", "MAKE IT YOURS", "TUNE YOUR RUN", 850, 820);
            Slider(p, "MUSIC VOLUME", 551, "nightrun.music", .6f);
            Slider(p, "EFFECTS VOLUME", 473, "nightrun.sfx", .8f);
            qualityLabel = Button(p, "", 48, 365, 754, () =>
            {
                int q = PlayerPrefs.GetInt("nightrun.quality", 2); q = q >= 4 ? 0 : q + 2;
                PlayerPrefs.SetInt("nightrun.quality", q); game.ApplySettings(); RefreshSettings();
            }).GetComponentInChildren<TMP_Text>();
            fullLabel = Button(p, "", 48, 281, 754, () =>
            {
                bool fullscreen = !Screen.fullScreen; Screen.fullScreen = fullscreen;
                PlayerPrefs.SetInt("nightrun.fullscreen", fullscreen ? 1 : 0); RefreshSettings();
            }).GetComponentInChildren<TMP_Text>();
            blurLabel = Button(p, "", 48, 197, 754, () =>
            {
                PlayerPrefs.SetInt("nightrun.blur", 1 - PlayerPrefs.GetInt("nightrun.blur", 0)); game.ApplySettings(); RefreshSettings();
            }).GetComponentInChildren<TMP_Text>();
            Button(p, "BACK", 48, 66, 754, () => { PlayerPrefs.Save(); CloseSubmenu(); }, true);
            RefreshSettings();
        }
        void RefreshSettings()
        {
            if (!qualityLabel) return;
            qualityLabel.text = "GRAPHICS QUALITY<pos=570>" + (PlayerPrefs.GetInt("nightrun.quality", 2) >= 4 ? "HIGH" : PlayerPrefs.GetInt("nightrun.quality", 2) == 0 ? "LOW" : "MEDIUM");
            fullLabel.text = "FULLSCREEN<pos=650>" + (PlayerPrefs.GetInt("nightrun.fullscreen", Screen.fullScreen ? 1 : 0) == 1 ? "ON" : "OFF");
            blurLabel.text = "MOTION BLUR<pos=650>" + (PlayerPrefs.GetInt("nightrun.blur", 0) == 1 ? "ON" : "OFF");
        }
        void ShowSubmenu(GameObject page)
        {
            menu.SetActive(false); how.SetActive(page == how); settings.SetActive(page == settings); RefreshSettings(); SelectFirst(page);
        }
        public bool CloseSubmenu()
        {
            if (!SubmenuOpen) return false;
            how.SetActive(false); settings.SetActive(false); menu.SetActive(true); SelectFirst(menu); return true;
        }
        void SelectFirst(GameObject page)
        {
            var first = page.GetComponentInChildren<Selectable>();
            EventSystem.current.SetSelectedGameObject(null);
            if (first) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        void OnState(GameState state)
        {
            how.SetActive(false); settings.SetActive(false);
            menu.SetActive(state == GameState.Menu); hud.SetActive(state != GameState.Menu);
            pause.SetActive(state == GameState.Paused); over.SetActive(state == GameState.GameOver);
            countdown.gameObject.SetActive(state == GameState.Playing);
            if (state == GameState.Menu) SelectFirst(menu);
            else if (state == GameState.Paused) SelectFirst(pause);
            else if (state == GameState.GameOver)
            {
                endReason.text = game.Run.Reason == EndReason.Wrecked ? "WRECKED / INTEGRITY LOST" : "OUT OF FUEL / TANK EMPTY";
                resultScore.text = game.Run.Score.ToString("D6"); resultBest.text = "BEST  " + game.Best.ToString("D6");
                resultDetail.text = (game.Run.Distance / 1000).ToString("0.00") + " KM  /  " + game.Run.Coins + " COINS  /  " + Mathf.FloorToInt(game.Run.Elapsed) + " SEC";
                newBest.gameObject.SetActive(game.Run.Score > game.BestAtStart); SelectFirst(over);
            }
        }
        public void ShowCountdown(string value) { countdown.text = value; }
        public void FlashDamage() { flash = .42f; }
        public void Refresh()
        {
            if (game.Run == null) return;
            var run = game.Run;
            score.text = run.Score.ToString("D6"); best.text = "BEST  " + game.Best.ToString("D6"); coins.text = "$  " + run.Coins.ToString("D3");
            speed.text = Mathf.RoundToInt(game.State == GameState.Menu || game.CountingDown ? 0 : run.Speed).ToString(); gauge.Speed = game.CountingDown ? 0 : run.Speed;
            fuelFill.fillAmount = run.Fuel / 100; nitroFill.fillAmount = run.Nitro / 100;
            fuelPercent.text = Mathf.CeilToInt(run.Fuel) + "%";
            Color fuelColor = run.Fuel < 10 ? new Color(1, .23f, .15f) : run.Fuel < 25 ? new Color(1, .72f, .15f) : Amber;
            fuelFill.color = fuelColor; fuelWarning.color = run.Fuel < 25 ? fuelColor : Muted;
            fuelWarning.text = run.Fuel < 10 ? "CRITICAL / FIND FUEL NOW" : run.Fuel < 25 ? "LOW FUEL / FIND A GREEN CAN" : "KEEP IT RUNNING";
            for (int i = 0; i < 3; i++) hearts[i].color = i < run.Lives ? White : new Color(.9f, .2f, .15f, .25f);
            boostLines.SetActive(game.IsRunning && run.Boosting);
            flash = Mathf.Max(0, flash - Time.unscaledDeltaTime);
            damage.color = new Color(1, .08f, .04f, flash * .4f);
        }
        // Automation navigates the same concrete screens used by buttons.
        public void OpenSettingsForCapture() => ShowSubmenu(settings);
        public void OpenHowForCapture() => ShowSubmenu(how);
    }
}
