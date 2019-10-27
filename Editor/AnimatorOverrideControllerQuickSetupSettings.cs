using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OverrideControllerToolsEditor
{
    [CreateAssetMenu]
    public class AnimatorOverrideControllerQuickSetupSettings : ScriptableObject
    {

        public static IEnumerable<AnimatorOverrideControllerQuickSetupSettings> assets =>
            AssetDatabase.FindAssets("t:" + typeof(AnimatorOverrideControllerQuickSetupSettings))
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Select(path => AssetDatabase.LoadAssetAtPath<AnimatorOverrideControllerQuickSetupSettings>(path));

        [System.Serializable]
        public struct StringReplace
        {
            public string oldValue, newValue;
        }

        public new string name;
        public RuntimeAnimatorController[] controllers = new RuntimeAnimatorController[0];
        public StringReplace[] animationNameStringReplacePairs = new StringReplace[0];

    }
}
