using AiCat.Data.Serialization;

namespace AiCat.Core.Services
{
    /// <summary>
    /// 选猫定义（不入库，仅选猫页展示）。
    /// Breed / Personality 用中文标签（受 schema CHECK 约束），Appearance 序列化进 cats.appearance_json。
    /// </summary>
    public sealed class CatDefinition
    {
        /// <summary>品种，如「橘猫」。</summary>
        public string Breed { get; }

        /// <summary>性格，如「话痨」。</summary>
        public string Personality { get; }

        /// <summary>外观（毛色/花纹/眼睛/配饰）。</summary>
        public CatAppearance Appearance { get; }

        /// <summary>选猫页展示文案。</summary>
        public string Description { get; }

        public CatDefinition(string breed, string personality, CatAppearance appearance, string description)
        {
            Breed = breed;
            Personality = personality;
            Appearance = appearance;
            Description = description;
        }
    }
}
