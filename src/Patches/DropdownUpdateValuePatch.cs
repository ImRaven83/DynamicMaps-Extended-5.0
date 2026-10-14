using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace DynamicMapsExtended
{
    // Workaround for DynamicMaps 2.0.0 on SPT 5.0. MapSelectDropdown.OnLoadMap calls the game's
    // BaseDropDownBox.UpdateValue(index, true, null, null, true). Under IL2CPP the two int?
    // parameters are Il2CppSystem.Nullable<int> objects, so passing null throws inside the
    // interop call. The exception aborts ModdedMapScreen.ChangeMap right after the map is drawn,
    // which skips the floor slider and every marker provider's OnMapChanged.
    // Make the same call with empty Nullable values. If anything about the call differs, the
    // original DynamicMaps code runs unchanged.
    [HarmonyPatch]
    internal static class DropdownUpdateValuePatch
    {
        private static FieldInfo _dropdownField;
        private static FieldInfo _selectableField;
        private static MethodInfo _updateValue;
        private static bool _logged;

        private static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("DynamicMaps.UI.Controls.MapSelectDropdown")
                ?? throw new MissingMemberException("DynamicMaps.UI.Controls.MapSelectDropdown not found");
            var mapDef = AccessTools.TypeByName("DynamicMaps.Data.MapDef")
                ?? throw new MissingMemberException("DynamicMaps.Data.MapDef not found");
            return AccessTools.Method(type, "OnLoadMap", new[] { mapDef })
                ?? throw new MissingMethodException(type.FullName, "OnLoadMap");
        }

        private static bool Prefix(object __instance, object __0)
        {
            try
            {
                var type = __instance.GetType();
                _dropdownField ??= AccessTools.Field(type, "_dropdown");
                _selectableField ??= AccessTools.Field(type, "_selectableMapDefs");

                var dropdown = _dropdownField?.GetValue(__instance);
                if (dropdown == null || !(_selectableField?.GetValue(__instance) is IList selectable))
                    return true;

                _updateValue ??= FindUpdateValue(dropdown.GetType());
                if (_updateValue == null)
                    return true;

                _updateValue.Invoke(dropdown, new object[]
                {
                    selectable.IndexOf(__0), true,
                    new Il2CppSystem.Nullable<int>(), new Il2CppSystem.Nullable<int>(), true
                });

                if (!_logged)
                {
                    _logged = true;
                    Plugin.Debug("Applied DynamicMaps map-dropdown UpdateValue workaround (IL2CPP nullable arguments).");
                }
                return false;
            }
            catch (Exception e)
            {
                Plugin.Debug("Map-dropdown UpdateValue workaround failed; using DynamicMaps' original call: " + e.Message);
                return true;
            }
        }

        // BaseDropDownBox.UpdateValue(int, bool, int?, int?, bool); AccessTools searches base types.
        private static MethodInfo FindUpdateValue(Type dropdownType)
        {
            return AccessTools.Method(dropdownType, "UpdateValue", new[]
            {
                typeof(int), typeof(bool), typeof(Il2CppSystem.Nullable<int>), typeof(Il2CppSystem.Nullable<int>), typeof(bool)
            });
        }
    }
}
