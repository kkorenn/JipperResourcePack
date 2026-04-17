using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using JALib.Core;
using JALib.Core.Patch;
using JALib.Core.Setting;
using JALib.Tools;
using JipperResourcePack.Async;
using JipperResourcePack.Keyviewer.OtherModApi;
using JipperResourcePack.SettingTool;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;
using Application = UnityEngine.Application;
using Object = UnityEngine.Object;

namespace JipperResourcePack.Keyviewer;

public class KeyViewer : Feature {
    private const int HandOutIndex = 20;
    private const int FootOutIndex = 36;
    private static readonly string[] CounterBannerSharedFileNames = [
        "KeyviewerBanner.png",
        "KeyviewerBanner.jpg",
        "KeyviewerBanner.jpeg",
        "KeyviewerBanner.avif",
        "counter_banner.png",
        "counter_banner.jpg",
        "counter_banner.jpeg",
        "counter_banner.avif"
    ];
    private static readonly string[] CounterBannerLegacyLeftFileNames = [
        "KeyviewerBanner.png",
        "KeyviewerBanner.jpg",
        "KeyviewerBanner.jpeg",
        "KeyviewerBanner.avif"
    ];
    private static readonly string[] CounterBannerLegacyRightFileNames = [
        "counter_banner.png",
        "counter_banner.jpg",
        "counter_banner.jpeg",
        "counter_banner.avif"
    ];
    private static readonly string[] CounterBannerLeftFileNames = [
        "KeyviewerBannerLeft.png",
        "KeyviewerBannerLeft.jpg",
        "KeyviewerBannerLeft.jpeg",
        "KeyviewerBannerLeft.avif",
        "counter_banner_left.png",
        "counter_banner_left.jpg",
        "counter_banner_left.jpeg",
        "counter_banner_left.avif"
    ];
    private static readonly string[] CounterBannerRightFileNames = [
        "KeyviewerBannerRight.png",
        "KeyviewerBannerRight.jpg",
        "KeyviewerBannerRight.jpeg",
        "KeyviewerBannerRight.avif",
        "counter_banner_right.png",
        "counter_banner_right.jpg",
        "counter_banner_right.jpeg",
        "counter_banner_right.avif"
    ];
    private static readonly string[] ColorSettingNames = [
        "Background",
        "BackgroundClicked",
        "Outline",
        "OutlineClicked",
        "Text",
        "TextClicked",
        "RainColor",
        "RainColor2",
        "RainColor3",
        "CounterBackgroundLeft",
        "CounterBackgroundRight",
        "CounterTintLeft",
        "CounterTintRight",
        "CounterTextLeft",
        "CounterTextRight"
    ];

    public static KeyViewerSettings Settings;
    public static readonly Color Background = new(0.5607843f, 0.2352941f, 1, 0.1960784f);
    public static readonly Color BackgroundClicked = Color.white;
    public static readonly Color Outline = new(0.5529412f, 0.2431373f, 1);
    public static readonly Color OutlineClicked = Color.white;
    public static readonly Color Text = Color.white;
    public static readonly Color TextClicked = Color.black;
    public static readonly Color RainColor = new(0.5137255f, 0.1254902f, 0.858823538f);
    public static readonly Color RainColor2 = Color.white;
    public static readonly Color RainColor3 = Color.magenta;
    public static readonly Color KeyLeftBackground = Background;
    public static readonly Color KeyRightBackground = Background;
    public static readonly Color KeyLeftOutline = Outline;
    public static readonly Color KeyRightOutline = Outline;
    public static readonly Color KeyLeftText = Text;
    public static readonly Color KeyRightText = Text;
    public static readonly Color KeyLeftBackgroundPressed = BackgroundClicked;
    public static readonly Color KeyRightBackgroundPressed = BackgroundClicked;
    public static readonly Color KeyLeftOutlinePressed = OutlineClicked;
    public static readonly Color KeyRightOutlinePressed = OutlineClicked;
    public static readonly Color KeyLeftTextPressed = TextClicked;
    public static readonly Color KeyRightTextPressed = TextClicked;
    public static readonly Color KeyLeftRainColor = RainColor2;
    public static readonly Color KeyRightRainColor = RainColor2;
    public static readonly Color CounterBackgroundLeft = new(0f, 0f, 0f, 0f);
    public static readonly Color CounterBackgroundRight = new(0f, 0f, 0f, 0f);
    public static readonly Color CounterTintLeft = Color.white;
    public static readonly Color CounterTintRight = Color.white;
    public static readonly Color CounterTextLeft = new(Text.r, Text.g, Text.b, 0.25f);
    public static readonly Color CounterTextRight = new(Text.r, Text.g, Text.b, 0.25f);
    public static readonly byte[] BackSequence10 = [8, 9];
    public static readonly byte[] BackSequence12 = [9, 8, 10, 11];
    public static readonly byte[] BackSequence16 = [12, 13, 9, 8, 10, 11, 14, 15];
    public static readonly byte[] BackSequence20 = [12, 13, 9, 8, 10, 11, 14, 15, 17, 16, 18, 19];
    public GameObject KeyViewerObject;
    public GameObject KeyViewerSizeObject;
    public Key[] Keys;
    public Thread KeyinputListener;
    public Key Kps;
    public int lastKps;
    public Key Total;
    public GameObject CounterBannerObject;
    public RawImage CounterBannerLeftImage;
    public RawImage CounterBannerRightImage;
    public RawImage CounterBannerLeftBackgroundFill;
    public RawImage CounterBannerRightBackgroundFill;
    public Image CounterBannerLeftFrameBackground;
    public Image CounterBannerRightFrameBackground;
    public Image CounterBannerLeftFrameOutline;
    public Image CounterBannerRightFrameOutline;
    public Image CounterBannerBackground;
    public Image CounterBannerOutline;
    public Texture2D CounterBannerLeftTexture;
    public Texture2D CounterBannerRightTexture;
    public AsyncText CounterBannerLeftCounterText;
    public AsyncText CounterBannerRightCounterText;
    public ConcurrentQueue<long> PressTimes;
    public Stopwatch Stopwatch;
    private bool Save;
    private bool KeyShare;
    private bool KeyChangeExpanded;
    private bool TextChangeExpanded;
    private bool[] ColorExpanded;
    private KeyviewerStyle currentKeyViewerStyle;
    private bool[] KeyPressed;
    private bool confirmResetCount;

    public int SelectedKey = -1;
    public int WinAPICool;
    public bool TextChanged;
    private string rainSizeString;
    private string rainHeightString;
    private string sizeString;
    private KeyViewerFrameUpdater frameUpdater;

    public KeyViewer() : base(Main.Instance, nameof(KeyViewer), settingType: typeof(KeyViewerSettings)) {
        if(ADOBase.platform != Platform.Windows) return;
        Patcher.AddPatch(Load);
        currentKeyViewerStyle = Settings.KeyViewerStyle;
        AdofaiTweaksAPI.Setup();
        KeyboardChatterBlockerAPI.Setup();
    }

    protected override void OnEnable() {
        KeyViewerObject = new GameObject("JipperResourcePack KeyViewer");
        Canvas canvas = KeyViewerObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        KeyViewerSizeObject = new GameObject("SizeObject");
        RectTransform rectTransform = KeyViewerSizeObject.AddComponent<RectTransform>();
        rectTransform.SetParent(KeyViewerObject.transform);
        rectTransform.localScale = new Vector3(Settings.Size, Settings.Size, 1);
        rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
        Keys = new Key[FootOutIndex];
        KeyViewerSettings settings = Settings;
        switch(settings.KeyViewerStyle) {
            case KeyviewerStyle.Key12:
                Initialize0KeyViewer();
                break;
            case KeyviewerStyle.Key16:
                Initialize1KeyViewer();
                break;
            case KeyviewerStyle.Key20:
                Initialize2KeyViewer();
                break;
            case KeyviewerStyle.Key10:
                Initialize3KeyViewer();
                break;
        }
        switch(settings.FootKeyViewerStyle) {
            case FootKeyviewerStyle.Key2:
                InitializeFootKeyViewer(2);
                break;
            case FootKeyviewerStyle.Key4:
                InitializeFootKeyViewer(4);
                break;
            case FootKeyviewerStyle.Key6:
                InitializeFootKeyViewer(6);
                break;
            case FootKeyviewerStyle.Key8:
                InitializeFootKeyViewer(8);
                break;
            case FootKeyviewerStyle.Key16:
                InitializeFootKeyViewer(16);
                break;
        }
        ApplyAllKeyColors();
        SetupCounterBanner();
        Object.DontDestroyOnLoad(KeyViewerObject);
        PressTimes = new ConcurrentQueue<long>();
        Stopwatch = Stopwatch.StartNew();
        frameUpdater = KeyViewerObject.AddComponent<KeyViewerFrameUpdater>();
        frameUpdater.owner = this;
        KeyinputListener = new Thread(ListenKey);
        KeyinputListener.Start();
        Application.wantsToQuit += Application_wantsToQuit;
        UpdateKeyLimit();
    }

    private bool Application_wantsToQuit() {
        KeyinputListener.Abort();
        KeyinputListener.Interrupt();
        return true;
    }

    protected override void OnDisable() {
        if(!KeyViewerObject) return;
        DestroyCounterBanner();
        Object.Destroy(KeyViewerObject);
        KeyViewerObject = null;
        KeyViewerSizeObject = null;
        GC.SuppressFinalize(Keys);
        Keys = null;
        KeyinputListener.Abort();
        KeyinputListener.Interrupt();
        KeyinputListener = null;
        if(frameUpdater) frameUpdater.owner = null;
        frameUpdater = null;
        GC.SuppressFinalize(PressTimes);
        PressTimes = null;
        Application.wantsToQuit -= Application_wantsToQuit;
    }

    protected override void OnGUI() {
        SettingGUI settingGUI = Main.SettingGUI;
        JALocalization localization = Main.Instance.Localization;
        KeyViewerSettings settings = Settings;
        settingGUI.AddSettingToggle(ref KeyShare, localization["keyViewer.keyShare"]);
        settingGUI.AddSettingToggle(ref settings.DownLocation, localization["keyViewer.downLocation"], ResetKeyViewer);
        settingGUI.AddSettingToggle(ref settings.CounterTextInsideBanners, "Show KPS/Total inside banner images", ResetKeyViewer);
        if(GUILayout.Button(localization["keyViewer.resetCount"])) confirmResetCount = true;
        if(confirmResetCount) {
            GUILayout.Label("<color=red>" + localization["keyViewer.resetCountConfirmText"] + "</color>");
            if(GUILayout.Button(localization["keyViewer.resetCountConfirm"])) {
                confirmResetCount = false;
                lastKps = 0;
                Kps.value.tmp.text = "0";
                Total.value.tmp.text = "0";
                for(int i = 0; i < settings.Count.Length; i++) settings.Count[i] = 0;
                settings.TotalCount = 0;
                UpdateCounterBannerCounterTexts();
                Main.Instance.SaveSetting();
            }
            if(GUILayout.Button(localization["keyViewer.resetCountCancel"])) confirmResetCount = false;
        }
        settingGUI.AddSettingToggle(ref settings.useRain, localization["keyViewer.useRain"], CheckResetRain);
        settingGUI.AddSettingSliderFloat(ref settings.rainSpeed, 100, ref rainSizeString, localization["keyViewer.rainSpeed"], 1, 800);
        settingGUI.AddSettingSliderFloat(ref settings.rainHeight, 200, ref rainHeightString, localization["keyViewer.rainHeight"], 1, 1000);
        if(ADOBase.platform == Platform.Windows && (AdofaiTweaksAPI.IsExist || KeyboardChatterBlockerAPI.IsExist))
            settingGUI.AddSettingToggle(ref settings.AutoSetupKeyLimit, localization["keyViewer.autoSetupKeyLimit"], UpdateKeyLimit);
        settingGUI.AddSettingEnum(ref settings.KeyViewerStyle, localization["keyViewer.style"], ChangeKeyViewer);
        settingGUI.AddSettingEnum(ref settings.FootKeyViewerStyle, localization["keyViewer.style"], ResetFootKeyViewer);
        settingGUI.AddSettingSliderFloat(ref settings.Size, 1, ref sizeString, localization["size"], 0, 2, () => {
            KeyViewerSizeObject.transform.localScale = new Vector3(settings.Size, settings.Size, 1);
        });
        KeyCode[] keyCodes = GetKeyCode();
        KeyCode[] footKeyCodes = GetFootKeyCode();
        string[] keyTexts = GetKeyText();
        GUILayout.Space(12f);
        GUIStyle toggleStyle = new() {
            fixedWidth = 10f,
            normal = new GUIStyleState { textColor = Color.white },
            fontSize = 15,
            margin = new RectOffset(4, 2, 6, 6)
        };
        GUILayout.BeginHorizontal();
        KeyChangeExpanded = GUILayout.Toggle(KeyChangeExpanded, KeyChangeExpanded ? "◢" : "▶", toggleStyle);
        if(GUILayout.Button(localization["keyViewer.keyChange"], GUI.skin.label)) KeyChangeExpanded = !KeyChangeExpanded;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        if(KeyChangeExpanded) {
            GUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            for(int i = 0; i < 8; i++) CreateButton(i, false);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            byte[] backSequence = GetBackSequence();
            for(int i = 0; i < backSequence.Length && i < 8; i++) CreateButton(backSequence[i], false);
            if(backSequence.Length > 8) {
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                for(int i = 8; i < backSequence.Length; i++) CreateButton(backSequence[i], false);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if(footKeyCodes != null) {
                GUILayout.BeginHorizontal();
                for(int i = 0; i < footKeyCodes.Length; i++) CreateButton(i++ + HandOutIndex, false);
                for(int i = 1; i < footKeyCodes.Length; i++) CreateButton(i++ + HandOutIndex, false);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            if(SelectedKey != -1 && !TextChanged) GUILayout.Label($"<b>{localization["keyViewer.inputKey"]}</b>");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(12f);
        }
        GUILayout.BeginHorizontal();
        TextChangeExpanded = GUILayout.Toggle(TextChangeExpanded, TextChangeExpanded ? "◢" : "▶", toggleStyle);
        if(GUILayout.Button(localization["keyViewer.textChange"], GUI.skin.label)) TextChangeExpanded = !TextChangeExpanded;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        if(TextChangeExpanded) {
            GUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            for(int i = 0; i < 8; i++) CreateButton(i, true);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            byte[] backSequence = GetBackSequence();
            for(int i = 0; i < backSequence.Length && i < 8; i++) CreateButton(backSequence[i], true);
            if(backSequence.Length > 8) {
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                for(int i = 8; i < backSequence.Length; i++) CreateButton(backSequence[i], true);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if(SelectedKey != -1 && TextChanged) {
                GUILayout.BeginHorizontal();
                GUILayout.Label(localization["keyViewer.inputText"]);
                string textArea = GUILayout.TextArea(keyTexts[SelectedKey] ?? KeyToString(keyCodes[SelectedKey]));
                if(keyTexts[SelectedKey] != textArea) {
                    Keys[SelectedKey].text.tmp.text = textArea;
                    if(textArea == KeyToString(keyCodes[SelectedKey])) textArea = null;
                    keyTexts[SelectedKey] = textArea;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if(GUILayout.Button(localization["keyViewer.textReset"])) {
                    keyTexts[SelectedKey] = null;
                    SelectedKey = -1;
                    Main.Instance.SaveSetting();
                }
                if(GUILayout.Button(localization["keyViewer.textSave"])) {
                    SelectedKey = -1;
                    Main.Instance.SaveSetting();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(12f);
        }
        GUILayout.BeginHorizontal();
        bool a = GUILayout.Toggle(ColorExpanded != null, ColorExpanded != null ? "◢" : "▶", toggleStyle);
        if(ColorExpanded != null != a) ColorExpanded = a ? new bool[ColorSettingNames.Length] : null;
        if(GUILayout.Button(localization["keyViewer.color"], GUI.skin.label)) ColorExpanded = ColorExpanded == null ? new bool[ColorSettingNames.Length] : null;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        if(ColorExpanded != null) {
            GUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            GUILayout.BeginVertical();
            string[] names = ColorSettingNames;
            for(int i = 0; i < names.Length; i++) {
                if(names[i] == "RainColor3" && Settings.KeyViewerStyle != KeyviewerStyle.Key20) continue;
                string label = names[i] switch {
                    "RainColor" => "Top Rain Color",
                    "RainColor2" => "Bottom Rain Color",
                    "RainColor3" => "Special Rain Color",
                    "CounterBackgroundLeft" => "Banner Left Background Fill",
                    "CounterBackgroundRight" => "Banner Right Background Fill",
                    "CounterTintLeft" => "Banner Left Image Color",
                    "CounterTintRight" => "Banner Right Image Color",
                    "CounterTextLeft" => "KPS Value Text Color",
                    "CounterTextRight" => "Total Value Text Color",
                    _ => localization["keyViewer.color." + char.ToLower(names[i][0]) + names[i][1..]]
                };
                GUILayout.BeginHorizontal();
                ColorExpanded[i] = GUILayout.Toggle(ColorExpanded[i], ColorExpanded[i] ? "◢" : "▶", toggleStyle);
                if(GUILayout.Button(label, GUI.skin.label)) ColorExpanded[i] = !ColorExpanded[i];
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if(!ColorExpanded[i]) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Space(18f);
                GUILayout.BeginVertical();
                if(settings.GetValue<ColorCache>(names[i]).SettingGUI(settingGUI, typeof(KeyViewer).GetValue<Color>(names[i]))) {
                    ApplyAllKeyColors();
                    Kps.background.color = Total.background.color = settings.Background;
                    Kps.outline.color = Total.outline.color = settings.Outline;
                    Kps.text.tmp.color = Kps.value.tmp.color = Total.text.tmp.color = Total.value.tmp.color = settings.Text;
                    UpdateCounterBannerTheme();
                    Main.Instance.SaveSetting();
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.Space(12f);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(12f);
        }
        if(SelectedKey == -1 || TextChanged || !Application.isFocused) return;
        if(Input.anyKeyDown) {
            foreach(KeyCode keyCode in Enum.GetValues(typeof(KeyCode))) {
                if(!Input.GetKeyDown(keyCode)) continue;
                SetupKey(keyCode);
                break;
            }
        } else {
            if(ADOBase.platform == Platform.Windows) {
                for(int i = 0; i < 256; i++) {
                    if((GetAsyncKeyState(i) & 0x8000) != 0 == KeyPressed[i]) continue;
                    if(KeyPressed[i]) {
                        KeyPressed[i] = false;
                        WinAPICool = 0;
                        continue;
                    }
                    if(WinAPICool++ < 6) break;
                    KeyCode keyCode = (KeyCode) i + 0x1000;
                    SetupKey(keyCode);
                    break;
                }
            }
        }
        return;

        void CreateButton(int i, bool textChanged) {
            if(!GUILayout.Button(Bold(i < HandOutIndex ? textChanged ? keyTexts[i] ?? KeyToString(keyCodes[i]) : ToString(keyCodes[i]) : ToString(footKeyCodes[i - HandOutIndex]),
                   i == SelectedKey && textChanged == TextChanged))) return;
            SelectedKey = i;
            TextChanged = textChanged;
            if(textChanged) return;
            WinAPICool = 0;
            KeyPressed = new bool[256];
            for(int i2 = 0; i2 < 256; i2++) KeyPressed[i2] = (GetAsyncKeyState(i2) & 0x8000) != 0;
        }

        void SetupKey(KeyCode keyCode) {
            if(SelectedKey < HandOutIndex) keyCodes[SelectedKey] = keyCode;
            else footKeyCodes[SelectedKey - HandOutIndex] = keyCode;
            Keys[SelectedKey].text.tmp.text = (SelectedKey < HandOutIndex ? keyTexts[SelectedKey] : null) ?? KeyToString(keyCode);
            SelectedKey = -1;
            WinAPICool = 0;
            KeyPressed = null;
            UpdateKeyLimit();
            Main.Instance.SaveSetting();
        }
    }

    private static KeyCode[] GetKeyCode() {
        return Settings.KeyViewerStyle switch {
            KeyviewerStyle.Key12 => Settings.key12,
            KeyviewerStyle.Key16 => Settings.key16,
            KeyviewerStyle.Key20 => Settings.key20,
            KeyviewerStyle.Key10 => Settings.key10,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static KeyCode[] GetFootKeyCode() {
        return Settings.FootKeyViewerStyle switch {
            FootKeyviewerStyle.Key2 => Settings.footkey2,
            FootKeyviewerStyle.Key4 => Settings.footkey4,
            FootKeyviewerStyle.Key6 => Settings.footkey6,
            FootKeyviewerStyle.Key8 => Settings.footkey8,
            FootKeyviewerStyle.Key16 => Settings.footkey16,
            _ => []
        };
    }

    private static string[] GetKeyText() {
        return Settings.KeyViewerStyle switch {
            KeyviewerStyle.Key12 => Settings.key12Text,
            KeyviewerStyle.Key16 => Settings.key16Text,
            KeyviewerStyle.Key20 => Settings.key20Text,
            KeyviewerStyle.Key10 => Settings.key10Text,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static byte[] GetBackSequence() {
        return Settings.KeyViewerStyle switch {
            KeyviewerStyle.Key12 => BackSequence12,
            KeyviewerStyle.Key16 => BackSequence16,
            KeyviewerStyle.Key20 => BackSequence20,
            KeyviewerStyle.Key10 => BackSequence10,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static string ToString(KeyCode keyCode) {
        if((int) keyCode < 0x1000) return keyCode.ToString();
        #region Custom KeyCode To String

        int code = (int) keyCode - 0x1000;
        switch(ADOBase.platform) {
            case Platform.Windows:
                return code switch {
                    >= 0x7C and <= 0x87 => "F" + (code - 0x6F),
                    >= 0x92 and <= 0x96 or 0xE1 or 0xE3 or 0xE4 or >= 0xE9 and <= 0xF5 => "OEM" + code,
                    _ => code switch {
                        0x15 => "RightAlt",
                        0x16 => "IME ON",
                        0x17 => "Junja",
                        0x18 => "Final",
                        0x19 => "RightControl",
                        0x1A => "IME OFF",
                        0x1C => "Convert",
                        0x1D => "NonConvert",
                        0x1E => "Accept",
                        0x1F => "ModeChange",
                        0xA6 => "BrowserBack",
                        0xA7 => "BrowserForward",
                        0xA8 => "BrowserRefresh",
                        0xA9 => "BrowserStop",
                        0xAA => "BrowserSearch",
                        0xAB => "BrowserFavorites",
                        0xAC => "BrowserHome",
                        0xAD => "VolumeMute",
                        0xAE => "VolumeDown",
                        0xAF => "VolumeUp",
                        0xB0 => "MediaNextTrack",
                        0xB1 => "MediaPreviousTrack",
                        0xB2 => "MediaStop",
                        0xB3 => "MediaPlayPause",
                        0xB4 => "LaunchMail",
                        0xB5 => "SelectMedia",
                        0xB6 => "LaunchApplication1",
                        0xB7 => "LaunchApplication2",
                        0xC1 => @"-\ろ",
                        0xDF => "OME8",
                        0xE2 => @"\\|",
                        0xE5 => "Process",
                        0xE7 => "Packet",
                        0xEB => "変換",
                        0xF6 => "Attn",
                        0xF7 => "CrSel",
                        0xF8 => "ExSel",
                        0xF9 => "EraseEOF",
                        0xFA => "Play",
                        0xFB => "Zoom",
                        0xFC => "NoName",
                        0xFD => "PA1",
                        0xFE => "Clear",
                        _ => "Key" + code
                    }
                };
            case Platform.Linux:
                return code switch {
                    >= 0xB7 and <= 0xC2 => "F" + (code - 0xAA),
                    0x54 or >= 0xC3 and <= 0xC7 or >= 0xF7 and <= 0xFF => "unnamed" + code,
                    _ => code switch {
                        0x00 => "Reserved",
                        0x55 => "Zenkakuhankaku",
                        0x56 => "102ND",
                        0x59 => "RO",
                        0x5A => "Katakana",
                        0x5B => "Hiragana",
                        0x5C => "Henkan",
                        0x5D => "KatakanaHiragana",
                        0x5E => "Muhenkan",
                        0x5F => "Comma",
                        0x60 => "Enter",
                        0x61 => "RightControl",
                        0x62 => "Slash",
                        0x63 => "SysRq",
                        0x64 => "RightAlt",
                        0x65 => "LineFeed",
                        0x70 => "Macro",
                        0x71 => "Mute",
                        0x72 => "VolumeDown",
                        0x73 => "VolumeUp",
                        0x74 => "Power",
                        0x75 => "Equal",
                        0x76 => "PlusMinus",
                        0x79 => "Pause",
                        0x7A => "RightAlt",
                        0x7B => "RightControl",
                        0x7C => "Yen",
                        0x7F => "Compose",
                        0x80 => "Stop",
                        0x81 => "Again",
                        0x82 => "Props",
                        0x83 => "Undo",
                        0x84 => "Front",
                        0x85 => "Copy",
                        0x86 => "Open",
                        0x87 => "Paste",
                        0x88 => "Find",
                        0x89 => "Cut",
                        0x8A => "Help",
                        0x8B => "Menu",
                        0x8C => "Calc",
                        0x8D => "Setup",
                        0x8E => "Sleep",
                        0x8F => "WakeUp",
                        0x90 => "File",
                        0x91 => "SendFile",
                        0x92 => "DeleteFile",
                        0x93 => "Xfer",
                        0x94 => "Prog1",
                        0x95 => "Prog2",
                        0x96 => "WWW",
                        0x97 => "MSDOS",
                        0x99 => "Direction",
                        0x9A => "CycleWindows",
                        0x9B => "Mail",
                        0x9C => "Bookmarks",
                        0x9D => "Computer",
                        0x9E => "Back",
                        0x9F => "Forward",
                        0xA0 => "CloseCD",
                        0xA1 => "EjectCD",
                        0xA2 => "EjectCloseCD",
                        0xA3 => "NextSong",
                        0xA4 => "PlayPause",
                        0xA5 => "PreviousSong",
                        0xA6 => "StopCD",
                        0xA7 => "Record",
                        0xA8 => "Rewind",
                        0xA9 => "Phone",
                        0xAA => "ISO",
                        0xAB => "Config",
                        0xAC => "HomePage",
                        0xAD => "Refresh",
                        0xAE => "Exit",
                        0xAF => "Move",
                        0xB0 => "Edit",
                        0xB3 => "LeftParen",
                        0xB4 => "RightParen",
                        0xB5 => "New",
                        0xB6 => "Redo",
                        0xC8 => "PlayCD",
                        0xC9 => "PauseCD",
                        0xCA => "Prog3",
                        0xCB => "Prog4",
                        0xCC => "Dashboard",
                        0xCD => "Suspend",
                        0xCE => "Close",
                        0xCF => "Play",
                        0xD0 => "FastForward",
                        0xD1 => "BassBoost",
                        0xD2 => "Print",
                        0xD3 => "HP",
                        0xD4 => "Camera",
                        0xD5 => "Sound",
                        0xD6 => "Question",
                        0xD7 => "Email",
                        0xD8 => "Chat",
                        0xD9 => "Search",
                        0xDA => "Connect",
                        0xDB => "Finance",
                        0xDC => "Sport",
                        0xDD => "Shop",
                        0xDE => "AltErase",
                        0xDF => "Cancel",
                        0xE0 => "BrightnessDown",
                        0xE1 => "BrightnessUp",
                        0xE2 => "Media",
                        0xE3 => "SwitchVideoMode",
                        0xE4 => "KbdIllumToggle",
                        0xE5 => "KbdIllumDown",
                        0xE6 => "KbdIllumUp",
                        0xE7 => "Send",
                        0xE8 => "Reply",
                        0xE9 => "ForwardMail",
                        0xEA => "Save",
                        0xEB => "Documents",
                        0xEC => "Battery",
                        0xED => "Bluetooth",
                        0xEE => "WLAN",
                        0xEF => "UWB",
                        0xF0 => "Unknown",
                        0xF1 => "VideoNext",
                        0xF2 => "VideoPrev",
                        0xF3 => "BrightnessCycle",
                        0xF4 => "BrightnessZero",
                        0xF5 => "DisplayOff",
                        0xF6 => "Wimax",
                        _ => "Key" + code
                    }
                };
            case Platform.Mac:
                return code switch {
                    105 => "F13",
                    107 => "F14",
                    113 => "F15",
                    106 => "F16",
                    64 => "F17",
                    79 => "F18",
                    80 => "F19",
                    90 => "F20",
                    63 => "fn",
                    261 => "Option",
                    55 => "Super",
                    _ => "Key" + code
                };
        }
        return "Key" + code;

        #endregion
    }

    private void CheckResetRain() {
        if(Settings.useRain) return;
        foreach(Key key in Keys) {
            if(!key) continue;
            key.RawRainQueue.Clear();
            while(key.RainList.Count > 0) {
                key.RainList[0].removed = true;
                key.RainList.RemoveAt(0);
            }
        }
    }

    private void ChangeKeyViewer() {
        KeyViewerSettings settings = Settings;
        if(KeyShare) {
            KeyCode[] keyCode1 = GetKeyCode();
            KeyCode[] keyCode2;
            string[] keyText1 = GetKeyText();
            string[] keyText2;
            switch(currentKeyViewerStyle) {
                case KeyviewerStyle.Key12:
                    keyCode2 = settings.key12;
                    keyText2 = settings.key12Text;
                    break;
                case KeyviewerStyle.Key16:
                    keyCode2 = settings.key16;
                    keyText2 = settings.key16Text;
                    break;
                case KeyviewerStyle.Key20:
                    keyCode2 = settings.key20;
                    keyText2 = settings.key20Text;
                    break;
                case KeyviewerStyle.Key10:
                    keyCode2 = settings.key10;
                    keyText2 = settings.key10Text;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            int size = Math.Min(keyCode1.Length, keyCode2.Length);
            for(int i = 0; i < size; i++) {
                keyCode1[i] = keyCode2[i];
                keyText1[i] = keyText2[i];
            }
            Mod.SaveSetting();
        }
        currentKeyViewerStyle = settings.KeyViewerStyle;
        ResetKeyViewer();
    }

    private void ResetKeyViewer() {
        SelectedKey = -1;
        DestroyCounterBanner();
        for(int i = 0; i < HandOutIndex; i++) {
            Key key = Keys[i];
            if(key) Object.Destroy(key.gameObject);
        }
        Object.Destroy(Total.gameObject);
        Object.Destroy(Kps.gameObject);
        switch(Settings.KeyViewerStyle) {
            case KeyviewerStyle.Key12:
                Initialize0KeyViewer();
                break;
            case KeyviewerStyle.Key16:
                Initialize1KeyViewer();
                break;
            case KeyviewerStyle.Key20:
                Initialize2KeyViewer();
                break;
            case KeyviewerStyle.Key10:
                Initialize3KeyViewer();
                break;
        }
        ApplyAllKeyColors();
        SetupCounterBanner();
        UpdateKeyLimit();
    }

    private void ResetFootKeyViewer() {
        for(int i = HandOutIndex; i < FootOutIndex; i++) {
            Key key = Keys[i];
            if(key) Object.Destroy(key.gameObject);
        }
        switch(Settings.FootKeyViewerStyle) {
            case FootKeyviewerStyle.Key2:
                InitializeFootKeyViewer(2);
                break;
            case FootKeyviewerStyle.Key4:
                InitializeFootKeyViewer(4);
                break;
            case FootKeyviewerStyle.Key6:
                InitializeFootKeyViewer(6);
                break;
            case FootKeyviewerStyle.Key8:
                InitializeFootKeyViewer(8);
                break;
            case FootKeyviewerStyle.Key16:
                InitializeFootKeyViewer(16);
                break;
        }
        ApplyAllKeyColors();
        UpdateKeyLimit();
    }

    private void ApplyAllKeyColors() {
        KeyCode[] keyCodes = GetKeyCode();
        for(int i = 0; i < keyCodes.Length; i++) UpdateKey(i, CheckKey(keyCodes[i]));
        KeyCode[] footKeyCodes = GetFootKeyCode();
        if(footKeyCodes == null) return;
        for(int i = 0; i < footKeyCodes.Length; i++) UpdateKey(i + HandOutIndex, CheckKey(footKeyCodes[i]));
    }

    private void SetupCounterBanner() {
        DestroyCounterBanner();
        if(!Kps || !Total) return;

        RectTransform kpsRect = (RectTransform) Kps.transform;
        RectTransform totalRect = (RectTransform) Total.transform;
        float left = Math.Min(kpsRect.anchoredPosition.x, totalRect.anchoredPosition.x);
        float right = Math.Max(kpsRect.anchoredPosition.x + kpsRect.sizeDelta.x, totalRect.anchoredPosition.x + totalRect.sizeDelta.x);
        float width = right - left;
        float height = width * 9f / 16f;
        float top = Math.Max(
            kpsRect.anchoredPosition.y + kpsRect.sizeDelta.y * 0.5f,
            totalRect.anchoredPosition.y + totalRect.sizeDelta.y * 0.5f
        );
        float centerY = top - height * 0.5f;

        CounterBannerObject = new GameObject("CounterBanner");
        RectTransform bannerRect = CounterBannerObject.AddComponent<RectTransform>();
        bannerRect.SetParent(KeyViewerSizeObject.transform);
        bannerRect.anchorMin = bannerRect.anchorMax = Vector2.zero;
        bannerRect.pivot = new Vector2(0, 0.5f);
        bannerRect.anchoredPosition = new Vector2(left, centerY);
        bannerRect.sizeDelta = new Vector2(width, height);
        bannerRect.localScale = Vector3.one;
        bannerRect.SetAsFirstSibling();

        const float panelGap = 4f;
        const float framePadding = 2f;
        float clampedGap = panelGap;
        float panelMaxWidth = (width - clampedGap) * 0.5f;
        if(panelMaxWidth <= 0f) {
            clampedGap = 0f;
            panelMaxWidth = width * 0.5f;
        }

        CounterBannerLeftTexture = LoadCounterBannerTexture(CounterBannerLeftFileNames, "left");
        CounterBannerRightTexture = LoadCounterBannerTexture(CounterBannerRightFileNames, "right");
        if(!CounterBannerLeftTexture || !CounterBannerRightTexture) {
            // Legacy behavior support: treat KeyviewerBanner.* as left and counter_banner.* as right.
            Texture2D legacyLeft = LoadCounterBannerTexture(CounterBannerLegacyLeftFileNames, "legacy-left");
            Texture2D legacyRight = LoadCounterBannerTexture(CounterBannerLegacyRightFileNames, "legacy-right");
            if(!CounterBannerLeftTexture) CounterBannerLeftTexture = legacyLeft;
            if(!CounterBannerRightTexture) CounterBannerRightTexture = legacyRight;
        }
        Texture2D sharedTexture = null;
        if(!CounterBannerLeftTexture || !CounterBannerRightTexture) {
            sharedTexture = LoadCounterBannerTexture(CounterBannerSharedFileNames, "shared");
            if(!CounterBannerLeftTexture) CounterBannerLeftTexture = sharedTexture;
            if(!CounterBannerRightTexture) CounterBannerRightTexture = sharedTexture;
        }
        if(!CounterBannerLeftTexture && !CounterBannerRightTexture) {
            Main.Instance.Warning(
                "Keyviewer banner images not found. Add KeyviewerBannerLeft/Right(.png/.jpg/.jpeg/.avif), or KeyviewerBanner(.png/.jpg/.jpeg/.avif)."
            );
        }
        float maxContentWidth = Mathf.Max(1f, panelMaxWidth - framePadding * 2f);
        float maxContentHeight = Mathf.Max(1f, height - framePadding * 2f);
        Vector2 leftContentSize = GetContainedSize(CounterBannerLeftTexture, maxContentWidth, maxContentHeight);
        Vector2 rightContentSize = GetContainedSize(CounterBannerRightTexture, maxContentWidth, maxContentHeight);
        float leftFrameWidth = leftContentSize.x + framePadding * 2f;
        float rightFrameWidth = rightContentSize.x + framePadding * 2f;
        float leftFrameHeight = leftContentSize.y + framePadding * 2f;
        float rightFrameHeight = rightContentSize.y + framePadding * 2f;
        float groupWidth = leftFrameWidth + clampedGap + rightFrameWidth;
        float groupX = Mathf.Max(0f, (width - groupWidth) * 0.5f);

        RectTransform leftPanelRect = CreateCounterBannerPanelRect("LeftPanel", bannerRect, groupX, leftFrameWidth, leftFrameHeight);
        RectTransform rightPanelRect = CreateCounterBannerPanelRect("RightPanel", bannerRect, groupX + leftFrameWidth + clampedGap, rightFrameWidth, rightFrameHeight);

        CounterBannerLeftFrameBackground = CreateCounterBannerLayer(
            "LeftFrameBackground",
            leftPanelRect,
            BundleLoader.KeyBackground,
            new Vector2(leftFrameWidth * 2f, leftFrameHeight * 2f),
            0f,
            Settings.Background
        );
        RectTransform leftMaskRect = CreateCounterBannerMask("LeftImageMask", leftPanelRect, leftFrameWidth, leftFrameHeight);

        CounterBannerRightFrameBackground = CreateCounterBannerLayer(
            "RightFrameBackground",
            rightPanelRect,
            BundleLoader.KeyBackground,
            new Vector2(rightFrameWidth * 2f, rightFrameHeight * 2f),
            0f,
            Settings.Background
        );
        RectTransform rightMaskRect = CreateCounterBannerMask("RightImageMask", rightPanelRect, rightFrameWidth, rightFrameHeight);

        CounterBannerBackground = null;
        CounterBannerOutline = null;
        Color fallbackColor = new(0.12f, 0.16f, 0.21f, 1f);
        CounterBannerLeftBackgroundFill = CreateCounterBannerTintLayer(
            "LeftBackgroundFill",
            leftMaskRect,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            Settings.CounterBackgroundLeft
        );
        CounterBannerRightBackgroundFill = CreateCounterBannerTintLayer(
            "RightBackgroundFill",
            rightMaskRect,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            Settings.CounterBackgroundRight
        );
        CounterBannerLeftImage = CreateCounterBannerImageLayer(
            "LeftImage",
            leftMaskRect,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            CounterBannerLeftTexture,
            fallbackColor,
            Settings.CounterTintLeft
        );
        CounterBannerRightImage = CreateCounterBannerImageLayer(
            "RightImage",
            rightMaskRect,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            CounterBannerRightTexture,
            fallbackColor,
            Settings.CounterTintRight
        );
        CounterBannerLeftFrameOutline = CreateCounterBannerLayer(
            "LeftFrameOutline",
            leftPanelRect,
            BundleLoader.KeyOutline,
            new Vector2(leftFrameWidth * 2f, leftFrameHeight * 2f),
            0f,
            Settings.Outline
        );
        CounterBannerRightFrameOutline = CreateCounterBannerLayer(
            "RightFrameOutline",
            rightPanelRect,
            BundleLoader.KeyOutline,
            new Vector2(rightFrameWidth * 2f, rightFrameHeight * 2f),
            0f,
            Settings.Outline
        );

        bool showCountersInsideBanners = Settings.CounterTextInsideBanners;
        if(showCountersInsideBanners) {
            CounterBannerLeftCounterText = CreateCounterBannerCounterText("LeftCounterText", leftMaskRect, lastKps.ToString(), Settings.CounterTextLeft);
            CounterBannerRightCounterText = CreateCounterBannerCounterText(
                "RightCounterText",
                rightMaskRect,
                Settings.TotalCount.ToString(),
                Settings.CounterTextRight
            );
            UpdateCounterBannerCounterTexts();
        }

        Kps.gameObject.SetActive(!showCountersInsideBanners);
        Total.gameObject.SetActive(!showCountersInsideBanners);
    }

    private static Image CreateCounterBannerLayer(string name, RectTransform parent, Sprite sprite, Vector2 size, float y, Color color) {
        GameObject gameObject = new(name);
        RectTransform transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(parent);
        transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
        transform.anchoredPosition = new Vector2(0f, y);
        transform.sizeDelta = size;
        transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        Image image = gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    private static RectTransform CreateCounterBannerPanelRect(string name, RectTransform parent, float x, float width, float height) {
        GameObject panelObject = new(name);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.SetParent(parent);
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(x, 0f);
        panelRect.sizeDelta = new Vector2(width, height);
        panelRect.localScale = Vector3.one;
        return panelRect;
    }

    private static RectTransform CreateCounterBannerMask(string name, RectTransform parent, float width, float height) {
        const float inset = 0.5f;
        const float overscan = 1.5f;
        GameObject maskObject = new(name);
        RectTransform maskRect = maskObject.AddComponent<RectTransform>();
        maskRect.SetParent(parent);
        maskRect.anchorMin = maskRect.anchorMax = maskRect.pivot = Vector2.zero;
        maskRect.anchoredPosition = new Vector2(inset - overscan, inset - overscan);
        maskRect.sizeDelta = new Vector2(
            Mathf.Max(1f, width - inset * 2f + overscan * 2f),
            Mathf.Max(1f, height - inset * 2f + overscan * 2f)
        );
        maskRect.localScale = Vector3.one;
        Image maskImage = maskObject.AddComponent<Image>();
        maskImage.sprite = BundleLoader.KeyBackground;
        maskImage.type = Image.Type.Sliced;
        maskImage.color = Color.white;
        Mask mask = maskObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        return maskRect;
    }

    private static RawImage CreateCounterBannerTintLayer(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Color color) {
        GameObject gameObject = new(name);
        RectTransform transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(parent);
        transform.anchorMin = anchorMin;
        transform.anchorMax = anchorMax;
        transform.offsetMin = Vector2.zero;
        transform.offsetMax = Vector2.zero;
        transform.localScale = Vector3.one;
        RawImage image = gameObject.AddComponent<RawImage>();
        image.texture = Texture2D.whiteTexture;
        image.color = color;
        return image;
    }

    private static RawImage CreateCounterBannerImageLayer(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Texture2D texture,
        Color fallbackColor,
        Color imageColor
    ) {
        GameObject containerObject = new(name + "Container");
        RectTransform containerTransform = containerObject.AddComponent<RectTransform>();
        containerTransform.SetParent(parent);
        containerTransform.anchorMin = anchorMin;
        containerTransform.anchorMax = anchorMax;
        containerTransform.offsetMin = Vector2.zero;
        containerTransform.offsetMax = Vector2.zero;
        containerTransform.localScale = Vector3.one;

        GameObject gameObject = new(name);
        RectTransform transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(containerTransform);
        transform.anchorMin = transform.anchorMax = new Vector2(0.5f, 1f);
        transform.pivot = new Vector2(0.5f, 1f);
        transform.anchoredPosition = Vector2.zero;
        transform.localScale = Vector3.one;
        float containerWidth = parent.rect.width * (anchorMax.x - anchorMin.x);
        float containerHeight = parent.rect.height * (anchorMax.y - anchorMin.y);
        if(texture && texture.height > 0 && containerWidth > 0f && containerHeight > 0f) {
            float imageAspect = (float) texture.width / texture.height;
            float containerAspect = containerWidth / containerHeight;
            float width = containerWidth;
            float height = containerHeight;
            if(imageAspect > containerAspect) {
                height = width / imageAspect;
            } else {
                width = height * imageAspect;
            }
            transform.sizeDelta = new Vector2(width, height);
        } else {
            transform.sizeDelta = new Vector2(containerWidth, containerHeight);
        }
        RawImage image = gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.color = texture ? OpaqueTint(imageColor) : fallbackColor;
        return image;
    }

    private static AsyncText CreateCounterBannerCounterText(string name, RectTransform parent, string value, Color color) {
        GameObject gameObject = new(name);
        RectTransform transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(parent);
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.offsetMin = new Vector2(4f, 4f);
        transform.offsetMax = new Vector2(-4f, -4f);
        transform.localScale = Vector3.one;

        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.font = BundleLoader.FontAsset;
        text.richText = true;
        text.enableAutoSizing = true;
        text.fontSizeMin = 8f;
        text.fontSizeMax = 28f;
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.text = value;
        return gameObject.AddComponent<AsyncText>();
    }

    private static Vector2 GetContainedSize(Texture2D texture, float maxWidth, float maxHeight) {
        float width = Mathf.Max(1f, maxWidth);
        float height = Mathf.Max(1f, maxHeight);
        if(!texture || texture.height <= 0) return new Vector2(width, height);
        float imageAspect = (float) texture.width / texture.height;
        float boxAspect = width / height;
        if(imageAspect > boxAspect) {
            height = width / imageAspect;
        } else {
            width = height * imageAspect;
        }
        return new Vector2(width, height);
    }

    private Texture2D LoadCounterBannerTexture(string[] fileNames, string bannerName) {
        foreach(string fileName in fileNames) {
            string path = Path.Combine(Main.Instance.Path, fileName);
            if(!File.Exists(path)) continue;
            try {
                byte[] imageBytes = TryReadBannerImageBytes(path);
                if(imageBytes == null || imageBytes.Length == 0) continue;
                Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                if(!texture.LoadImage(imageBytes, false)) {
                    Object.Destroy(texture);
                    continue;
                }
                Main.Instance.Log($"Loaded {bannerName} keyviewer banner image: {path}");
                return texture;
            } catch(Exception e) {
                Main.Instance.LogException(e);
            }
        }
        return null;
    }

    private static byte[] TryReadBannerImageBytes(string path) {
        if(!path.EndsWith(".avif", StringComparison.OrdinalIgnoreCase)) return File.ReadAllBytes(path);
        if(ADOBase.platform != Platform.Mac) {
            Main.Instance.Warning("AVIF banner is currently supported on macOS only.");
            return null;
        }
        return ConvertAvifToPngBytes(path);
    }

    private static byte[] ConvertAvifToPngBytes(string path) {
        string outputPath = Path.Combine(Path.GetTempPath(), $"jrp_banner_{Guid.NewGuid():N}.png");
        try {
            Process process = new() {
                StartInfo = new ProcessStartInfo {
                    FileName = "/usr/bin/sips",
                    Arguments = $"-s format png {QuoteProcessArgument(path)} --out {QuoteProcessArgument(outputPath)}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            process.WaitForExit();
            if(process.ExitCode != 0 || !File.Exists(outputPath)) {
                Main.Instance.Warning($"Failed to decode AVIF banner with sips (exit code {process.ExitCode}).");
                return null;
            }
            return File.ReadAllBytes(outputPath);
        } catch(Exception e) {
            Main.Instance.LogException(e);
            return null;
        } finally {
            if(File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    private static string QuoteProcessArgument(string value) {
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    private void UpdateCounterBannerTheme() {
        if(CounterBannerBackground) CounterBannerBackground.color = Settings.Background;
        if(CounterBannerOutline) CounterBannerOutline.color = Settings.Outline;
        if(CounterBannerLeftFrameBackground) CounterBannerLeftFrameBackground.color = Settings.Background;
        if(CounterBannerRightFrameBackground) CounterBannerRightFrameBackground.color = Settings.Background;
        if(CounterBannerLeftFrameOutline) CounterBannerLeftFrameOutline.color = Settings.Outline;
        if(CounterBannerRightFrameOutline) CounterBannerRightFrameOutline.color = Settings.Outline;
        if(CounterBannerLeftBackgroundFill) CounterBannerLeftBackgroundFill.color = Settings.CounterBackgroundLeft;
        if(CounterBannerRightBackgroundFill) CounterBannerRightBackgroundFill.color = Settings.CounterBackgroundRight;
        if(CounterBannerLeftImage && CounterBannerLeftImage.texture) CounterBannerLeftImage.color = OpaqueTint(Settings.CounterTintLeft);
        if(CounterBannerRightImage && CounterBannerRightImage.texture) CounterBannerRightImage.color = OpaqueTint(Settings.CounterTintRight);
        if(CounterBannerLeftCounterText) CounterBannerLeftCounterText.color = Settings.CounterTextLeft;
        if(CounterBannerRightCounterText) CounterBannerRightCounterText.color = Settings.CounterTextRight;
    }

    private static Color OpaqueTint(Color color) {
        color.a = 1f;
        return color;
    }

    private void UpdateCounterBannerCounterTexts() {
        if(!Settings.CounterTextInsideBanners) return;
        if(CounterBannerLeftCounterText) CounterBannerLeftCounterText.text = lastKps.ToString();
        if(CounterBannerRightCounterText) CounterBannerRightCounterText.text = Settings.TotalCount.ToString();
    }

    private void DestroyCounterBanner() {
        if(CounterBannerObject) Object.Destroy(CounterBannerObject);
        CounterBannerObject = null;
        CounterBannerLeftImage = null;
        CounterBannerRightImage = null;
        CounterBannerLeftBackgroundFill = null;
        CounterBannerRightBackgroundFill = null;
        CounterBannerLeftFrameBackground = null;
        CounterBannerRightFrameBackground = null;
        CounterBannerLeftFrameOutline = null;
        CounterBannerRightFrameOutline = null;
        CounterBannerBackground = null;
        CounterBannerOutline = null;
        CounterBannerLeftCounterText = null;
        CounterBannerRightCounterText = null;
        if(CounterBannerLeftTexture) Object.Destroy(CounterBannerLeftTexture);
        if(CounterBannerRightTexture && CounterBannerRightTexture != CounterBannerLeftTexture) Object.Destroy(CounterBannerRightTexture);
        CounterBannerLeftTexture = null;
        CounterBannerRightTexture = null;
    }

    private static string Bold(string text, bool bold) {
        return bold ? $"<b>{text}</b>" : text;
    }

    protected override void OnHideGUI() {
        WinAPICool = 0;
        sizeString = null;
        KeyPressed = null;
        confirmResetCount = false;
        if(SelectedKey == -1) return;
        Main.Instance.SaveSetting();
        SelectedKey = -1;
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool CheckKey(KeyCode keyCode) {
        return (int) keyCode < 0x1000 ? Input.GetKey(keyCode) : GetAsyncKeyState((int) keyCode - 0x1000) != 0;
    }

    private void UpdateKpsPerFrame() {
        if(!Enabled || PressTimes == null || Stopwatch == null || !Kps || !Kps.value) return;
        long elapsedMilliseconds = Stopwatch.ElapsedMilliseconds;
        while(PressTimes.TryPeek(out long result)) {
            if(elapsedMilliseconds - result > 1000) {
                PressTimes.TryDequeue(out long _);
                continue;
            }
            break;
        }
        lastKps = PressTimes.Count;
        Kps.value.text = lastKps.ToString();
        if(Settings.CounterTextInsideBanners && CounterBannerLeftCounterText) CounterBannerLeftCounterText.text = lastKps.ToString();
    }

    private class KeyViewerFrameUpdater : MonoBehaviour {
        public KeyViewer owner;

        private void Update() {
            owner?.UpdateKpsPerFrame();
        }
    }

    private void ListenKey() {
        try {
            KeyViewerSettings settings = Settings;
            bool[] keyState = new bool[FootOutIndex];
            int repeat = 0;
            while(KeyinputListener is { IsAlive: true } && Enabled) {
                long elapsedMilliseconds = Stopwatch.ElapsedMilliseconds;
                float speed = settings.rainSpeed;
                float height = settings.rainHeight;
                KeyCode[] keyCodes = GetKeyCode();
                for(int i = 0; i < keyCodes.Length; i++) {
                    bool current = CheckKey(keyCodes[i]);
                    Key key = Keys[i];
                    if(!key) continue;
                    for(int j = 0; j < key.RainList.Count; j++) {
                        RawRain rain = key.RainList[j];
                        if(rain.UpdateLocation(elapsedMilliseconds, current && keyState[i] && j == key.RainList.Count - 1, speed, height)) continue;
                        key.RainList.Remove(rain);
                        rain.removed = true;
                        j--;
                    }
                    if(current == keyState[i]) continue;
                    keyState[i] = current;
                    UpdateKey(i, current);
                    if(!current) continue;
                    if(i == 9 && settings.KeyViewerStyle == KeyviewerStyle.Key10) i = 10;
                    key.value.text = (++settings.Count[i]).ToString();
                    Total.value.text = (++settings.TotalCount).ToString();
                    UpdateCounterBannerCounterTexts();
                    PressTimes.Enqueue(elapsedMilliseconds);
                    if(settings.useRain) {
                        RawRain rawRain = new(elapsedMilliseconds, key.color);
                        key.RawRainQueue.Enqueue(rawRain);
                        key.RainList.Add(rawRain);
                    }
                    Save = true;
                }
                keyCodes = GetFootKeyCode();
                for(int i = 0; i < keyCodes.Length; i++) {
                    bool current = CheckKey(keyCodes[i]);
                    int index = i + HandOutIndex;
                    Key key = Keys[index];
                    if(!key || current == keyState[index]) continue;
                    keyState[index] = current;
                    UpdateKey(index, current);
                    if(!current) continue;
                    PressTimes.Enqueue(elapsedMilliseconds);
                    settings.Count[index]++;
                    Total.value.text = (++settings.TotalCount).ToString();
                    UpdateCounterBannerCounterTexts();
                    Save = true;
                }
                while(PressTimes.TryPeek(out long result)) {
                    if(elapsedMilliseconds - result > 1000)
                        PressTimes.TryDequeue(out long _);
                    else break;
                }
                if(lastKps == PressTimes.Count) continue;
                lastKps = PressTimes.Count;
                Kps.value.text = lastKps.ToString();
                UpdateCounterBannerCounterTexts();
                if(++repeat < 100 || !Save || !Enabled) continue;
                Main.Instance.SaveSetting();
                Save = false;
                repeat = 0;
            }
        } catch (ThreadAbortException) {
        } catch (Exception e) {
            if(KeyinputListener is not { IsAlive: true }) return;
            Main.Instance.LogException(e);
        }
    }

    private void UpdateKey(int i, bool enabled) {
        Key key = Keys[i];
        KeyViewerSettings settings = Settings;
        key.background.color = enabled ? settings.BackgroundClicked : settings.Background;
        key.outline.color = enabled ? settings.OutlineClicked : settings.Outline;
        key.text.color = enabled ? settings.TextClicked : settings.Text;
        if(key.value) key.value.color = key.text.color;
    }

    private int GetKeyViewerVerticalOffset() {
        // Keep default mode slightly lower without requiring DownLocation toggle.
        return Settings.DownLocation ? 200 : 93;
    }

    private void Initialize0KeyViewer() {
        int remove = GetKeyViewerVerticalOffset();
        for(int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 259 - remove, 50, 0);
        Keys[8] = CreateKey(8, 81 + 54, 205 - remove, 77, 1);
        Keys[9] = CreateKey(9, 81, 205 - remove, 50, 1);
        Keys[10] = CreateKey(10, 54 * 4, 205 - remove, 77, 1);
        Keys[11] = CreateKey(11, 54 * 4 + 81, 205 - remove, 50, 1);
        for(int i = 0; i < 4; i++) {
            int j = BackSequence12[i];
            Keys[j].rainParent = Keys[i + 2].rainParent;
        }
        Kps = CreateKey(-1, 0, 205 - remove, 77, -1);
        Total = CreateKey(-2, 81 + 54 * 5, 205 - remove, 77, -1);
    }

    private void Initialize1KeyViewer() {
        int remove = GetKeyViewerVerticalOffset();
        for(int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 300 - remove, 50, 0);
        for(int i = 0; i < 8; i++) {
            int j = BackSequence16[i];
            Keys[j] = CreateKey(j, 54 * i, 246 - remove, 50, 1);
            Keys[j].rainParent = Keys[i].rainParent;
        }
        Kps = CreateKey(-1, 0, 200 - remove, 212, -1, true);
        Total = CreateKey(-2, 216, 200 - remove, 212, -1, true);
    }

    private void Initialize2KeyViewer() {
        int remove = GetKeyViewerVerticalOffset();
        for(int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 313 - remove, 50, 0);
        for(int i = 0; i < 8; i++) {
            int j = BackSequence20[i];
            Keys[j] = CreateKey(j, 54 * i, 259 - remove, 50, 1);
            Keys[j].rainParent = Keys[i].rainParent;
        }
        Keys[16] = CreateKey(16, 81 + 54, 205 - remove, 77, 3);
        Keys[17] = CreateKey(17, 81, 205 - remove, 50, 3);
        Keys[18] = CreateKey(18, 54 * 4, 205 - remove, 77, 3);
        Keys[19] = CreateKey(19, 54 * 4 + 81, 205 - remove, 50, 3);
        Kps = CreateKey(-1, 0, 205 - remove, 77, -1);
        Total = CreateKey(-2, 81 + 54 * 5, 205 - remove, 77, -1);
    }

    private void Initialize3KeyViewer() {
        int remove = GetKeyViewerVerticalOffset();
        for(int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 259 - remove, 50, 0);
        Keys[8] = CreateKey(8, 81, 205 - remove, 131, 1);
        Keys[8].rainParent = Keys[3].rainParent;
        Keys[9] = CreateKey(9, 54 * 4, 205 - remove, 131, 1);
        Keys[9].rainParent = Keys[4].rainParent;
        Kps = CreateKey(-1, 0, 205 - remove, 77, -1);
        Total = CreateKey(-2, 81 + 54 * 5, 205 - remove, 77, -1);
    }

    private void InitializeFootKeyViewer(int size) {
        bool twoLine = size > 10;
        if(twoLine) size /= 2;
        int limit = size + HandOutIndex;
        for(int line = 0; line < (twoLine ? 2 : 1); line++) {
            int x = 432;
            for(int i = 20; i < 22; i++) for(int j = i; j < limit; j++) {
                Keys[j + line * size] = CreateKey(j++ + line * size, x, 15 + line * 30, 30, -1, true, false);
                x += 34;
            }
        }
    }

    private Key CreateKey(int i, float x, float y, float sizeX, int raining, bool slim = false, bool count = true) {
        GameObject gameObject = new("Key " + i);
        KeyViewerSettings settings = Settings;
        RectTransform objTransform = gameObject.AddComponent<RectTransform>();
        objTransform.SetParent(KeyViewerSizeObject.transform);
        objTransform.sizeDelta = new Vector2(sizeX, slim ? 30 : 50);
        objTransform.anchorMin = objTransform.anchorMax = Vector2.zero;
        objTransform.pivot = new Vector2(0, 0.5f);
        objTransform.anchoredPosition = new Vector2(x, y);
        objTransform.localScale = Vector3.one;
        Key key = gameObject.AddComponent<Key>();
        gameObject = new GameObject("Background");
        RectTransform transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(objTransform);
        transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = new Vector2(sizeX * 2, (slim ? 30 : 50) * 2);
        transform.localScale = new Vector3(0.5f, 0.5f);
        Image image = gameObject.AddComponent<Image>();
        image.color = settings.Background;
        image.sprite = BundleLoader.KeyBackground;
        image.type = Image.Type.Sliced;
        key.background = gameObject.AddComponent<AsyncImage>();
        gameObject = new GameObject("Outline");
        transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(objTransform);
        transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = new Vector2(sizeX * 2, (slim ? 30 : 50) * 2);
        transform.localScale = new Vector3(0.5f, 0.5f);
        image = gameObject.AddComponent<Image>();
        image.color = settings.Outline;
        image.sprite = BundleLoader.KeyOutline;
        image.type = Image.Type.Sliced;
        key.outline = gameObject.AddComponent<AsyncImage>();
        gameObject = new GameObject("KeyText");
        transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(objTransform);
        if(slim) {
            transform.sizeDelta = new Vector2(sizeX / 2, 30);
            transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0, 0.5f);
            transform.anchoredPosition = new Vector2(count ? 10 : 7.5f, 0);
        } else {
            transform.sizeDelta = new Vector2(sizeX - 4, 32);
            transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 1);
            transform.anchoredPosition = new Vector2(0, 2);
        }
        transform.localScale = Vector3.one;
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.font = BundleLoader.FontAsset;
        text.enableAutoSizing = true;
        text.fontSizeMin = 0;
        text.fontSizeMax = 20;
        text.alignment = slim && count ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
        text.color = settings.Text;
        key.text = gameObject.AddComponent<AsyncText>();
        if(count) {
            gameObject = new GameObject("CountText");
            transform = gameObject.AddComponent<RectTransform>();
            transform.SetParent(objTransform);
            if(slim) {
                transform.sizeDelta = new Vector2(sizeX / 2, 30);
                transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(1, 0.5f);
                transform.anchoredPosition = new Vector2(-10, 0);
            } else {
                transform.sizeDelta = new Vector2(sizeX - 4, 16);
                transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 0);
                transform.anchoredPosition = new Vector2(0, 2);
            }
            transform.localScale = Vector3.one;
            text = gameObject.AddComponent<TextMeshProUGUI>();
            text.font = BundleLoader.FontAsset;
            text.enableAutoSizing = true;
            text.fontSizeMin = 0;
            text.fontSizeMax = 20;
            text.alignment = slim ? TextAlignmentOptions.Right : TextAlignmentOptions.Top;
            key.value = gameObject.AddComponent<AsyncText>();
        }
        UpdateKeyText(key, i);
        key.color = raining < 2 ? raining + 1 : raining;
        if(raining != 0 && raining != 2 && raining != 3) return key;
        key.rainParent = new GameObject("RainLine");
        transform = key.rainParent.AddComponent<RectTransform>();
        transform.SetParent(objTransform);
        transform.sizeDelta = new Vector2(sizeX, 275);
        transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
        transform.anchoredPosition = new Vector2(0, raining switch {
            0 => -223,
            3 => -115,
            _ => -169
        });
        transform.localScale = Vector3.one;
        return key;
    }

    private static void UpdateKeyText(Key key, int i) {
        switch(i) {
            case -1:
                key.text.tmp.text = "KPS";
                key.value.tmp.text = "0";
                return;
            case -2:
                key.text.tmp.text = "Total";
                key.value.tmp.text = Settings.TotalCount.ToString();
                return;
            default:
                KeyCode[] keyCodes;
                KeyViewerSettings settings = Settings;
                if(i < HandOutIndex) {
                    keyCodes = GetKeyCode();
                    string[] keyText = GetKeyText();
                    key.text.tmp.text = keyText[i] ?? KeyToString(keyCodes[i]);
                    ApplySpaceKeyTextNudge(key, keyCodes[i]);
                    if(i == 9 && settings.KeyViewerStyle == KeyviewerStyle.Key10) i = 10;
                    key.value.tmp.text = settings.Count[i].ToString();
                } else {
                    keyCodes = GetFootKeyCode();
                    key.text.tmp.text = KeyToString(keyCodes[i - HandOutIndex]);
                    ApplySpaceKeyTextNudge(key, keyCodes[i - HandOutIndex]);
                }
                break;
        }
    }

    private static void ApplySpaceKeyTextNudge(Key key, KeyCode keyCode) {
        if(!key || !key.text || !key.text.tmp) return;
        TextMeshProUGUI text = key.text.tmp;
        RectTransform rectTransform = text.rectTransform;
        if(Mathf.Abs(rectTransform.pivot.x - 0.5f) > 0.01f) return;
        Vector2 anchoredPosition = rectTransform.anchoredPosition;
        anchoredPosition.x = keyCode switch {
            KeyCode.Space => 4f,
            KeyCode.LeftShift => 2.5f,
            KeyCode.RightShift => 2.5f,
            _ => 0f
        };
        rectTransform.anchoredPosition = anchoredPosition;
        text.fontSizeMax = keyCode == KeyCode.Backspace ? 16f : 20f;
    }

    #region KeyCode To Showing String

    public static string KeyToString(KeyCode keyCode) {
        string keyString = ToString(keyCode);
        if(keyString.StartsWith("Alpha")) keyString = keyString[5..];
        if(keyString.StartsWith("Keypad")) keyString = keyString[6..];
        if(keyString.StartsWith("Left")) keyString = 'L' + keyString[4..];
        if(keyString.StartsWith("Right")) keyString = 'R' + keyString[5..];
        if(keyString.EndsWith("Shift")) keyString = keyString[..^5] + "⇧";
        if(keyString.EndsWith("Control")) keyString = keyString[..^7] + "Ctrl";
        if(keyString.StartsWith("Mouse")) keyString = "M" + keyString[5..];
        return keyString switch {
            "Plus" => "+",
            "Minus" => "-",
            "Multiply" => "*",
            "Divide" => "/",
            "Enter" => "↵",
            "Equals" => "=",
            "Period" => ".",
            "Return" => "↵",
            "None" => " ",
            "Tab" => "⇥",
            "Backslash" => "\\",
            "Backspace" => "Back",
            "Slash" => "/",
            "LBracket" => "[",
            "RBracket" => "]",
            "Semicolon" => ";",
            "Comma" => ",",
            "Quote" => "'",
            "UpArrow" => "↑",
            "DownArrow" => "↓",
            "LeftArrow" => "←",
            "RightArrow" => "→",
            "Space" => "␣",
            "BackQuote" => "`",
            "PageDown" => "Pg↓",
            "PageUp" => "Pg↑",
            "CapsLock" => "⇪",
            "Insert" => "Ins",
            "Zenkakuhankaku" => "全角",
            _ => keyString
        };
    }

    #endregion

    private static void UpdateKeyLimit() {
        KeyViewerSettings settings = Settings;
        if(ADOBase.platform != Platform.Windows || !settings.AutoSetupKeyLimit || !AdofaiTweaksAPI.IsExist && !KeyboardChatterBlockerAPI.IsExist) return;
        Dictionary<KeyCode, List<int>> codeDictionary = GetKeyCodes();
        KeyCode[] keyCodes = GetKeyCode();
        KeyCode[] footKeyCodes = GetFootKeyCode();
        HashSet<KeyCode> keys = [..keyCodes.Where(t => (int) t < 0x1000)];
        foreach(KeyCode keyCode in footKeyCodes) if((int) keyCode < 0x1000) keys.Add(keyCode);
        HashSet<ushort> asyncKeys = [];
        foreach(KeyCode code in keyCodes) {
            if((int) code < 0x1000) {
                if(!codeDictionary.TryGetValue(code, out List<int> value)) continue;
                foreach(int i in value) asyncKeys.Add((ushort) i);
            } else asyncKeys.Add((ushort) ((int) code - 0x1000));
        }
        foreach(KeyCode code in footKeyCodes) {
            if((int) code < 0x1000) {
                if(!codeDictionary.TryGetValue(code, out List<int> value)) continue;
                foreach(int i in value) asyncKeys.Add((ushort) i);
            } else asyncKeys.Add((ushort) ((int) code - 0x1000));
        }
        List<KeyCode> keyList = new(keys);
        List<ushort> asyncKeyList = asyncKeys.ToList();
        if(AdofaiTweaksAPI.IsExist) AdofaiTweaksAPI.UpdateKeyLimit(keyList, asyncKeyList);
        if(KeyboardChatterBlockerAPI.IsExist) KeyboardChatterBlockerAPI.UpdateKeyLimit(keyList, asyncKeyList);
    }

    private static Dictionary<KeyCode, List<int>> GetKeyCodes() {
        JArray array = JArray.Parse(File.ReadAllText(Path.Combine(Main.Instance.Path, "KeyCodes.json")));
        Dictionary<KeyCode, List<int>> dictionary = new();
        int i = -1;
        KeyCode lastCode = KeyCode.None;
        foreach(KeyCode keyCode in Enum.GetValues(typeof(KeyCode))) {
            if(i == -1) {
                i++;
                continue;
            }
            if(keyCode > KeyCode.Mouse6) break;
            if(lastCode == keyCode) continue;
            lastCode = keyCode;
            JToken token = array[i++];
            if(token.Type == JTokenType.Array) {
                List<int> list = [];
                list.AddRange(token.Select(t => t.Value<int>()));
                dictionary.Add(keyCode, list);
            } else {
                int value = token.Value<int>();
                if(value == -1) continue;
                dictionary.Add(keyCode, [value]);
            }
        }
        return dictionary;
    }

    [JAPatch(typeof(UnityModManager.ModEntry), "Load", PatchType.Postfix, false, TryingCatch = false)]
    private static void Load() {
        AdofaiTweaksAPI.Setup();
        KeyboardChatterBlockerAPI.Setup();
        UpdateKeyLimit();
    }

    public class KeyViewerSettings : JASetting {
        public KeyviewerStyle KeyViewerStyle = KeyviewerStyle.Key16;
        public FootKeyviewerStyle FootKeyViewerStyle = FootKeyviewerStyle.Key4;
        public KeyCode[] key10 = [
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.Comma
        ];
        public string[] key10Text = new string[10];
        public KeyCode[] key12 = [
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period
        ];
        public string[] key12Text = new string[12];
        public KeyCode[] key16 = [
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period, KeyCode.CapsLock, KeyCode.LeftShift, KeyCode.Return, KeyCode.H
        ];
        public string[] key16Text = new string[16];
        public KeyCode[] key20 = [
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period, KeyCode.CapsLock, KeyCode.LeftShift, KeyCode.Return, KeyCode.H,
            KeyCode.CapsLock, KeyCode.D, KeyCode.RightShift, KeyCode.Semicolon
        ];
        public string[] key20Text = new string[20];
        public KeyCode[] footkey2 = [KeyCode.F8, KeyCode.F3];
        public KeyCode[] footkey4 = [KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2];
        public KeyCode[] footkey6 = [KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2, KeyCode.F6, KeyCode.F1];
        public KeyCode[] footkey8 = [KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1];
        public KeyCode[] footkey16 = [
            KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1,
            KeyCode.Alpha0, KeyCode.Alpha6, KeyCode.Alpha9, KeyCode.Alpha5, KeyCode.Alpha8, KeyCode.Alpha4, KeyCode.Alpha7, KeyCode.Alpha3
        ];
        public int[] Count = new int[36];
        public int TotalCount;
        public bool DownLocation;
        public bool CounterTextInsideBanners = true;
        public bool AutoSetupKeyLimit = true;
        public float Size = 1;
        public bool useRain = true;
        public float rainSpeed = 100;
        public float rainHeight = 200;
        public ColorCache Background = new(KeyViewer.Background);
        public ColorCache BackgroundClicked = new(KeyViewer.BackgroundClicked);
        public ColorCache Outline = new(KeyViewer.Outline);
        public ColorCache OutlineClicked = new(KeyViewer.OutlineClicked);
        public ColorCache Text = new(KeyViewer.Text);
        public ColorCache TextClicked = new(KeyViewer.TextClicked);
        public ColorCache RainColor = new(KeyViewer.RainColor);
        public ColorCache RainColor2 = new(KeyViewer.RainColor2);
        public ColorCache RainColor3 = new(KeyViewer.RainColor3);
        public ColorCache KeyLeftBackground = new(KeyViewer.KeyLeftBackground);
        public ColorCache KeyRightBackground = new(KeyViewer.KeyRightBackground);
        public ColorCache KeyLeftOutline = new(KeyViewer.KeyLeftOutline);
        public ColorCache KeyRightOutline = new(KeyViewer.KeyRightOutline);
        public ColorCache KeyLeftText = new(KeyViewer.KeyLeftText);
        public ColorCache KeyRightText = new(KeyViewer.KeyRightText);
        public ColorCache KeyLeftBackgroundPressed = new(KeyViewer.KeyLeftBackgroundPressed);
        public ColorCache KeyRightBackgroundPressed = new(KeyViewer.KeyRightBackgroundPressed);
        public ColorCache KeyLeftOutlinePressed = new(KeyViewer.KeyLeftOutlinePressed);
        public ColorCache KeyRightOutlinePressed = new(KeyViewer.KeyRightOutlinePressed);
        public ColorCache KeyLeftTextPressed = new(KeyViewer.KeyLeftTextPressed);
        public ColorCache KeyRightTextPressed = new(KeyViewer.KeyRightTextPressed);
        public ColorCache KeyLeftRainColor = new(KeyViewer.KeyLeftRainColor);
        public ColorCache KeyRightRainColor = new(KeyViewer.KeyRightRainColor);
        public ColorCache CounterBackgroundLeft = new(KeyViewer.CounterBackgroundLeft);
        public ColorCache CounterBackgroundRight = new(KeyViewer.CounterBackgroundRight);
        public ColorCache CounterTintLeft = new(KeyViewer.CounterTintLeft);
        public ColorCache CounterTintRight = new(KeyViewer.CounterTintRight);
        public ColorCache CounterTextLeft = new(KeyViewer.CounterTextLeft);
        public ColorCache CounterTextRight = new(KeyViewer.CounterTextRight);

        public KeyViewerSettings(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
            Settings = this;
            if(Count.Length == 24) {
                int[] cur = Count;
                Count = new int[FootOutIndex];
                for(int i = 0; i < 16; i++) Count[i] = cur[i];
                for(int i = 16; i < 24; i++) Count[i + 4] = cur[i];
            }
            if(jsonObject?["KeyLeftBackground"] == null) KeyLeftBackground = new ColorCache(KeyViewer.KeyLeftBackground);
            if(jsonObject?["KeyRightBackground"] == null) KeyRightBackground = new ColorCache(KeyViewer.KeyRightBackground);
            if(jsonObject?["KeyLeftOutline"] == null) KeyLeftOutline = new ColorCache(KeyViewer.KeyLeftOutline);
            if(jsonObject?["KeyRightOutline"] == null) KeyRightOutline = new ColorCache(KeyViewer.KeyRightOutline);
            if(jsonObject?["KeyLeftText"] == null) KeyLeftText = new ColorCache(KeyViewer.KeyLeftText);
            if(jsonObject?["KeyRightText"] == null) KeyRightText = new ColorCache(KeyViewer.KeyRightText);
            if(jsonObject?["KeyLeftBackgroundPressed"] == null) KeyLeftBackgroundPressed = new ColorCache(KeyViewer.KeyLeftBackgroundPressed);
            if(jsonObject?["KeyRightBackgroundPressed"] == null) KeyRightBackgroundPressed = new ColorCache(KeyViewer.KeyRightBackgroundPressed);
            if(jsonObject?["KeyLeftOutlinePressed"] == null) KeyLeftOutlinePressed = new ColorCache(KeyViewer.KeyLeftOutlinePressed);
            if(jsonObject?["KeyRightOutlinePressed"] == null) KeyRightOutlinePressed = new ColorCache(KeyViewer.KeyRightOutlinePressed);
            if(jsonObject?["KeyLeftTextPressed"] == null) KeyLeftTextPressed = new ColorCache(KeyViewer.KeyLeftTextPressed);
            if(jsonObject?["KeyRightTextPressed"] == null) KeyRightTextPressed = new ColorCache(KeyViewer.KeyRightTextPressed);
            if(jsonObject?["KeyLeftRainColor"] == null) KeyLeftRainColor = new ColorCache(KeyViewer.KeyLeftRainColor);
            if(jsonObject?["KeyRightRainColor"] == null) KeyRightRainColor = new ColorCache(KeyViewer.KeyRightRainColor);
            if(jsonObject?["CounterBackgroundLeft"] == null) CounterBackgroundLeft = new ColorCache(KeyViewer.CounterBackgroundLeft);
            if(jsonObject?["CounterBackgroundRight"] == null) CounterBackgroundRight = new ColorCache(KeyViewer.CounterBackgroundRight);
            if(jsonObject?["CounterTintLeft"] == null) CounterTintLeft = new ColorCache(KeyViewer.CounterTintLeft);
            if(jsonObject?["CounterTintRight"] == null) CounterTintRight = new ColorCache(KeyViewer.CounterTintRight);
            if(jsonObject?["CounterTextLeft"] == null) CounterTextLeft = new ColorCache(KeyViewer.CounterTextLeft);
            if(jsonObject?["CounterTextRight"] == null) CounterTextRight = new ColorCache(KeyViewer.CounterTextRight);
        }
    }
}
