using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;

namespace NightRun
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameAssets Assets { get; private set; }
        public RunModel Run { get; private set; }
        public GameState State { get; private set; } = GameState.Menu;
        public RoadWorld World { get; private set; }
        public PlayerCar Car { get; private set; }
        public NightRunUI UI { get; private set; }
        public ChaseCamera Chase { get; private set; }
        public NightRunAudio Audio { get; private set; }
        public int Best { get; private set; }
        public int BestAtStart { get; private set; }
        public bool CountingDown => countdown > 0;
        public bool IsRunning => State == GameState.Playing && !CountingDown;
        public event Action<GameState> StateChanged;
        public InputActionAsset Controls { get; private set; }
        float countdown;
        int lastCount, lastTransitionFrame = -1;
        MotionBlur blur;
        const string BestKey = "nightrun.best.v1";

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        void Start()
        {
            var preview = GameObject.Find("Editor Preview"); if (preview) preview.SetActive(false);
            Assets = Resources.Load<GameAssets>("NightRunAssets");
            if (!Assets) { Debug.LogError("NIGHTRUN assets missing. Run NIGHTRUN > Rebuild Project in the Editor."); enabled = false; return; }
            Assets = Instantiate(Assets);
            var localPack = Resources.Load<LocalAssetPack>("LocalAsphaltCatalog");
            if (localPack) localPack.Apply(Assets);
            Application.targetFrameRate = 60;
            Run = new RunModel(Assets.config);
            Best = PlayerPrefs.GetInt(BestKey, 0);
            Controls = Instantiate(Assets.controls);
            World = new GameObject("Pooled endless highway").AddComponent<RoadWorld>(); World.Initialize(this);
            Car = new GameObject("Player").AddComponent<PlayerCar>(); Car.Initialize(this);
            Chase = new GameObject("Main Camera").AddComponent<ChaseCamera>(); Chase.Initialize(this);
            Audio = gameObject.AddComponent<NightRunAudio>(); Audio.Initialize(this);
            UI = new GameObject("Interface").AddComponent<NightRunUI>(); UI.Initialize(this);
            InitializePostProcessing();
            BindInput();
            ApplySettings();
            if (PlayerPrefs.HasKey("nightrun.fullscreen") && Array.IndexOf(Environment.GetCommandLineArgs(), "-screen-fullscreen") < 0)
                Screen.fullScreen = PlayerPrefs.GetInt("nightrun.fullscreen") == 1;
            GoToMenu();
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-nightrun-smoke") >= 0)
                gameObject.AddComponent<RunAutomation>().Initialize(this);
        }
        void BindInput()
        {
            Controls.FindAction("Gameplay/Left").performed += _ => { if (IsRunning) Car.ChangeLane(-1); };
            Controls.FindAction("Gameplay/Right").performed += _ => { if (IsRunning) Car.ChangeLane(1); };
            Controls.FindAction("Gameplay/Boost").performed += _ => { if (IsRunning) Run.SetBoost(true); };
            Controls.FindAction("Gameplay/Boost").canceled += _ => Run.SetBoost(false);
            Controls.FindAction("Gameplay/Pause").performed += _ => Pause();
            Controls.FindAction("UI/Cancel").performed += _ =>
            {
                if (lastTransitionFrame == Time.frameCount) return;
                if (UI.CloseSubmenu()) return;
                if (State == GameState.Paused) Resume();
            };
        }
        void Update()
        {
            if (Run == null) return;
            float dt = Time.deltaTime;
            if (State == GameState.Playing)
            {
                if (countdown > 0)
                {
                    countdown -= dt;
                    int number = Mathf.CeilToInt(countdown);
                    if (number != lastCount) { lastCount = number; Audio.Count(); }
                    UI.ShowCountdown(countdown > 0 ? Mathf.CeilToInt(countdown).ToString() : "GO");
                    if (countdown <= 0) StartCoroutine(HideGo());
                }
                else
                {
                    Run.Tick(dt);
                    Car.Tick(dt);
                    World.Tick(dt);
                    if (Run.Score > Best) Best = Run.Score;
                    if (Run.Ended) EndRun();
                }
            }
            if (State == GameState.Menu || IsRunning) World.TickDecor(dt);
            Car.TickVisual(dt);
            Audio.Tick();
            UI.Refresh();
        }
        IEnumerator HideGo() { yield return new WaitForSeconds(.65f); if (!CountingDown) UI.ShowCountdown(""); }
        public void StartRun()
        {
            StopAllCoroutines();
            BestAtStart = Best;
            Run.Reset(); Car.ResetCar(); World.ResetWorld();
            countdown = 3; lastCount = 3;
            SetState(GameState.Playing); UI.ShowCountdown("3"); Audio.Count();
        }
        public void Pause()
        {
            if (State != GameState.Playing || lastTransitionFrame == Time.frameCount) return;
            Run.SetBoost(false); SetState(GameState.Paused);
        }
        public void Resume()
        {
            if (State != GameState.Paused || lastTransitionFrame == Time.frameCount) return;
            SetState(GameState.Playing);
        }
        public void GoToMenu()
        {
            SaveBest(); StopAllCoroutines(); countdown = 0;
            Run.Reset(); Car.ResetCar(); World.ResetWorld();
            SetState(GameState.Menu); UI.ShowCountdown("");
        }
        void SetState(GameState state)
        {
            State = state; lastTransitionFrame = Time.frameCount;
            Time.timeScale = state == GameState.Paused || state == GameState.GameOver ? 0 : 1;
            Controls.FindActionMap("Gameplay").Disable(); Controls.FindActionMap("UI").Disable();
            Controls.FindActionMap("UI").Enable();
            if (state == GameState.Playing)
            {
                Controls.FindActionMap("Gameplay").Enable();
                Controls.FindAction("UI/Navigate").Disable(); Controls.FindAction("UI/Submit").Disable(); Controls.FindAction("UI/Cancel").Disable();
                EventSystemSelectionClear();
            }
            Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
            StateChanged?.Invoke(state);
        }
        static void EventSystemSelectionClear()
        {
            if (UnityEngine.EventSystems.EventSystem.current)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
        void EndRun() { SaveBest(); SetState(GameState.GameOver); }
        public void Damage(Vector3 position)
        {
            if (!IsRunning || !Run.Hit()) return;
            World.Effects.Burst(new Vector3(Car.transform.position.x, .65f, 1.8f), new Color(1, .45f, .08f), 1);
            Chase.Shake(); Audio.Impact(); UI.FlashDamage();
        }
        public void Collect(bool fuel, Vector3 position)
        {
            if (fuel) Run.CollectFuel(); else Run.CollectCoin();
            World.Effects.Burst(position + Vector3.up, fuel ? NightRunUI.Mint : NightRunUI.Amber, 0);
            Audio.Pickup(fuel);
        }
        public void SaveBest()
        {
            if (Run != null) Best = Mathf.Max(Best, Run.Score);
            PlayerPrefs.SetInt(BestKey, Best); PlayerPrefs.Save();
        }
        public void Quit() { SaveBest(); Application.Quit(); }
        void OnApplicationQuit() { SaveBest(); }
        void OnApplicationFocus(bool focused) { if (!focused && State == GameState.Playing && Run != null && Array.IndexOf(Environment.GetCommandLineArgs(), "-nightrun-smoke") < 0) Pause(); }
        void OnDestroy()
        {
            if (Controls) { Controls.Disable(); Destroy(Controls); }
            Time.timeScale = 1;
            if (Instance == this) Instance = null;
        }
        void InitializePostProcessing()
        {
            var layer = Chase.gameObject.AddComponent<PostProcessLayer>();
            layer.Init(Assets.postProcess);
            layer.volumeLayer = 1 << 8; layer.volumeTrigger = Chase.transform;
            layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
            var go = new GameObject("Night grading · Bloom"); go.layer = 8;
            var volume = go.AddComponent<PostProcessVolume>(); volume.isGlobal = true;
            volume.sharedProfile = ScriptableObject.CreateInstance<PostProcessProfile>();
            var bloom = volume.sharedProfile.AddSettings<Bloom>(); bloom.enabled.Override(true);
            bloom.intensity.Override(.45f); bloom.threshold.Override(1.2f); bloom.softKnee.Override(.7f);
            var grading = volume.sharedProfile.AddSettings<ColorGrading>(); grading.enabled.Override(true);
            grading.tonemapper.Override(Tonemapper.ACES); grading.postExposure.Override(.65f);
            grading.contrast.Override(8); grading.saturation.Override(-4);
            var vignette = volume.sharedProfile.AddSettings<Vignette>(); vignette.enabled.Override(true); vignette.intensity.Override(.24f);
            blur = volume.sharedProfile.AddSettings<MotionBlur>(); blur.shutterAngle.Override(70); blur.sampleCount.Override(6);
        }
        public void ApplySettings()
        {
            QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("nightrun.quality", 2), true);
            QualitySettings.vSyncCount = 0;
            QualitySettings.pixelLightCount = PlayerPrefs.GetInt("nightrun.quality", 2) == 0 ? 2 : 6;
            if (blur) blur.enabled.Override(PlayerPrefs.GetInt("nightrun.blur", 0) == 1);
            if (Audio) Audio.ApplyVolumes();
        }
    }
}
