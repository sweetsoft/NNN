#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using H = NNN.SatoHachiObservationFactory;

namespace NNN.Editor
{
    public static class SatoHachiPlayableSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/SatoHachiVerticalSlice.unity";
        private const string Root = "Assets/Playable";
        [MenuItem("NNN/Playable/Legacy Test/Open Sato Hachi DAY1-11")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) CreateScene(); else EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("NNN/Playable/Legacy Test/Play Sato Hachi DAY1-11")]
        public static void OpenAndPlay()
        {
            Open(); EditorApplication.ExecuteMenuItem("Window/General/Game"); EditorApplication.isPlaying = true;
        }
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory(Root + "/Materials"); AssetDatabase.Refresh();
            var definition = AssetDatabase.LoadAssetAtPath<ObservationPresentationDefinition>(Root + "/SatoHachiPresentation.asset");
            if (definition == null) { definition = ScriptableObject.CreateInstance<ObservationPresentationDefinition>(); AssetDatabase.CreateAsset(definition, Root + "/SatoHachiPresentation.asset"); }
            FillDefinition(definition); EditorUtility.SetDirty(definition);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Fixed Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 3.7f, -11); camera.transform.LookAt(new Vector3(0, 1, 0));
            camera.orthographic = true; camera.orthographicSize = 2.25f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .17f, .19f);
            var background = new GameObject("Background");
            var home = new GameObject("HOME"); home.transform.parent = background.transform;
            var street = new GameObject("SHOPPING_STREET"); street.transform.parent = background.transform;
            var wall = Material("Wall", new Color(.83f, .84f, .76f)); var floor = Material("Floor", new Color(.66f, .56f, .43f));
            var dark = Material("Ink", new Color(.14f, .22f, .24f)); var white = Material("White", new Color(.97f, .94f, .82f));
            var teal = Material("Teal", new Color(.25f, .52f, .51f)); var orange = Material("Hachi", new Color(.87f, .57f, .22f));
            Shape(home.transform, "Wall", PrimitiveType.Cube, new Vector3(0, 1.8f, 2), new Vector3(18, 5, .2f), wall);
            Shape(home.transform, "Floor", PrimitiveType.Cube, new Vector3(0, -.15f, 0), new Vector3(18, .3f, 6), floor);
            Shape(home.transform, "Door", PrimitiveType.Cube, new Vector3(-4, 1.2f, 1.75f), new Vector3(1.8f, 2.4f, .2f), dark);
            Shape(home.transform, "Window", PrimitiveType.Cube, new Vector3(3.5f, 2, 1.75f), new Vector3(2.7f, 1.5f, .2f), teal);
            Shape(home.transform, "WindowBar", PrimitiveType.Cube, new Vector3(3.5f, 2, 1.5f), new Vector3(.06f, 1.5f, .1f), white);
            Shape(home.transform, "WindowSill", PrimitiveType.Cube, new Vector3(3.5f, 1.15f, 0), new Vector3(1.6f, .1f, .8f), white);
            Shape(home.transform, "Bed", PrimitiveType.Cube, new Vector3(4.8f, .12f, 0), new Vector3(1.5f, .24f, .9f), teal);
            Shape(home.transform, "TowerPost", PrimitiveType.Cube, new Vector3(2.8f, .7f, 0), new Vector3(.22f, 1.4f, .22f), floor);
            Shape(home.transform, "TowerTop", PrimitiveType.Cube, new Vector3(2.8f, 1.5f, 0), new Vector3(1.2f, .13f, .8f), white);
            Shape(home.transform, "Carrier", PrimitiveType.Cube, new Vector3(-.4f, .28f, .7f), new Vector3(.9f, .56f, .7f), dark);
            Shape(home.transform, "CarrierDoor", PrimitiveType.Cube, new Vector3(-.4f, .28f, .28f), new Vector3(.65f, .4f, .05f), teal);
            Shape(street.transform, "Street", PrimitiveType.Cube, new Vector3(0, -.15f, 0), new Vector3(18, .3f, 6), Material("Pavement", new Color(.53f, .59f, .57f)));
            Shape(street.transform, "ShopLeft", PrimitiveType.Cube, new Vector3(-2, 1.7f, 2), new Vector3(9, 3.4f, .4f), wall);
            Shape(street.transform, "ShopRight", PrimitiveType.Cube, new Vector3(6, 1.7f, 2), new Vector3(4, 3.4f, .4f), floor);
            Shape(street.transform, "CatOnlyAlley", PrimitiveType.Cube, new Vector3(3.3f, 1.4f, 2.2f), new Vector3(1.1f, 2.8f, .4f), dark);
            Shape(street.transform, "Awning", PrimitiveType.Cube, new Vector3(-2, 2.7f, 1.3f), new Vector3(8, .28f, 1.4f), teal);
            Shape(street.transform, "ShopDoor", PrimitiveType.Cube, new Vector3(-3, 1, 1.6f), new Vector3(1.7f, 2, .15f), dark);
            street.SetActive(false);
            var markers = new GameObject("Scene Markers").transform;
            Marker(markers, "Human_Default", -1.8f, 0); Marker(markers, "Human_Door", -3, 0); Marker(markers, "Human_Sofa", -2, 0); Marker(markers, "Human_Table", -.8f, 0);
            Marker(markers, "Cat_Default", 1, 0); Marker(markers, "Cat_Door", -3.8f, 0); Marker(markers, "Cat_Window", 3.5f, 1.2f); Marker(markers, "Cat_Bed", 4.8f, .25f); Marker(markers, "Cat_Tower", 2.8f, 1.6f);
            Marker(markers, "Cat_NearHumanDoor", -2.15f, 0);
            Marker(markers, "Cat_Carrier", -.4f, .2f); Marker(markers, "Human_Alley", 1.7f, 0); Marker(markers, "Cat_Alley", 3.1f, 0);
            var human = Actor("HumanActor", false, teal, white, dark); var cat = Actor("CatActor", true, orange, white, dark);
            var view = new GameObject("ObservationView"); var action = view.AddComponent<ObservationActionPresenter>();
            action.Definition = definition; action.Human = human; action.Cat = cat;
            var presenter = view.AddComponent<ObservationScenePresenter>(); presenter.Actions = action; presenter.Markers = markers; presenter.Home = home; presenter.ShoppingStreet = street;
            foreach (string prop in new[] { "TowerPost", "TowerTop" }) presenter.WorldProps.Add(new WorldVisibilityBinding { Flag = H.Tower, Target = home.transform.Find(prop).gameObject });
            foreach (string prop in new[] { "Carrier", "CarrierDoor" }) presenter.WorldProps.Add(new WorldVisibilityBinding { Flag = H.Carrier, Target = home.transform.Find(prop).gameObject });
            var controller = new GameObject("GameController").AddComponent<SatoHachiPlayableController>(); controller.Definition = definition; controller.Presentation = presenter;
            var ui = new GameObject("UI - Day Caption CatReport Action Result NextDay").AddComponent<SatoHachiPlayableUI>(); ui.Controller = controller; ui.WorldCamera = camera;
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.Refresh();
            Debug.Log("Playable Scene created: " + ScenePath);
        }
        private static void FillDefinition(ObservationPresentationDefinition d)
        {
            d.Actions.Clear(); d.Stages.Clear(); d.Information.Clear(); d.GuidedActions.Clear();
            string[] ids = { "CAT_WALK", "CAT_APPROACH", "CAT_LOOK", "CAT_GROOM", "CAT_REST", "CAT_EAT", "CAT_MEOW", "CAT_PAW", "CAT_JUMP", "HARNESS_FREEZE", "HARNESS_LOW_WALK", "HUMAN_LOOK", "HUMAN_CROUCH", "HUMAN_PLACE", "HUMAN_DOOR", "HUMAN_HOLD", "HUMAN_PHONE", "HUMAN_STOP" };
            ActorMotion[] motions = { ActorMotion.Walk, ActorMotion.Walk, ActorMotion.Look, ActorMotion.Groom, ActorMotion.Sit, ActorMotion.Eat, ActorMotion.Meow, ActorMotion.Paw, ActorMotion.Jump, ActorMotion.Idle, ActorMotion.Walk, ActorMotion.Look, ActorMotion.Crouch, ActorMotion.Place, ActorMotion.Place, ActorMotion.Hold, ActorMotion.Phone, ActorMotion.Idle };
            for (int i = 0; i < ids.Length; i++) d.Actions.Add(new ActionMotionBinding { ActionId = ids[i], Motion = motions[i] });
            Stage(d, H.FirstContact, "", "HOME", "Human_Door", "Cat_Default", "Cat_NearHumanDoor");
            Stage(d, H.EnterHome, "", "HOME", "Human_Door", "Cat_Door", "Cat_Default");
            Stage(d, H.Cohabitation, "", "HOME", "Human_Default", "Cat_Bed");
            Stage(d, H.OutsideRequest, "", "HOME", "Human_Door", "Cat_Door");
            Stage(d, "NORMAL_OUTSIDE_REQUEST", "", "HOME", "Human_Door", "Cat_Door");
            Stage(d, H.TowerResponse, "", "HOME", "Human_Default", "Cat_Default", "Cat_Tower");
            Stage(d, "NORMAL_USE_TOWER", "", "HOME", "Human_Default", "Cat_Default", "Cat_Tower");
            Stage(d, "NORMAL_VERTICAL_PATROL", "", "HOME", "Human_Default", "Cat_Tower", "Cat_Window");
            Stage(d, "NORMAL_LOOK_WINDOW", "", "HOME", "Human_Default", "Cat_Window");
            Stage(d, "NORMAL_HOME_REST", "", "HOME", "Human_Default", "Cat_Bed");
            Stage(d, H.SafetySearch, "", "HOME", "Human_Table", "Cat_Default");
            Stage(d, H.FirstHarness, "", "HOME", "Human_Default", "Cat_Default", "Cat_Bed");
            Stage(d, H.HarnessSteps, "", "HOME", "Human_Default", "Cat_Default", "Cat_Window");
            Stage(d, H.ShortTripObservation, "DEPARTURE", "HOME", "Human_Table", "Cat_Default", "Cat_Carrier");
            Stage(d, H.ShortTripObservation, "ARRIVAL", "SHOPPING_STREET", "Human_Default", "Cat_Carrier", "Cat_Default");
            Stage(d, H.ShortTripObservation, "MISMATCH", "SHOPPING_STREET", "Human_Alley", "Cat_Default", "Cat_Alley");
            Stage(d, H.ShortTripObservation, "RETURN", "HOME", "Human_Default", "Cat_Bed");
            string[] tags = { H.WantsOutside, H.IndoorOpportunity, H.FamiliarStreets, H.HarnessUncertain, H.Walkable, H.BusyRoad, H.CatOnlyPaths, H.TripPartiallyWorks, H.RouteMismatch };
            string[] labels = { "休んでも外へ行きたがる", "室内の高低差が少ない", "商店街に慣れた道がある", "ハーネスへの適応はまだ不明", "商店街は約600mの徒歩圏", "経路に大通りがある", "猫だけが通れる細道がある", "キャリーと現地ハーネスで再訪できる", "人間が付いていけない経路がある" };
            for (int i = 0; i < tags.Length; i++) d.Information.Add(new InformationLabel { Id = tags[i], Label = labels[i] });
            d.GuidedActions.AddRange(new[] { NNNActionCatalog.Skip, NNNActionCatalog.Skip, NNNActionCatalog.Skip, H.InvestigateIndoor, H.InstallTower, H.InvestigatePast, H.PrepareGear, H.InvestigateHarness, H.InvestigateRoute, H.ShortTrip, NNNActionCatalog.Skip });
        }
        private static void Stage(ObservationPresentationDefinition d, string e, string s, string bg, string human, string cat, string to = null)
            => d.Stages.Add(new SceneStageBinding { EventId = e, SceneId = s, BackgroundId = bg, HumanMarker = human, CatMarker = cat, CatDestination = to });
        private static void Marker(Transform parent, string name, float x, float y)
        { var t = new GameObject(name).transform; t.parent = parent; t.position = new Vector3(x, y, 0); }
        private static Material Material(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Unlit/Color")); AssetDatabase.CreateAsset(m, path); }
            m.color = color; EditorUtility.SetDirty(m); return m;
        }
        internal static Transform Shape(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            var g = GameObject.CreatePrimitive(primitive); g.name = name; g.transform.SetParent(parent, false); g.transform.localPosition = position; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = material; var collider = g.GetComponent<Collider>(); if (collider != null) UnityEngine.Object.DestroyImmediate(collider); return g.transform;
        }
        internal static CharacterActorView Actor(string name, bool cat, Material color, Material light, Material dark)
        {
            var root = new GameObject(name); var actor = root.AddComponent<CharacterActorView>();
            actor.Visual = new GameObject("Replaceable Visual").transform; actor.Visual.SetParent(root.transform, false);
            Shape(actor.Visual, "Body", cat ? PrimitiveType.Sphere : PrimitiveType.Capsule, new Vector3(0, cat ? .36f : .85f, 0), cat ? new Vector3(.9f, .55f, .5f) : new Vector3(.65f, .65f, .5f), color);
            actor.Head = Shape(actor.Visual, "Head", PrimitiveType.Sphere, new Vector3(0, cat ? .69f : 1.65f, -.08f), Vector3.one * (cat ? .56f : .48f), cat ? color : light);
            Shape(actor.Head, "EyeL", PrimitiveType.Sphere, new Vector3(-.22f, .08f, -.43f), new Vector3(.14f, .2f, .1f), dark);
            Shape(actor.Head, "EyeR", PrimitiveType.Sphere, new Vector3(.22f, .08f, -.43f), new Vector3(.14f, .2f, .1f), dark);
            if (cat)
            {
                Shape(actor.Head, "EarL", PrimitiveType.Cube, new Vector3(-.3f, .43f, 0), new Vector3(.23f, .4f, .25f), color).localRotation = Quaternion.Euler(0, 0, -20);
                Shape(actor.Head, "EarR", PrimitiveType.Cube, new Vector3(.3f, .43f, 0), new Vector3(.23f, .4f, .25f), color).localRotation = Quaternion.Euler(0, 0, 20);
                Shape(actor.Visual, "Tail", PrimitiveType.Capsule, new Vector3(.53f, .46f, .1f), new Vector3(.12f, .3f, .12f), color).localRotation = Quaternion.Euler(0, 0, -30);
            }
            actor.Gesture = Shape(actor.Visual, cat ? "Paw" : "Hand", PrimitiveType.Sphere, new Vector3(.28f, cat ? .12f : .8f, -.28f), Vector3.one * .2f, light);
            return actor;
        }
    }
}
#endif
