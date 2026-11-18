using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class SetupMenuAnimations
{
    [MenuItem("Tools/Setup Menu Animations")]
    public static void ExecuteSetup()
    {
        ConfigureAnimationFBX("Assets/Assets/Animations/Male Sitting Pose.fbx", "Male Sitting Pose", true);
        ConfigureAnimationFBX("Assets/Assets/Animations/Sit To Stand.fbx", "Sit To Stand", false);

        SetupPlayerAnimatorController();

        try
        {
            SetupMainMenuUI.ExecuteSetup();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SetupMenuAnimations] Error running SetupMainMenuUI: " + ex);
        }
    }

    private static void ConfigureAnimationFBX(string path, string clipName, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("Importer not found at: " + path);
            return;
        }

        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips != null && clips.Length > 0)
        {
            ModelImporterClipAnimation clip = clips[0];
            clip.name = clipName;
            clip.loopTime = loop;
            if (loop)
            {
                clip.loopPose = true;
            }
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = true;
            clip.keepOriginalPositionXZ = true;

            importer.clipAnimations = new ModelImporterClipAnimation[] { clip };
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
            Debug.Log($"[SetupMenuAnimations] Successfully re-imported {path} as Humanoid (Loop: {loop}).");
        }
    }

    private static void SetupPlayerAnimatorController()
    {
        string controllerPath = "Assets/Assets/Animations/PlayerAnimations/PlayerAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogWarning("PlayerAnimator.controller not found at: " + controllerPath);
            return;
        }

        // Find animation clips
        AnimationClip sittingClip = FindClipInAsset("Assets/Assets/Animations/Male Sitting Pose.fbx");
        AnimationClip sitToStandClip = FindClipInAsset("Assets/Assets/Animations/Sit To Stand.fbx");

        if (sittingClip == null || sitToStandClip == null)
        {
            Debug.LogWarning($"Clips not yet loaded: sittingClip={sittingClip != null}, sitToStandClip={sitToStandClip != null}. Re-run after import.");
            return;
        }

        AnimatorControllerLayer baseLayer = controller.layers[0];
        AnimatorStateMachine sm = baseLayer.stateMachine;

        // Check or add parameters
        bool hasSitToStandTrigger = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == "SitToStand") hasSitToStandTrigger = true;
        }

        if (!hasSitToStandTrigger)
        {
            controller.AddParameter("SitToStand", AnimatorControllerParameterType.Trigger);
        }

        // Check if SittingPose and SitToStand states exist
        AnimatorState sittingState = null;
        AnimatorState sitToStandState = null;
        AnimatorState sprintState = null;

        foreach (var childState in sm.states)
        {
            if (childState.state.name == "SittingPose" || childState.state.name == "Sitting")
                sittingState = childState.state;
            else if (childState.state.name == "SitToStand")
                sitToStandState = childState.state;
            else if (childState.state.name == "Sprint")
                sprintState = childState.state;
        }

        if (sittingState == null)
        {
            sittingState = sm.AddState("SittingPose", new Vector3(0, -90, 0));
        }
        sittingState.motion = sittingClip;

        if (sitToStandState == null)
        {
            sitToStandState = sm.AddState("SitToStand", new Vector3(0, -30, 0));
        }
        sitToStandState.motion = sitToStandClip;

        // Set SittingPose as default state for the main menu!
        sm.defaultState = sittingState;

        // Transition from SittingPose to SitToStand when SitToStand trigger fires OR GameStarted is true
        bool hasTransitionToStand = false;
        bool hasGameStartedTransition = false;
        foreach (var tr in sittingState.transitions)
        {
            if (tr.destinationState == sitToStandState)
            {
                hasTransitionToStand = true;
                foreach (var cond in tr.conditions)
                {
                    if (cond.parameter == "GameStarted") hasGameStartedTransition = true;
                }
            }
        }

        if (!hasTransitionToStand)
        {
            var tr = sittingState.AddTransition(sitToStandState);
            tr.hasExitTime = false;
            tr.duration = 0.15f;
            tr.AddCondition(AnimatorConditionMode.If, 0, "SitToStand");
        }

        if (!hasGameStartedTransition)
        {
            var tr = sittingState.AddTransition(sitToStandState);
            tr.hasExitTime = false;
            tr.duration = 0.15f;
            tr.AddCondition(AnimatorConditionMode.If, 0, "GameStarted");
        }

        // Transition from SitToStand to Sprint when SitToStand animation completes
        if (sprintState != null)
        {
            bool hasTransitionToSprint = false;
            foreach (var tr in sitToStandState.transitions)
            {
                if (tr.destinationState == sprintState)
                {
                    hasTransitionToSprint = true;
                    break;
                }
            }

            if (!hasTransitionToSprint)
            {
                var tr = sitToStandState.AddTransition(sprintState);
                tr.hasExitTime = true;
                tr.exitTime = 0.88f; // Seamless blend into sprint towards end of standing up
                tr.duration = 0.2f;
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[SetupMenuAnimations] PlayerAnimator.controller successfully configured with SittingPose -> SitToStand -> Sprint!");
    }

    private static AnimationClip FindClipInAsset(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (assets == null) return null;

        foreach (var obj in assets)
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }
}
