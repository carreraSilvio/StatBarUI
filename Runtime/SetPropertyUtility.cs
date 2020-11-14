using System;
using UnityEngine;

namespace Visage.Runtime
{
    /// <summary>
    /// Original code from namespace UnityEngine.UI.SetPropertyUtility
    /// </summary>
    internal static class SetPropertyUtility
    {
        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (currentValue.Equals((object)newValue))
                return false;
            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((object)currentValue == null && (object)newValue == null || (object)currentValue != null && currentValue.Equals((object)newValue))
                return false;
            currentValue = newValue;
            return true;
        }
    }
    
}