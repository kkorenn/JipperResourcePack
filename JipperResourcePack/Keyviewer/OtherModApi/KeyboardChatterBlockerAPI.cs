using System.Collections.Generic;
using UnityEngine;

namespace JipperResourcePack.Keyviewer.OtherModApi;

public class KeyboardChatterBlockerAPI {
    public static bool IsExist;
    public static object Setting;

    public static void Setup() {
        // Optional dependency: keep disabled when the mod isn't installed.
        IsExist = false;
        Setting = null;
    }

    public static void SetSetting() {
        IsExist = false;
        Setting = null;
    }

    public static void UpdateKeyLimit(List<KeyCode> keys, List<ushort> asyncKeys) {
        // No-op on builds without KeyboardChatterBlocker dependency.
    }
}
