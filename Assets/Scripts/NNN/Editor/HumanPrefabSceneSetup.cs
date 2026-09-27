#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NNN.Editor
{
    /// <summary>ユーザーのモデルPrefabを保持し、SceneのHuman Visualだけを交換する。</summary>
    public static class HumanPrefabSceneSetup
    {
        private const string PrefabPath = "Assets/NNN/Human/HumanActorProt.prefab";
        private static readonly string[] States = {
            "HUMAN_IDLE_STAND", "HUMAN_IDLE_SIT", "HUMAN_WALK", "HUMAN_CROUCH", "HUMAN_PET",
            "HUMAN_PC", "HUMAN_PHONE", "HUMAN_PICKUP", "HUMAN_DRINK", "HUMAN_SLEEP" };

        private static GameObject LoadPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var stepped = prefab != null ? prefab.GetComponentInChildren<SteppedAnimator>(true) : null;
            var controller = stepped != null && stepped.MasterAnimator != null
                ? stepped.MasterAnimator.runtimeAnimatorController as AnimatorController : null;
            if (controller == null || States.Any(id => !controller.layers[0].stateMachine.states.Any(x => x.state.name == id)))
                throw new InvalidOperationException("Human prefab requires MasterAnimator and all ten HUMAN states.");
            return prefab;
        }

        internal static void Attach(CharacterActorView actor)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab(), actor.Visual);
            var model = instance.AddComponent<ModelActorAnimation>();
            model.Stepped = instance.GetComponentInChildren<SteppedAnimator>(true);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(model.DefaultEuler);
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            {
                // Stage PointがActorの位置を管理する。素材のRoot Motionで二重移動させない。
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            }
            foreach (var id in new[] { "HUMAN_PLACE", "HUMAN_CLEAR_TABLE", "HUMAN_HOLD" })
                model.Actions.Add(new ModelActorAnimation.ActionState { ActionId = id, State = "HUMAN_PICKUP" });
            model.Actions.Add(new ModelActorAnimation.ActionState { ActionId = "HUMAN_PLAY", State = "HUMAN_PET" });
            // 未提供ActionはModelActorAnimationの立ちIdleへフォールバックする。
            actor.ModelAnimation = model;
            actor.Head = null; actor.Gesture = null; actor.SpriteAnimation = null;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            EnsureLight();
        }

        private static void EnsureLight()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Light>() != null) return;
            var light = new GameObject("Human Model Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1;
            light.transform.rotation = Quaternion.Euler(40, -30, 0);
        }

        [MenuItem("NNN/Playable/Apply Human Prefab")]
        public static void ApplyToScenes()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            LoadPrefab(); // 既存Visualを消す前に参照とステートを検証する。
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (var path in new[] { SatoHachiPlayableSceneBuilder.ScenePath, SatoKotaPlayableSceneBuilder.KotaScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var actor = UnityEngine.Object.FindFirstObjectByType<ObservationActionPresenter>().Human;
                foreach (Transform child in actor.Visual.Cast<Transform>().ToArray())
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                Attach(actor);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("HUMAN PREFAB: applied to Hachi and Kota scenes.");
        }

        public static void Verify(CharacterActorView actor)
        {
            var model = actor.ModelAnimation;
            if (model == null || model.MasterAnimator == null) throw new Exception("Missing human model/MasterAnimator");
            foreach (var state in States)
            {
                model.Play(state);
                foreach (var part in model.Stepped.GetComponentsInChildren<Animator>())
                    if (!part.GetCurrentAnimatorStateInfo(0).IsName(state) || part.applyRootMotion)
                        throw new Exception("Human part state/root motion mismatch: " + state);
            }
            model.Play("UNKNOWN_ACTION");
            if (model.CurrentState != "HUMAN_IDLE_STAND") throw new Exception("Human fallback failed");
            actor.ResetPose();
            Debug.Log("HUMAN ANIMATION: PASS / ten states, all parts synchronized, root motion disabled, fallback/reset");
        }
        public static void RunBatch()
        {
            ApplyToScenes();
            SatoKotaPlayableVerification.RunBatch();
        }
    }
}
#endif
