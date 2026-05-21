// Editor-only script — lives in an Editor/ folder so it is stripped from builds.
// Menu: Pong Legends → 1. Create Assets  →  2. Setup CharacterSelect Scene  →  3. Setup Game Scene
// Run them in order the first time you open the project.

using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PongLegends.Editor
{
    public static class PongLegendsSetup
    {
        private const string CharactersPath = "Assets/ScriptableObjects/Characters";
        private const string SOPath         = "Assets/ScriptableObjects";

        // ─── Step 1: Assets ────────────────────────────────────────────────────

        [MenuItem("Pong Legends/1. Create All Assets")]
        public static void CreateAllAssets()
        {
            EnsureFolder("Assets/ScriptableObjects");
            EnsureFolder(CharactersPath);

            CreateCharacter("JohnnyPong",   "Johnny Pong",   HexColor("D2691E"), HexColor("FFD700"), AbilityType.CoolWave,      VisualFeature.Sunglasses,  1.0f);
            CreateCharacter("RyuPong",      "Ryu Pong",      HexColor("FFFFFF"), HexColor("FF0000"), AbilityType.Uppercut,      VisualFeature.Headband,    1.0f);
            CreateCharacter("ElectraPong",  "Electra Pong",  HexColor("FFFF00"), HexColor("00FFFF"), AbilityType.LightningBolt, VisualFeature.Electric,    1.0f);
            CreateCharacter("TankPong",     "Tank Pong",     HexColor("808080"), HexColor("556B2F"), AbilityType.IronShell,     VisualFeature.Armor,       1.2f);
            CreateCharacter("ShadowPong",   "Shadow Pong",   HexColor("4B0082"), HexColor("8B00FF"), AbilityType.ShadowClone,   VisualFeature.NinjaMask,   1.0f);
            CreateCharacter("PixelPong",    "Pixel Pong",    HexColor("00FF00"), HexColor("FFFFFF"), AbilityType.GlitchBomb,    VisualFeature.PixelBlocks, 1.0f);
            CreateCharacter("InfernoPong",  "Inferno Pong",  HexColor("FF4500"), HexColor("FFD700"), AbilityType.Fireball,      VisualFeature.Flames,      1.0f);
            CreateCharacter("FrostPong",    "Frost Pong",    HexColor("87CEEB"), HexColor("FFFFFF"), AbilityType.IceShot,       VisualFeature.IceCrystals, 1.0f);

            // Session data
            string sdPath = $"{SOPath}/SessionData.asset";
            if (!File.Exists(sdPath))
            {
                var sd = ScriptableObject.CreateInstance<SessionData>();
                AssetDatabase.CreateAsset(sd, sdPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Pong Legends: All assets created in " + CharactersPath);
        }

        private static void CreateCharacter(string fileName, string charName,
                                             Color paddle, Color accent,
                                             AbilityType ability, VisualFeature feature,
                                             float heightMult)
        {
            string path = $"{CharactersPath}/{fileName}.asset";
            if (File.Exists(path)) return;

            var def = ScriptableObject.CreateInstance<CharacterDefinition>();
            def.characterName         = charName;
            def.paddleColor           = paddle;
            def.accentColor           = accent;
            def.abilityType           = ability;
            def.visualFeature         = feature;
            def.paddleHeightMultiplier = heightMult;
            AssetDatabase.CreateAsset(def, path);
        }

        // ─── Step 2: CharacterSelect Scene ─────────────────────────────────────

        [MenuItem("Pong Legends/2. Setup CharacterSelect Scene")]
        public static void SetupCharacterSelectScene()
        {
            EnsureFolder("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGO = new GameObject("Main Camera");
            var cam   = camGO.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 3.6f;
            cam.backgroundColor  = new Color(0.05f, 0.05f, 0.1f);
            cam.clearFlags       = CameraClearFlags.SolidColor;
            camGO.tag            = "MainCamera";

            // EventSystem (required for pointer events)
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Title
            MakeText(canvasGO.transform, "Title", "PONG LEGENDS",
                     new Vector2(0, 280), new Vector2(800, 80), 72, new Color(1f, 0.84f, 0f), FontStyles.Bold);

            // Subtitle
            MakeText(canvasGO.transform, "Subtitle", "SELECT YOUR FIGHTER",
                     new Vector2(0, 210), new Vector2(600, 40), 28, Color.yellow, FontStyles.Normal);

            // Instructions
            MakeText(canvasGO.transform, "Instructions", "Arrow keys or click to select  •  ENTER to confirm",
                     new Vector2(0, -310), new Vector2(900, 30), 20, new Color(0.7f, 0.7f, 0.7f), FontStyles.Normal);

            // Character grid (4x2) — 8 slots
            var gridGO = new GameObject("CharacterGrid");
            gridGO.transform.SetParent(canvasGO.transform, false);
            var gridRect = gridGO.AddComponent<RectTransform>();
            gridRect.anchoredPosition = new Vector2(0, -20);
            gridRect.sizeDelta        = new Vector2(1060, 450);
            var layout = gridGO.AddComponent<GridLayoutGroup>();
            layout.cellSize        = new Vector2(245, 210);
            layout.spacing         = new Vector2(20, 20);
            layout.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;
            layout.childAlignment  = TextAnchor.MiddleCenter;

            var slots = new CharacterSlot[8];
            for (int i = 0; i < 8; i++)
            {
                var slotGO = new GameObject($"Slot_{i}");
                slotGO.transform.SetParent(gridGO.transform, false);
                var img = slotGO.AddComponent<Image>();
                img.color = new Color(0.17f, 0.17f, 0.27f);
                var slot  = slotGO.AddComponent<CharacterSlot>();
                slots[i]  = slot;

                // Name text child — populated at runtime by CharacterSlot.Initialize
                MakeText(slotGO.transform, "Name", "",
                         new Vector2(0, -80), new Vector2(230, 30), 16, Color.white, FontStyles.Normal);
            }

            // CharacterSelectManager
            var mgrGO  = new GameObject("CharacterSelectManager");
            var mgr    = mgrGO.AddComponent<CharacterSelectManager>();
            var so     = LoadSessionData();
            SetField(mgr, "sessionData", so);
            SetField(mgr, "slots", slots);
            SetField(mgr, "allCharacters", LoadAllCharacters());

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/CharacterSelect.unity");
            Debug.Log("Pong Legends: CharacterSelect scene created at Assets/Scenes/CharacterSelect.unity");
            AddSceneToBuildSettings("Assets/Scenes/CharacterSelect.unity");
        }

        // ─── Step 3: Game Scene ─────────────────────────────────────────────────

        [MenuItem("Pong Legends/3. Setup Game Scene")]
        public static void SetupGameScene()
        {
            EnsureFolder("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // EventSystem (required for UI pointer events)
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();

            // Camera
            var camGO = new GameObject("Main Camera");
            var cam   = camGO.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 3.6f;
            cam.backgroundColor  = new Color(0.1f, 0.1f, 0.18f);
            cam.clearFlags       = CameraClearFlags.SolidColor;
            camGO.tag            = "MainCamera";
            camGO.transform.position = new Vector3(0f, 0f, -10f);

            // Center dashed line
            var lineParent = new GameObject("CenterLine");
            lineParent.AddComponent<CenterLine>();
            for (int i = 0; i < 15; i++)
            {
                float y = Mathf.Lerp(-3.3f, 3.3f, i / 14f);
                var seg = new GameObject($"Seg{i}");
                seg.transform.SetParent(lineParent.transform, false);
                seg.transform.position   = new Vector3(0f, y, 0f);
                seg.transform.localScale = new Vector3(0.04f, 0.28f, 1f);
                var sr = seg.AddComponent<SpriteRenderer>();
                sr.sprite       = null; // assigned at runtime via SpriteFactory
                sr.color        = new Color(0.27f, 0.27f, 0.4f);
                sr.sortingOrder = 0;
            }

            // Ball
            var ballGO = new GameObject("Ball");
            ballGO.AddComponent<SpriteRenderer>();
            var ball = ballGO.AddComponent<Ball>();

            // Paddles
            var playerGO = new GameObject("PlayerPaddle");
            playerGO.AddComponent<SpriteRenderer>();
            var playerPaddle = playerGO.AddComponent<Paddle>();

            var aiGO = new GameObject("AIPaddle");
            aiGO.AddComponent<SpriteRenderer>();
            var aiPaddle = aiGO.AddComponent<Paddle>();

            // --- Canvas ---
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Header row
            var playerNameTxt = MakeText(canvasGO.transform, "PlayerName", "PLAYER",
                new Vector2(-480, 320), new Vector2(300, 40), 22, Color.white, FontStyles.Bold);
            MakeText(canvasGO.transform, "VS", "VS",
                new Vector2(0, 320), new Vector2(80, 40), 22, new Color(1f, 0.84f, 0f), FontStyles.Bold);
            var aiNameTxt = MakeText(canvasGO.transform, "AIName", "AI",
                new Vector2(480, 320), new Vector2(300, 40), 22, Color.white, FontStyles.Bold);

            // Scores
            var playerScoreTxt = MakeText(canvasGO.transform, "PlayerScore", "0",
                new Vector2(-250, 260), new Vector2(120, 70), 56, Color.white, FontStyles.Bold);
            var aiScoreTxt = MakeText(canvasGO.transform, "AIScore", "0",
                new Vector2(250, 260), new Vector2(120, 70), 56, Color.white, FontStyles.Bold);

            // Score banner — shown briefly after each point, hidden initially
            var bannerTxt = MakeText(canvasGO.transform, "ScoreBanner", "",
                new Vector2(0, 0), new Vector2(900, 80), 52,
                new Color(1f, 0.84f, 0f), FontStyles.Bold);
            bannerTxt.gameObject.SetActive(false);

            // Instructions
            MakeText(canvasGO.transform, "Instructions",
                "W/S or ↑↓ to move  •  SPACE for special  •  ESC to quit",
                new Vector2(0, -330), new Vector2(900, 26), 18,
                new Color(0.6f, 0.6f, 0.6f), FontStyles.Normal);

            // Winner overlay
            var overlayGO = new GameObject("WinnerOverlay");
            overlayGO.transform.SetParent(canvasGO.transform, false);
            var overlayRect = overlayGO.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayGO.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.75f);
            var overlay = overlayGO.AddComponent<WinnerOverlay>();

            var resultTxt = MakeText(overlayGO.transform, "ResultText", "VICTORY",
                new Vector2(0, 60), new Vector2(600, 100), 72, new Color(1f, 0.84f, 0f), FontStyles.Bold);
            var nameTxt = MakeText(overlayGO.transform, "WinnerName", "",
                new Vector2(0, -30), new Vector2(600, 60), 42, Color.white, FontStyles.Normal);
            var promptTxt = MakeText(overlayGO.transform, "Prompt", "Press ENTER to return",
                new Vector2(0, -120), new Vector2(600, 36), 26, Color.white, FontStyles.Normal);

            SetField(overlay, "panel",      overlayGO as GameObject);
            SetField(overlay, "resultText", resultTxt);
            SetField(overlay, "nameText",   nameTxt);
            SetField(overlay, "promptText", promptTxt);
            overlayGO.SetActive(false);

            // Score Manager
            var scoreMgrGO = new GameObject("ScoreManager");
            var scoreMgr   = scoreMgrGO.AddComponent<ScoreManager>();
            SetField(scoreMgr, "playerScoreText", playerScoreTxt);
            SetField(scoreMgr, "aiScoreText",     aiScoreTxt);
            SetField(scoreMgr, "playerNameText",  playerNameTxt);
            SetField(scoreMgr, "aiNameText",      aiNameTxt);
            SetField(scoreMgr, "winnerOverlay",   overlay);

            // Ability System
            var abilitySysGO = new GameObject("AbilitySystem");
            var abilitySys   = abilitySysGO.AddComponent<AbilitySystem>();

            // Gameplay Manager
            var gmGO = new GameObject("GameplayManager");
            var gm   = gmGO.AddComponent<GameplayManager>();
            var so   = LoadSessionData();
            SetField(gm, "sessionData",     so);
            SetField(gm, "ball",            ball);
            SetField(gm, "playerPaddle",    playerPaddle);
            SetField(gm, "aiPaddle",        aiPaddle);
            SetField(gm, "scoreManager",    scoreMgr);
            SetField(gm, "abilitySystem",   abilitySys);
            SetField(gm, "scoreBannerText", bannerTxt);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
            Debug.Log("Pong Legends: Game scene created at Assets/Scenes/Game.unity");
            AddSceneToBuildSettings("Assets/Scenes/Game.unity");
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        private static TextMeshProUGUI MakeText(Transform parent, string goName, string text,
                                                 Vector2 pos, Vector2 size, float fontSize,
                                                 Color color, FontStyles style)
        {
            var go   = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta        = size;
            var tmp  = go.AddComponent<TextMeshProUGUI>();
            tmp.text       = text;
            tmp.fontSize   = fontSize;
            tmp.color      = color;
            tmp.fontStyle  = style;
            tmp.alignment  = TextAlignmentOptions.Center;
            return tmp;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }

        private static SessionData LoadSessionData()
        {
            return AssetDatabase.LoadAssetAtPath<SessionData>($"{SOPath}/SessionData.asset");
        }

        private static CharacterDefinition[] LoadAllCharacters()
        {
            string[] names = { "JohnnyPong", "RyuPong", "ElectraPong", "TankPong",
                               "ShadowPong", "PixelPong", "InfernoPong", "FrostPong" };
            var defs = new CharacterDefinition[names.Length];
            for (int i = 0; i < names.Length; i++)
                defs[i] = AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"{CharactersPath}/{names[i]}.asset");
            return defs;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                if (s.path == scenePath) return;

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
