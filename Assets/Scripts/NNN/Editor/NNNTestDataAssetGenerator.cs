#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NNN.Editor
{
    /// <summary>Factoryの一時オブジェクトを編集可能な.asset群として保存する。</summary>
    [InitializeOnLoad]
    public static class NNNTestDataAssetGenerator
    {
        /// <summary>自動生成する全テストアセットの基準フォルダ。</summary>
        private const string Root = "Assets/Resources/NNNTestData";
        /// <summary>実行時にResources.Loadするプロファイルの固定保存先。</summary>
        private const string ProfilePath = Root + "/Tanaka_TestProfile.asset";

        /// <summary>スクリプト読込完了後に不足データの生成を予約する。</summary>
        static NNNTestDataAssetGenerator()
        {
            EditorApplication.delayCall += EnsureGenerated;
        }

        /// <summary>プロファイルが存在しない場合だけ初期データを生成する。</summary>
        [MenuItem("NNN/Test Data/Generate If Missing")]
        public static void EnsureGenerated()
        {
            // ドメインリロード直後は、ファイルが存在していても型情報の復元前で
            // LoadAssetAtPath<T>が一時的にnullを返すことがある。パスの存在を先に確認し、
            // その瞬間を「未生成」と誤判定して連番アセットを作らないようにする。
            if (AssetDatabase.AssetPathExists(ProfilePath))
            {
                var existing = AssetDatabase.LoadAssetAtPath<CaseGenerationProfile>(ProfilePath);
                if (existing == null)
                {
                    Debug.LogWarning("NNN test profile exists but is not loadable yet. Automatic generation was skipped: " + ProfilePath);
                    return;
                }

                if (existing.HumanBenchmarks == null || existing.HumanBenchmarks.Count != 10)
                    Debug.LogWarning("NNN test profile is incomplete. Use 'NNN/Test Data/Rebuild All' explicitly after reviewing references.");
                return;
            }

            Generate();
        }

        /// <summary>生成済みデータを削除し、コード上の初期値から作り直す。</summary>
        [MenuItem("NNN/Test Data/Rebuild All")]
        public static void Rebuild()
        {
            if (AssetDatabase.IsValidFolder(Root) && !AssetDatabase.DeleteAsset(Root))
            {
                Debug.LogError("NNN test data rebuild aborted because the existing folder could not be deleted: " + Root);
                return;
            }
            Generate();
        }

        /// <summary>フォルダ、参照先、最後にプロファイルの順で永続化する。</summary>
        private static void Generate()
        {
            // 既存Profileがある状態での追記生成は禁止する。
            // 以前はGenerateUniqueAssetPathにより失敗が見えず、" 9"や" 10"が増殖していた。
            if (AssetDatabase.AssetPathExists(ProfilePath))
            {
                Debug.LogError("NNN test data generation aborted because the profile already exists: " + ProfilePath);
                return;
            }

            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "NNNTestData");
            EnsureFolder(Root, "Traits");
            EnsureFolder(Root, "Cats");
            EnsureFolder(Root, "Case");
            EnsureFolder(Root, "Benchmarks");

            var profile = NNNTestDataFactory.CreateRuntimeProfile();
            var saved = new HashSet<Object>();

            foreach (var cat in profile.Cats)
                foreach (var trait in cat.Traits)
                    SaveOnce(trait, Root + "/Traits/" + trait.name + ".asset", saved);
            foreach (var human in profile.HumanArchetypes)
                foreach (var trait in human.PreferredTraits)
                    SaveOnce(trait, Root + "/Traits/" + trait.name + ".asset", saved);
            foreach (var benchmark in profile.HumanBenchmarks)
                foreach (var trait in benchmark.Archetype.PreferredTraits)
                    SaveOnce(trait, Root + "/Traits/" + trait.name + ".asset", saved);

            foreach (var cat in profile.Cats) SaveOnce(cat, Root + "/Cats/" + cat.name + ".asset", saved);
            foreach (var human in profile.HumanArchetypes) SaveOnce(human, Root + "/Case/" + human.name + ".asset", saved);
            foreach (var lifestyle in profile.Lifestyles) SaveOnce(lifestyle, Root + "/Case/" + lifestyle.name + ".asset", saved);
            foreach (var residence in profile.Residences) SaveOnce(residence, Root + "/Case/" + residence.name + ".asset", saved);
            foreach (var problem in profile.Problems) SaveOnce(problem, Root + "/Case/" + problem.name + ".asset", saved);
            foreach (var benchmark in profile.HumanBenchmarks)
            {
                SaveOnce(benchmark.Archetype, Root + "/Benchmarks/" + benchmark.Archetype.name + ".asset", saved);
                SaveOnce(benchmark.Lifestyle, Root + "/Benchmarks/" + benchmark.Lifestyle.name + ".asset", saved);
                SaveOnce(benchmark.Residence, Root + "/Benchmarks/" + benchmark.Residence.name + ".asset", saved);
                SaveOnce(benchmark, Root + "/Benchmarks/" + benchmark.name + ".asset", saved);
            }
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("NNN test data generated at " + Root);
        }

        /// <summary>AssetDatabase上に指定の子フォルダがなければ作成する。</summary>
        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        /// <summary>同じ一時オブジェクトを重複保存せず、一度だけ.asset化する。</summary>
        private static void SaveOnce(Object item, string path, HashSet<Object> saved)
        {
            if (item == null || !saved.Add(item)) return;
            if (AssetDatabase.AssetPathExists(path))
            {
                Debug.LogError("NNN test data generation found an unexpected existing asset and was stopped for this item: " + path);
                return;
            }
            AssetDatabase.CreateAsset(item, path);
        }
    }
}
#endif
