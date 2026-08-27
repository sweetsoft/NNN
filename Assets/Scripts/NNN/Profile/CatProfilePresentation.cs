using System;
using System.Collections.Generic;

namespace NNN
{
    /// <summary>CatDefinitionから生成した、UI非依存の表示用プロフィール。</summary>
    [Serializable]
    public sealed class CatProfilePresentation
    {
        public string DisplayName { get; }
        public int Age { get; }
        public IReadOnlyList<string> PlayerTags { get; }
        public string Behavior { get; }
        public string Caution { get; }

        public CatProfilePresentation(string displayName, int age, IList<string> playerTags,
            string behavior, string caution)
        {
            DisplayName = displayName ?? string.Empty;
            Age = age;
            PlayerTags = new List<string>(playerTags ?? Array.Empty<string>()).AsReadOnly();
            Behavior = behavior ?? string.Empty;
            Caution = caution;
        }
    }
}
