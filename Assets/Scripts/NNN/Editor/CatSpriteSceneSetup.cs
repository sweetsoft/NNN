#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NNN.Editor
{
    /// <summary>確定素材だけを明示的に採用する。作成中クリップの自動検索・採用はしない。</summary>
    public static class CatSpriteSceneSetup
    {
        private const string Folder = "Assets/NNN/CatAnimation/";

        public static void RunBatch()
        {
            ApplyToScenes();
            SatoKotaPlayableVerification.RunBatch();
        }

        internal static void Attach(CharacterActorView actor)
        {
            var idle = Clip("ANIM_IDLE");
            var sit = Clip("ANIM_SIT");
            var rest = Clip("ANIM_REST");
            var walk = Clip("ANIM_WALK");
            var run = Clip("ANIM_RUN");
            var paw = Clip("ANIM_PAW");
            var rub = Clip("ANIM_RUB");
            var sleep = Clip("ANIM_SLEEP");
            var jump = Clip("ANIM_JUMP_UP");
            var jumpDown = Clip("ANIM_JUMP_DOWN");
            var drink = Clip("ANIM_DRINK");
            // 元シートのPPUと足元ピボットを保持し、Scene側のサイズだけを調整する。
            var sprite = new GameObject("Cat Sprite");
            sprite.transform.SetParent(actor.Visual, false);
            sprite.transform.localScale = Vector3.one * .36f;
            var renderer = sprite.AddComponent<SpriteRenderer>();
            var animator = sprite.AddComponent<Animator>();
            animator.runtimeAnimatorController = Controller(idle, sit, rest, walk, run, paw, rub, sleep, jump, jumpDown, drink);
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var animation = sprite.AddComponent<SpriteActorAnimation>();
            animation.Renderer = renderer;
            animation.Animator = animator;
            animation.FacingCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            animation.Idle = idle;
            animation.Actions.Add(new SpriteActorAnimation.ActionClip { ActionId = "CAT_SIT", Clip = sit });
            animation.Actions.Add(new SpriteActorAnimation.ActionClip { ActionId = "CAT_REST", Clip = rest });
            // 同じ移動表現を使うActionにも歩行素材を共有する。物語のActionId自体は変更しない。
            foreach (var id in new[] { "CAT_WALK", "CAT_APPROACH", "CAT_ENTER_HOME", "CAT_EXPLORE", "HARNESS_LOW_WALK", "CAT_FOLLOW" })
                animation.Actions.Add(new SpriteActorAnimation.ActionClip { ActionId = id, Clip = walk });
            animation.Actions.Add(new SpriteActorAnimation.ActionClip { ActionId = "CAT_RUN", Clip = run });
            animation.Actions.Add(new SpriteActorAnimation.ActionClip { ActionId = "CAT_PAW", Clip = paw });
            animation.Actions.Add(new SpriteActorAnimation.ActionClip { ActionId = "CAT_PLAY", Clip = paw });
            animation.Actions.Add(new SpriteActorAnimation.ActionClip { ActionId = "CAT_RUB", Clip = rub });
            // 睡眠イベント追加時に利用できるよう登録。休憩を睡眠へ読み替えることはしない。
            animation.Actions.Add(new SpriteActorAnimation.ActionClip { ActionId = "CAT_SLEEP", Clip = sleep });
            animation.Actions.Add(new SpriteActorAnimation.ActionClip { ActionId = "CAT_JUMP", Clip = jump, Loop = false });
            animation.Actions.Add(new SpriteActorAnimation.ActionClip { ActionId = "CAT_JUMP_DOWN", Clip = jumpDown, Loop = false });
            animation.Actions.Add(new SpriteActorAnimation.ActionClip { ActionId = "CAT_DRINK", Clip = drink });
            actor.SpriteAnimation = animation;
            var curve = AnimationUtility.GetObjectReferenceCurveBindings(idle)[0];
            renderer.sprite = (Sprite)AnimationUtility.GetObjectReferenceCurve(idle, curve)[0].value;
            if (animation.FacingCamera != null) sprite.transform.rotation = animation.FacingCamera.transform.rotation;
        }

        private static AnimatorController Controller(params AnimationClip[] clips)
        {
            const string path = "Assets/Playable/ConfirmedCat.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var clip in clips)
            {
                var state = machine.states.FirstOrDefault(x => x.state.name == clip.name).state;
                if (state == null) state = machine.AddState(clip.name);
                state.motion = clip;
                if (clip == clips[0]) machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + name + ".anim");
            if (clip == null) throw new InvalidOperationException("Missing confirmed clip: " + name);
            var curves = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            if (AnimationUtility.GetCurveBindings(clip).Length != 0 || curves.Length != 1 ||
                curves[0].type != typeof(SpriteRenderer) || curves[0].path != "" || curves[0].propertyName != "m_Sprite" ||
                AnimationUtility.GetObjectReferenceCurve(clip, curves[0]).Any(x => !(x.value is Sprite)))
                throw new InvalidOperationException("Expected root SpriteRenderer frames only: " + name);
            return clip;
        }

        [MenuItem("NNN/Playable/Apply Confirmed Cat Sprites")]
        public static void ApplyToScenes()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before updating scenes.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (var path in new[] { SatoHachiPlayableSceneBuilder.ScenePath, SatoKotaPlayableSceneBuilder.KotaScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var actor = UnityEngine.Object.FindFirstObjectByType<ObservationActionPresenter>().Cat;
                // 猫のVisualだけ差し替え、Sceneの配置・UI・進行設定は保持する。
                foreach (Transform child in actor.Visual.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                actor.Head = null; actor.Gesture = null; actor.SpriteAnimation = null;
                Attach(actor);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CAT SPRITES: applied all 11 confirmed clips, including DRINK, to both playable scenes.");
        }
    }
}
#endif
