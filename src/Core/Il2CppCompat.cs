using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine;

namespace DynamicMapsExtended
{
    // SPT 5.0 runs EFT under IL2CPP. DynamicMaps' own components (MapView, ModdedMapScreen, ...)
    // are managed classes injected into IL2CPP, while Unity lookups return interop wrappers typed
    // as the requested base class. These helpers translate between the two so reflection keeps
    // operating on the real DynamicMaps instances.
    internal static class Il2CppCompat
    {
        // Returns the managed object behind an injected IL2CPP instance, or the wrapper itself
        // when the object is a plain Unity/game type.
        internal static object ToManaged(Il2CppObjectBase value)
        {
            if (value == null || value.Pointer == IntPtr.Zero)
                return null;

            try
            {
                return ClassInjectorBase.GetMonoObjectFromIl2CppPointer(value.Pointer) ?? value;
            }
            catch
            {
                return value;
            }
        }

        internal static object[] FindObjectsOfTypeAll(Type managedType)
        {
            if (managedType == null)
                return Array.Empty<object>();

            var found = Resources.FindObjectsOfTypeAll(Il2CppType.From(managedType));
            if (found == null)
                return Array.Empty<object>();

            var result = new List<object>(found.Length);
            foreach (var item in found)
            {
                var managed = ToManaged(item);
                if (managed != null && managedType.IsInstanceOfType(managed))
                    result.Add(managed);
            }
            return result.ToArray();
        }

        internal static object GetComponentInParent(Component component, Type managedType)
        {
            if (component == null || managedType == null)
                return null;

            var found = component.GetComponentInParent(Il2CppType.From(managedType));
            var managed = ToManaged(found);
            return managed != null && managedType.IsInstanceOfType(managed) ? managed : null;
        }
    }
}
