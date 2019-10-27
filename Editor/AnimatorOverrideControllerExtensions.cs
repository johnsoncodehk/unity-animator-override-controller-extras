using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace OverrideControllerToolsEditor
{

    public static class AnimatorOverrideControllerExtensions
    {

        public static IEnumerable<T> LoadChildAssets<T>(this Object obj) where T : Object
        {
            return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj))
                .Where(asset => asset is T)
                .Select(asset => asset as T);
        }

        public static List<AnimationClip> GetUnusedAnimations(this AnimatorOverrideController controller)
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            var includeClips = LoadChildAssets<AnimationClip>(controller);

            foreach (AnimationClip includeClip in includeClips)
            {
                if (!controller.animationClips.Contains(includeClip))
                {
                    clips.Add(includeClip);
                }
            }

            return clips;
        }

        public static void ExportController(this AnimatorOverrideController overrideController)
        {
            string controllerPath = AssetDatabase.GetAssetPath(overrideController.runtimeAnimatorController);
            string path = EditorUtility.SaveFilePanelInProject(
                "Export to AnimatorController",
                overrideController.name + ".controller",
                "controller",
                "",
                AssetDatabase.GetAssetPath(overrideController));

            if (string.IsNullOrEmpty(controllerPath) || string.IsNullOrEmpty(path))
                return;

            if (AssetDatabase.CopyAsset(controllerPath, path))
            {
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                AnimatorController animator = controller as AnimatorController;
                var states = animator.LoadChildAssets<AnimatorState>();
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                overrideController.GetOverrides(overrides);
                foreach (var clipPair in overrides)
                {
                    if (clipPair.Value == null)
                    {
                        continue;
                    }
                    AnimationClip overrideClip = new AnimationClip();
                    EditorUtility.CopySerialized(clipPair.Value, overrideClip);
                    overrideClip.hideFlags = HideFlags.None;
                    AssetDatabase.AddObjectToAsset(overrideClip, controller);
                    foreach (AnimatorState state in states)
                    {
                        if (state.motion == null)
                        {
                            continue;
                        }
                        bool isSame = false;
                        if (AssetDatabase.GetAssetPath(state.motion) == AssetDatabase.GetAssetPath(state))
                        {
                            isSame = state.motion.GetFileId() == clipPair.Key.GetFileId();
                        }
                        else
                        {
                            isSame = state.motion == clipPair.Key;
                        }
                        if (isSame)
                        {
                            state.motion = overrideClip;
                        }
                    }
                }
                var clips = animator.LoadChildAssets<AnimationClip>();
                states = animator.LoadChildAssets<AnimatorState>();
                foreach (AnimationClip clip in clips)
                {
                    bool isFound = false;
                    foreach (AnimatorState state in states)
                    {
                        if (state.motion == clip)
                        {
                            isFound = true;
                            break;
                        }
                    }
                    if (!isFound)
                    {
                        Object.DestroyImmediate(clip, true);
                    }
                }
            }

            AssetImporter assetImporter = AssetImporter.GetAtPath(path);
            assetImporter.SaveAndReimport();
        }

        public static void CreateOverrideAnimations(this AnimatorOverrideController overrideController, AnimationClip[] originalClips, AnimatorOverrideControllerQuickSetupSettings settings)
        {
            foreach (AnimationClip clip in originalClips)
            {
                string overrideClipName = clip.name;
                foreach (var strReplace in settings.animationNameStringReplacePairs)
                {
                    overrideClipName = overrideClipName.Replace(strReplace.oldValue, strReplace.newValue);
                }
                AnimationClip overrideClip = new AnimationClip();
                EditorUtility.CopySerialized(clip, overrideClip);
                overrideClip.name = overrideClipName;
                AssetDatabase.AddObjectToAsset(overrideClip, overrideController);
                overrideController[clip] = overrideClip;
            }

            AssetImporter assetImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(overrideController));
            assetImporter.SaveAndReimport();
        }

        private static long GetFileId(this Object obj)
        {
            SerializedObject serializedObject = new SerializedObject(obj);
            PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);
            SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");
            return localIdProp.longValue;
        }
    }
}
