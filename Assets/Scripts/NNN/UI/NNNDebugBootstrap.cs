using UnityEngine;

namespace NNN
{
    /// <summary>任意のシーンでPlayした直後にデバッグUIを自動設置する。</summary>
    public static class NNNDebugBootstrap
    {
        /// <summary>既存UIとの二重生成を避け、シーン切替後も残るUIを作る。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            if (Object.FindObjectOfType<NNNDebugUI>() != null) return;
            var root = new GameObject("NNN Debug UI");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<NNNDebugUI>();
            root.AddComponent<ObservationDebugRunner>();
        }
    }
}
