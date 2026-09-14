#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using K = NNN.SatoKotaObservationFactory;
using static NNN.Editor.SatoHachiPlayableSceneBuilder;

namespace NNN.Editor
{
    /// <summary>
    /// コタのGame View用Sceneを再生成するEditor専用ツール。
    /// 物語はFactory、動作と配置はPresentationDefinition、見た目はSceneの仮素材へ分離する。
    /// 再生成は保存済みSceneを上書きするため、Sceneを手で調整した場合は先に差分を確認する。
    /// </summary>
    public static class SatoKotaPlayableSceneBuilder
    {
        public const string KotaScenePath = "Assets/Scenes/SatoKotaVerticalSlice.unity";
        private const string Root = "Assets/Playable/Kota";
        [MenuItem("NNN/Playable/Open Sato Kota DAY1-11")]
        public static void OpenKota()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(KotaScenePath)) CreateKotaScene(); else EditorSceneManager.OpenScene(KotaScenePath);
        }
        [MenuItem("NNN/Playable/Play Sato Kota DAY1-11")]
        public static void PlayKota()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(KotaScenePath)) CreateKotaScene(); else EditorSceneManager.OpenScene(KotaScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game"); EditorApplication.isPlaying = true;
        }
        /// <summary>
        /// Content・Presentation・Materialを既存パスで再利用し、Sceneのオブジェクトを作り直す。
        /// コタSceneをビルド対象の先頭に置くが、既存のテストScene登録は削除しない。
        /// </summary>
        public static void CreateKotaScene()
        {
            Directory.CreateDirectory(Root); Directory.CreateDirectory("Assets/Scenes"); AssetDatabase.Refresh();
            // NewSceneは未使用アセットの解放を伴う。参照を取得する前にSceneを切り替え、
            // 取得したばかりのPresentation参照が途中で破棄されるのを避ける。
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var d = Asset<ObservationPresentationDefinition>("SatoKotaPresentation"); FillDefinition(d);
            var content = Asset<SatoKotaPlayableContent>("SatoKotaContent");
            var camera = new GameObject("Fixed Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 3.7f, -11); camera.transform.LookAt(new Vector3(0, 1, 0));
            camera.orthographic = true; camera.orthographicSize = 2.25f; camera.backgroundColor = new Color(.12f, .17f, .19f);
            var home = new GameObject("HOME"); var unusedBackground = new GameObject("Unused Background"); unusedBackground.SetActive(false);
            var wall = Mat("Wall", new Color(.85f, .85f, .77f)); var wood = Mat("Wood", new Color(.65f, .51f, .36f));
            var ink = Mat("Ink", new Color(.12f, .2f, .22f)); var white = Mat("Cream", new Color(.98f, .95f, .83f));
            var teal = Mat("Teal", new Color(.24f, .54f, .52f)); var kota = Mat("Kota", new Color(.73f, .64f, .49f)); var red = Mat("PenRed", new Color(.85f, .2f, .14f));
            Cube(home.transform, "Wall", new Vector3(0, 1.8f, 2), new Vector3(18, 5, .2f), wall);
            Cube(home.transform, "Floor", new Vector3(0, -.15f, 0), new Vector3(18, .3f, 6), wood);
            Cube(home.transform, "Door", new Vector3(-4, 1.2f, 1.8f), new Vector3(1.6f, 2.4f, .15f), ink);
            Cube(home.transform, "Window", new Vector3(3.2f, 2, 1.8f), new Vector3(2, 1.5f, .12f), teal);
            Cube(home.transform, "Desk", new Vector3(-1.2f, .95f, .35f), new Vector3(2.8f, .14f, 1.25f), wood);
            Cube(home.transform, "DeskLegL", new Vector3(-2.4f, .45f, .35f), new Vector3(.13f, .9f, .6f), ink);
            Cube(home.transform, "DeskLegR", new Vector3(0, .45f, .35f), new Vector3(.13f, .9f, .6f), ink);
            Cube(home.transform, "PC", new Vector3(-1.6f, 1.45f, .85f), new Vector3(1.1f, .75f, .12f), ink);
            Cube(home.transform, "Screen", new Vector3(-1.6f, 1.45f, .77f), new Vector3(.95f, .58f, .03f), teal);
            Cube(home.transform, "Keyboard", new Vector3(-1.5f, 1.055f, -.02f), new Vector3(.85f, .06f, .38f), ink);
            Cube(home.transform, "Bed", new Vector3(4.5f, .12f, 0), new Vector3(1.4f, .24f, .8f), teal);
            Cube(home.transform, "Shelf", new Vector3(2, .85f, .5f), new Vector3(1.3f, .13f, .8f), wood);
            Cube(home.transform, "PaperBag", new Vector3(1.7f, .2f, -.4f), new Vector3(.55f, .4f, .5f), white);
            var tower = new GameObject("Cat Tower"); tower.transform.parent = home.transform;
            Cube(tower.transform, "Post", new Vector3(3, .7f, 0), new Vector3(.22f, 1.4f, .22f), wood);
            Cube(tower.transform, "Top", new Vector3(3, 1.45f, 0), new Vector3(1.1f, .13f, .8f), white);
            var spot = new GameObject("Desk Side Cat Spot"); spot.transform.parent = home.transform;
            Cube(spot.transform, "Cushion", new Vector3(.7f, .6f, 0), new Vector3(1, .22f, .85f), teal);
            Cube(spot.transform, "Support", new Vector3(.7f, .25f, 0), new Vector3(.65f, .5f, .55f), wood);
            var toy = Shape(home.transform, "Cat Toy", PrimitiveType.Sphere, new Vector3(1, .12f, -.35f), Vector3.one * .24f, red);
            // 座標はScene生成時にMarkerへ集約する。再生コードは名前だけを参照するため、
            // 机の高さや小物の落下先を変更してもSimulatorのログや条件を編集する必要はない。
            var markers = new GameObject("Scene Markers").transform;
            Mark(markers, "Human_Default", -1.6f, 0, .9f); Mark(markers, "Human_Door", -3, 0);
            Mark(markers, "Human_OtherSide", 2, 0); Mark(markers, "Cat_Default", 1.2f, 0);
            Mark(markers, "Cat_Door", -3.8f, 0); Mark(markers, "Cat_NearHuman", -2.3f, 0);
            Mark(markers, "Cat_DeskFloor", -.5f, 0); Mark(markers, "Cat_Desk", -.35f, 1.02f);
            Mark(markers, "Cat_Tower", 3, 1.52f); Mark(markers, "Cat_Shelf", 2, .92f);
            Mark(markers, "Cat_Window", 3.8f, 1.52f); Mark(markers, "Cat_Bed", 4.5f, .24f);
            Mark(markers, "Cat_Spot", .7f, .71f); Mark(markers, "Cat_OtherSide", 1.3f, 0);
            Mark(markers, "Pen_Desk", .1f, 1.07f, -.24f); Mark(markers, "Pen_Floor", .3f, .06f, -.65f);
            Mark(markers, "Object_Desk", -2.2f, 1.15f, .1f); Mark(markers, "Object_Storage", -2.2f, .15f, .7f);
            var pen = Cube(home.transform, "Pen", markers.Find("Pen_Desk").position, new Vector3(.45f, .075f, .075f), red).gameObject.AddComponent<ObservationPropView>();
            pen.Id = "PEN"; pen.RestMarker = markers.Find("Pen_Desk");
            var fragile = Cube(home.transform, "Fragile Object", markers.Find("Object_Desk").position, new Vector3(.25f, .3f, .25f), white).gameObject.AddComponent<ObservationPropView>();
            fragile.Id = "FRAGILE"; fragile.RestMarker = markers.Find("Object_Desk");
            // 人間が収納した事実を見たあと、翌日また机へ戻る矛盾を避ける。
            // ペンは毎朝の作業用なので既定のResetEachDay=trueを使う。
            fragile.ResetEachDay = false;
            var human = Actor("HumanActor", false, teal, white, ink); var cat = Actor("CatActor", true, kota, white, ink);
            var action = new GameObject("ObservationView").AddComponent<ObservationActionPresenter>(); action.Definition = d; action.Human = human; action.Cat = cat;
            var presenter = action.gameObject.AddComponent<ObservationScenePresenter>(); presenter.Actions = action; presenter.Markers = markers;
            presenter.Home = home; presenter.ShoppingStreet = unusedBackground; presenter.Props.Add(pen); presenter.Props.Add(fragile);
            presenter.WorldProps.Add(new WorldVisibilityBinding { Flag = K.Tower, Target = tower });
            presenter.WorldProps.Add(new WorldVisibilityBinding { Flag = K.DeskSpot, Target = spot });
            presenter.WorldProps.Add(new WorldVisibilityBinding { Flag = K.DeskCleared, Target = fragile.gameObject, HideWhenSet = true });
            var controller = new GameObject("GameController").AddComponent<SatoHachiPlayableController>();
            controller.Content = content; controller.Definition = d; controller.Presentation = presenter;
            var ui = new GameObject("UI - Observation Review Report Action Result").AddComponent<SatoHachiPlayableUI>(); ui.Controller = controller; ui.WorldCamera = camera;
            EditorUtility.SetDirty(d); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, KotaScenePath);
            // 起動Sceneはコタ。既存のテストScene登録は保持する。
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(KotaScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(x => x.path != KotaScenePath)).ToArray();
            AssetDatabase.Refresh(); Debug.Log("KOTA PLAYABLE SCENE: " + KotaScenePath);
        }
        /// <summary>
        /// Actionのfallback、場面の初期配置、ログ途中の演出、情報の表示名、Guided選択を登録する。
        /// ログを追加・並べ替えした際はStepの添字も確認する。素材不足をイベント条件へ持ち込まない。
        /// </summary>
        public static void FillDefinition(ObservationPresentationDefinition d)
        {
            d.Actions.Clear(); d.Stages.Clear(); d.LogStages.Clear(); d.Information.Clear(); d.GuidedActions.Clear();
            d.CatName = "コタ"; d.HumanName = "佐藤"; d.SliceTitle = "SatoKota Vertical Slice"; d.GuidedActions.AddRange(K.GuidedActions);
            Bind(d, ActorMotion.Walk, "CAT_WALK", "CAT_RUN", "CAT_APPROACH", "CAT_ENTER_HOME", "CAT_EXPLORE");
            Bind(d, ActorMotion.Look, "CAT_LOOK", "CAT_SNIFF", "CAT_RUB", "HUMAN_LOOK");
            Bind(d, ActorMotion.Jump, "CAT_JUMP"); Bind(d, ActorMotion.Paw, "CAT_PAW", "CAT_PLAY", "HUMAN_PC", "HUMAN_PLAY");
            Bind(d, ActorMotion.Sit, "CAT_REST", "CAT_SIT"); Bind(d, ActorMotion.Eat, "CAT_EAT"); Bind(d, ActorMotion.Groom, "CAT_GROOM");
            Bind(d, ActorMotion.Crouch, "HUMAN_CROUCH"); Bind(d, ActorMotion.Place, "HUMAN_PLACE", "HUMAN_CLEAR_TABLE"); Bind(d, ActorMotion.Hold, "HUMAN_HOLD");
            Stage(d, K.Contact, "Cat_Door", "Human_Door"); Step(d, K.Contact, 1, to: "Cat_NearHuman");
            Stage(d, K.Entry, "Cat_Door", "Human_Door"); Step(d, K.Entry, 0, to: "Cat_Default"); Step(d, K.Entry, 2, to: "Cat_OtherSide");
            Stage(d, K.Living, "Cat_Bed"); Stage(d, K.DeskTrouble, "Cat_Default");
            Step(d, K.DeskTrouble, 1, to: "Cat_DeskFloor"); Step(d, K.DeskTrouble, 2, to: "Cat_Desk");
            Step(d, K.DeskTrouble, 3, at: "Cat_Desk", prop: "PEN", propTo: "Pen_Floor");
            Step(d, K.DeskTrouble, 4, at: "Cat_DeskFloor"); Step(d, K.DeskTrouble, 5, at: "Cat_Default", to: "Cat_DeskFloor");
            Stage(d, K.Night, "Cat_Default"); Step(d, K.Night, 0, to: "Cat_OtherSide"); Step(d, K.Night, 1, to: "Cat_Shelf"); Step(d, K.Night, 3, at: "Cat_Default");
            Stage(d, K.PlayResponse, "Cat_Default"); Step(d, K.PlayResponse, 2, to: "Cat_DeskFloor"); Step(d, K.PlayResponse, 4, to: "Cat_Desk");
            Stage(d, K.VerticalInterest, "Cat_Default"); Step(d, K.VerticalInterest, 0, to: "Cat_Shelf"); Step(d, K.VerticalInterest, 2, at: "Cat_Default", to: "Cat_DeskFloor");
            Stage(d, K.VerticalResponse, "Cat_Default"); Step(d, K.VerticalResponse, 0, to: "Cat_Tower"); Step(d, K.VerticalResponse, 1, at: "Cat_Tower", to: "Cat_Window");
            Step(d, K.VerticalResponse, 3, at: "Cat_DeskFloor", to: "Cat_Desk"); Step(d, K.VerticalResponse, 4, at: "Cat_Desk");
            Stage(d, K.Proximity, "Cat_Desk"); Step(d, K.Proximity, 2, human: "Human_OtherSide"); Step(d, K.Proximity, 3, at: "Cat_DeskFloor", to: "Cat_OtherSide");
            Stage(d, K.HumanAdaptation, "Cat_DeskFloor"); Step(d, K.HumanAdaptation, 0, prop: "FRAGILE", propTo: "Object_Storage"); Step(d, K.HumanAdaptation, 2, at: "Cat_Default");
            Stage(d, K.SharedSpace, "Cat_Default"); Step(d, K.SharedSpace, 1, to: "Cat_DeskFloor"); Step(d, K.SharedSpace, 2, to: "Cat_Spot");
            Step(d, K.SharedSpace, 3, at: "Cat_Spot"); Step(d, K.SharedSpace, 5, prop: "PEN", propTo: "Pen_Floor");
            Stage(d, K.DeskVisit, "Cat_DeskFloor", to: "Cat_Desk"); Stage(d, K.TowerJump, "Cat_Default", to: "Cat_Tower");
            Stage(d, "KOTA_NORMAL_NEAR_DESK", "Cat_Spot"); Stage(d, "KOTA_NORMAL_REST", "Cat_Bed");
            Stage(d, K.NightRun, "Cat_Default", to: "Cat_DeskFloor");
            string[] ids = { K.EveningKnowledge, K.PlayKnowledge, K.VerticalKnowledge, K.ExplorationKnowledge, K.ProximityKnowledge,
                K.PlayRoutine, K.VerticalRoute, K.Tower, K.DeskSpot, K.DeskCleared };
            string[] labels = { "夕方～夜に活発", "遊びへの反応が強い", "高い場所を好んで使う", "部屋を巡回する傾向", "佐藤の近くで過ごす傾向",
                "帰宅後に遊ぶ時間", "登ってよい高所動線", "キャットタワー", "机横の猫用スペース", "壊れやすい小物を収納" };
            for (int i = 0; i < ids.Length; i++) d.Information.Add(new InformationLabel { Id = ids[i], Label = labels[i] });
        }
        private static void Bind(ObservationPresentationDefinition d, ActorMotion motion, params string[] actions)
        { foreach (string id in actions) d.Actions.Add(new ActionMotionBinding { ActionId = id, Motion = motion }); }
        private static void Stage(ObservationPresentationDefinition d, string id, string cat, string human = "Human_Default", string to = null)
            => d.Stages.Add(new SceneStageBinding { EventId = id, CatMarker = cat, HumanMarker = human, CatDestination = to });
        private static void Step(ObservationPresentationDefinition d, string id, int index, string at = null, string to = null, string human = null, string prop = null, string propTo = null)
            => d.LogStages.Add(new LogStageBinding { EventId = id, LogIndex = index, CatMarker = at, CatDestination = to, HumanMarker = human, PropId = prop, PropDestination = propTo });
        // 既存アセットを更新し、Sceneから参照するGUIDを再生成のたびに変えない。
        private static T Asset<T>(string name) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Root + "/" + name + ".asset");
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, Root + "/" + name + ".asset"); }
            return asset;
        }
        private static Material Mat(string name, Color color)
        {
            string path = Root + "/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Unlit/Color")); AssetDatabase.CreateAsset(m, path); }
            m.color = color; EditorUtility.SetDirty(m); return m;
        }
        private static Transform Cube(Transform p, string name, Vector3 pos, Vector3 size, Material mat) => Shape(p, name, PrimitiveType.Cube, pos, size, mat);
        private static void Mark(Transform parent, string name, float x, float y, float z = 0)
        { var t = new GameObject(name).transform; t.parent = parent; t.position = new Vector3(x, y, z); }
    }
}
#endif
