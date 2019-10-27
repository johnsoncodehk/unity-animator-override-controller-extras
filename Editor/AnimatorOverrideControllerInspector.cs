using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace OverrideControllerToolsEditor
{

    [CanEditMultipleObjects, CustomEditor(typeof(AnimatorOverrideController))]
    public class AnimatorOverrideControllerInspector : DecoratorEditor
    {

        [MenuItem("CONTEXT/AnimatorOverrideController/Delete Unused Animations")]
        static void DeleteUnusedAnimationsOption(MenuCommand menuCommand)
        {
            AnimatorOverrideController overrideController = menuCommand.context as AnimatorOverrideController;
            List<AnimationClip> unusedClips = overrideController.GetUnusedAnimations();
            foreach (AnimationClip clip in unusedClips)
            {
                Object.DestroyImmediate(clip, true);
            }
            AssetImporter assetImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(overrideController));
            assetImporter.SaveAndReimport();
        }

        [MenuItem("CONTEXT/AnimatorOverrideController/Delete Unused Animations", true)]
        static bool DeleteUnusedAnimationsOptionValidation(MenuCommand menuCommand)
        {
            AnimatorOverrideController overrideController = menuCommand.context as AnimatorOverrideController;
            List<AnimationClip> unusedClips = overrideController.GetUnusedAnimations();
            return unusedClips.Count > 0;
        }

        [MenuItem("CONTEXT/AnimatorOverrideController/Export to AnimatorController")]
        static void ExportAnimatorControllerOption(MenuCommand menuCommand)
        {
            AnimatorOverrideController overrideController = menuCommand.context as AnimatorOverrideController;
            overrideController.ExportController();
        }

        [MenuItem("CONTEXT/AnimatorOverrideController/Export to AnimatorController", true)]
        static bool ExportAnimatorControllerOptionValidation(MenuCommand menuCommand)
        {
            AnimatorOverrideController overrideController = menuCommand.context as AnimatorOverrideController;
            return overrideController.runtimeAnimatorController;
        }

        public AnimatorOverrideControllerInspector() : base("AnimatorOverrideControllerInspector") { }

        private bool quickSetupFlagsFoldOut = true;
        private Dictionary<string, bool> quickSetupFolderFoldOuts = new Dictionary<string, bool>();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            AnimatorOverrideController overrideController = target as AnimatorOverrideController;

            if (overrideController.runtimeAnimatorController)
                return;

            if (quickSetupFlagsFoldOut = EditorGUILayout.Foldout(quickSetupFlagsFoldOut, "Quick Setup"))
            {
                EditorGUI.indentLevel++;

                Dictionary<string, List<RuntimeAnimatorController>> quickSetupControllers = new Dictionary<string, List<RuntimeAnimatorController>>();

                foreach (var settings in AnimatorOverrideControllerQuickSetupSettings.assets)
                {
                    string displayName = string.IsNullOrEmpty(settings.name) ? settings.name : settings.name;
                    foreach (var controller in settings.controllers)
                    {
                        if (controller == null)
                        {
                            continue;
                        }

                        if (!quickSetupControllers.ContainsKey(displayName))
                        {
                            quickSetupControllers[displayName] = new List<RuntimeAnimatorController>();
                        }
                        quickSetupControllers[displayName].Add(controller);
                    }
                    foreach (var kvp in quickSetupControllers)
                    {
                        string folder = kvp.Key;
                        if (!quickSetupFolderFoldOuts.ContainsKey(folder))
                        {
                            quickSetupFolderFoldOuts[folder] = true;
                        }
                        if (quickSetupFolderFoldOuts[folder] = EditorGUILayout.Foldout(quickSetupFolderFoldOuts[folder], folder))
                        {
                            EditorGUI.indentLevel++;
                            foreach (var controller in kvp.Value)
                            {
                                string controllerName = controller.name;
                                DrawButton(
                                    true,
                                    controllerName,
                                    () =>
                                    {
                                        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                                        overrideController.runtimeAnimatorController = controller;
                                        overrideController.GetOverrides(overrides);
                                        overrideController.CreateOverrideAnimations(overrides.Where(cp => cp.Value == null).Select(cp => cp.Key).ToArray(), settings);
                                    }
                                );
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawButton(bool enable, string name, System.Action onClick)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            GUI.enabled = enable;
            if (GUILayout.Button(name))
            {
                onClick();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}
