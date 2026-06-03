// Editor-only script — lives in an Editor/ folder so it is stripped from builds.
// Menu: Pong Legends → 1. Create Assets  →  2. Setup CharacterSelect Scene  →  3. Setup Game Scene  →  4. Configure Sprites
// Run them in order the first time you open the project.

using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
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

            CreateCharacter("JohnnyPong",   "Johnny Pong",   HexColor("D2691E"), HexColor("FFD700"), AbilityType.Paparazzi,      VisualFeature.Sunglasses,  1.0f);
            CreateCharacter("RyuPong",      "Ryu Pong",      HexColor("FFFFFF"), HexColor("FF0000"), AbilityType.Uppercut,      VisualFeature.Headband,    1.0f);
            CreateCharacter("TelePong",     "Tele-Pong",     HexColor("FFFF00"), HexColor("00FFFF"), AbilityType.LightningBolt, VisualFeature.Electric,    1.0f);
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
            if (AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path) != null) return;

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

            // Instructions — text is updated at runtime by GameplayManager with the selected ability name
            var instructionsTxt = MakeText(canvasGO.transform, "Instructions",
                "↑↓ to move  •  A/S/D for kicks  •  SPACE for special  •  ESC to quit",
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

            SetField(overlay, "panel",      overlayGO);
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
            SetField(gm, "scoreBannerText",   bannerTxt);
            SetField(gm, "instructionsText", instructionsTxt);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
            Debug.Log("Pong Legends: Game scene created at Assets/Scenes/Game.unity");
            AddSceneToBuildSettings("Assets/Scenes/Game.unity");
        }

        // ─── Step 4: Sprite imports ────────────────────────────────────────────

        [MenuItem("Pong Legends/4. Configure Sprites")]
        public static void ConfigureSprites()
        {
            ConfigureSprite("Assets/Resources/PaparazziCamera.png");
            AssetDatabase.Refresh();
            Debug.Log("Pong Legends: Sprite import settings applied.");
        }

        private static void ConfigureSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Sprite not found at {path}");
                return;
            }
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode          = FilterMode.Point;
            importer.mipmapEnabled       = false;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
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

            if (field == null)
            {
                Debug.LogError($"SetField: field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }

            field.SetValue(target, value);
        }

        private static SessionData LoadSessionData()
        {
            var sd = AssetDatabase.LoadAssetAtPath<SessionData>($"{SOPath}/SessionData.asset");
            if (sd == null)
                Debug.LogError("Pong Legends: SessionData not found — run step 1 (Create All Assets) first.");
            return sd;
        }

        private static CharacterDefinition[] LoadAllCharacters()
        {
            string[] names = { "JohnnyPong", "RyuPong", "TelePong", "TankPong",
                               "ShadowPong", "PixelPong", "InfernoPong", "FrostPong" };
            var defs = new CharacterDefinition[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                defs[i] = AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"{CharactersPath}/{names[i]}.asset");
                if (defs[i] == null)
                    Debug.LogError($"Pong Legends: CharacterDefinition '{names[i]}' not found — run step 1 (Create All Assets) first.");
            }
            return defs;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            string guid   = AssetDatabase.CreateFolder(parent, folder);

            if (string.IsNullOrEmpty(guid))
                throw new IOException($"Failed to create folder: {path}");
        }

        private static Color HexColor(string hex)
        {
            if (!ColorUtility.TryParseHtmlString("#" + hex, out Color c))
                Debug.LogError($"HexColor: invalid hex value '{hex}'");
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

        private static void AddSceneToBuildSettingsFirst(string scenePath)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == scenePath);
            list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ─── Step 5: Lobby Scene ────────────────────────────────────────────────

        [MenuItem("Pong Legends/5. Setup Lobby Scene")]
        public static void SetupLobbyScene()
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

            // EventSystem
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();

            // NetworkManager (LobbyManager will also create one if missing, but pre-placing it is cleaner)
            new GameObject("NetworkManager").AddComponent<NetworkManager>();

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Global title — always visible behind all panels
            MakeText(canvasGO.transform, "Title", "PONG LEGENDS",
                new Vector2(0, 300), new Vector2(700, 80), 60,
                new Color(1f, 0.84f, 0f), FontStyles.Bold);

            // ── Panel_ModeSelect ─────────────────────────────────────────────
            var pMode = MakePanel(canvasGO.transform, "Panel_ModeSelect", active: true);
            MakeText(pMode.transform, "Subtitle", "SELECT GAME MODE",
                new Vector2(0, 100), new Vector2(500, 40), 26, Color.yellow, FontStyles.Normal);
            var btnVsAI   = MakeButton(pMode.transform,  "Btn_VsAI",   "PLAY VS AI",
                new Vector2(0, 10),  new Vector2(340, 75), new Color(0.1f, 0.55f, 0.1f));
            var btnOnline = MakeButton(pMode.transform,  "Btn_Online", "PLAY ONLINE",
                new Vector2(0, -90), new Vector2(340, 75), new Color(0.0f, 0.45f, 0.65f));

            // ── Panel_HostJoin ───────────────────────────────────────────────
            var pHostJoin = MakePanel(canvasGO.transform, "Panel_HostJoin", active: false);
            MakeText(pHostJoin.transform, "Header", "ONLINE PLAY",
                new Vector2(0, 215), new Vector2(500, 58), 42,
                new Color(1f, 0.84f, 0f), FontStyles.Bold);
            var btnCreate   = MakeButton(pHostJoin.transform, "Btn_Create",   "CREATE GAME",
                new Vector2(0, 135), new Vector2(320, 68), new Color(0.0f, 0.45f, 0.65f));
            MakeText(pHostJoin.transform, "OrLabel", "── OR JOIN WITH CODE ──",
                new Vector2(0, 65), new Vector2(500, 28), 16,
                new Color(0.5f, 0.5f, 0.5f), FontStyles.Normal);
            var fldJoin     = MakeInputField(pHostJoin.transform, "InputField_JoinCode",
                "Enter room code…", new Vector2(0, 5), new Vector2(300, 52));
            var btnJoin     = MakeButton(pHostJoin.transform, "Btn_Join",     "JOIN GAME",
                new Vector2(0, -60), new Vector2(300, 62), new Color(0.0f, 0.45f, 0.65f));
            MakeText(pHostJoin.transform, "SpectateLabel", "── OR SPECTATE ──",
                new Vector2(0, -128), new Vector2(500, 26), 15,
                new Color(0.5f, 0.5f, 0.5f), FontStyles.Normal);
            var fldSpectate = MakeInputField(pHostJoin.transform, "InputField_SpectateCode",
                "Enter room code…", new Vector2(0, -175), new Vector2(300, 50));
            var btnSpectate = MakeButton(pHostJoin.transform, "Btn_Spectate", "SPECTATE",
                new Vector2(0, -238), new Vector2(300, 58), new Color(0.35f, 0.35f, 0.35f));
            var btnBack     = MakeButton(pHostJoin.transform, "Btn_Back",     "← BACK",
                new Vector2(0, -315), new Vector2(180, 48), new Color(0.5f, 0.1f, 0.1f));

            // ── Panel_WaitingRoom ────────────────────────────────────────────
            var pWaiting = MakePanel(canvasGO.transform, "Panel_WaitingRoom", active: false);
            MakeText(pWaiting.transform, "WRHeader", "WAITING ROOM",
                new Vector2(0, 290), new Vector2(600, 50), 34,
                new Color(1f, 0.84f, 0f), FontStyles.Bold);
            var roomCodeTxt = MakeText(pWaiting.transform, "RoomCodeText", "CODE: ——",
                new Vector2(0, 252), new Vector2(500, 34), 22, Color.white, FontStyles.Bold);
            var shareURLTxt = MakeText(pWaiting.transform, "ShareURLText", "",
                new Vector2(0, 222), new Vector2(820, 26), 14,
                new Color(0.6f, 0.8f, 1f), FontStyles.Normal);
            var btnCopyLink = MakeButton(pWaiting.transform, "Btn_CopyLink", "COPY LINK",
                new Vector2(0, 185), new Vector2(180, 42), new Color(0.2f, 0.2f, 0.4f), 16f);
            var statusTxt   = MakeText(pWaiting.transform, "StatusText", "",
                new Vector2(0, 150), new Vector2(700, 30), 18,
                new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);
            MakeText(pWaiting.transform, "PickLabel", "PICK YOUR FIGHTER",
                new Vector2(0, 115), new Vector2(500, 30), 20, Color.yellow, FontStyles.Bold);

            // Character grid (4×2)
            var gridGO   = new GameObject("CharacterGrid");
            gridGO.transform.SetParent(pWaiting.transform, false);
            var gridRect = gridGO.AddComponent<RectTransform>();
            gridRect.anchoredPosition = new Vector2(0, -28);
            gridRect.sizeDelta        = new Vector2(1020, 220);
            var layout = gridGO.AddComponent<GridLayoutGroup>();
            layout.cellSize        = new Vector2(235, 200);
            layout.spacing         = new Vector2(16, 16);
            layout.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;
            layout.childAlignment  = TextAnchor.MiddleCenter;

            var slots = new CharacterSlot[8];
            for (int i = 0; i < 8; i++)
            {
                var slotGO = new GameObject($"Slot_{i}");
                slotGO.transform.SetParent(gridGO.transform, false);
                var img  = slotGO.AddComponent<Image>();
                img.color = new Color(0.17f, 0.17f, 0.27f);
                var slot = slotGO.AddComponent<CharacterSlot>();
                slots[i] = slot;
                MakeText(slotGO.transform, "Name", "",
                    new Vector2(0, -76), new Vector2(220, 28), 15, Color.white, FontStyles.Normal);
            }

            var btnConfirm = MakeButton(pWaiting.transform, "Btn_Confirm", "CONFIRM",
                new Vector2(-110, -238), new Vector2(200, 65), new Color(0.1f, 0.55f, 0.1f));
            var btnCancel  = MakeButton(pWaiting.transform, "Btn_Cancel",  "CANCEL",
                new Vector2( 110, -238), new Vector2(160, 55), new Color(0.5f, 0.1f, 0.1f));

            // ── LobbyManager ──────────────────────────────────────────────────
            var lmGO = new GameObject("LobbyManager");
            var lm   = lmGO.AddComponent<LobbyManager>();
            SetField(lm, "sessionData",       LoadSessionData());
            SetField(lm, "allCharacters",     LoadAllCharacters());
            SetField(lm, "panelModeSelect",   pMode);
            SetField(lm, "panelHostJoin",     pHostJoin);
            SetField(lm, "panelWaitingRoom",  pWaiting);
            SetField(lm, "joinCodeField",     fldJoin);
            SetField(lm, "spectateCodeField", fldSpectate);
            SetField(lm, "roomCodeText",      roomCodeTxt);
            SetField(lm, "shareURLText",      shareURLTxt);
            SetField(lm, "statusText",        statusTxt);
            SetField(lm, "slots",             slots);

            // Wire all button onClick events
            AddButtonListener(btnVsAI,    lm, "PlayOffline");
            AddButtonListener(btnOnline,  lm, "ShowHostJoinPanel");
            AddButtonListener(btnCreate,  lm, "CreateRoom");
            AddButtonListener(btnJoin,    lm, "JoinRoom");
            AddButtonListener(btnSpectate,lm, "JoinAsSpectator");
            AddButtonListener(btnBack,    lm, "ShowModePanel");
            AddButtonListener(btnCopyLink,lm, "CopyLink");
            AddButtonListener(btnConfirm, lm, "ConfirmCharacter");
            AddButtonListener(btnCancel,  lm, "LeaveRoom");

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Lobby.unity");
            Debug.Log("Pong Legends: Lobby scene created at Assets/Scenes/Lobby.unity");
            AddSceneToBuildSettingsFirst("Assets/Scenes/Lobby.unity");
        }

        // ─── UI Helpers ─────────────────────────────────────────────────────────

        // Full-screen panel (stretches to canvas edges). No background image by default —
        // the camera background shows through so panels feel cohesive.
        private static GameObject MakePanel(Transform parent, string goName, bool active = true)
        {
            var go   = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.SetActive(active);
            return go;
        }

        private static Button MakeButton(Transform parent, string goName, string label,
                                          Vector2 pos, Vector2 size, Color bgColor,
                                          float fontSize = 22f)
        {
            var go   = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta        = size;
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();

            // Label
            var lblGO   = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lblRect = lblGO.AddComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static TMP_InputField MakeInputField(Transform parent, string goName,
                                                      string placeholder,
                                                      Vector2 pos, Vector2 size)
        {
            var go   = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta        = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.22f);
            var field = go.AddComponent<TMP_InputField>();

            // Text Area (clips content)
            var areaGO   = new GameObject("Text Area");
            areaGO.transform.SetParent(go.transform, false);
            var areaRect = areaGO.AddComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(10, 4);
            areaRect.offsetMax = new Vector2(-10, -4);
            areaGO.AddComponent<RectMask2D>();

            // Placeholder
            var phGO   = new GameObject("Placeholder");
            phGO.transform.SetParent(areaGO.transform, false);
            var phRect = phGO.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            var phTmp = phGO.AddComponent<TextMeshProUGUI>();
            phTmp.text      = placeholder;
            phTmp.fontSize  = 17;
            phTmp.color     = new Color(0.5f, 0.5f, 0.5f);
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Text
            var txtGO   = new GameObject("Text");
            txtGO.transform.SetParent(areaGO.transform, false);
            var txtRect = txtGO.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            var txtTmp = txtGO.AddComponent<TextMeshProUGUI>();
            txtTmp.fontSize  = 19;
            txtTmp.color     = Color.white;
            txtTmp.alignment = TextAlignmentOptions.MidlineLeft;

            field.textViewport  = areaRect;
            field.placeholder   = phTmp;
            field.textComponent = txtTmp;

            return field;
        }

        // Wires a zero-argument public method on target as a persistent Button.onClick listener.
        private static void AddButtonListener(Button button, Object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method == null)
            {
                Debug.LogError($"AddButtonListener: method '{methodName}' not found on {target.GetType().Name}");
                return;
            }
            var action = System.Delegate.CreateDelegate(typeof(UnityAction), target, method) as UnityAction;
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }
    }
}
