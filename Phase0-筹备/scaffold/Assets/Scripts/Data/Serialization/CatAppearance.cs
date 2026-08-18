using System;

namespace AiCat.Data.Serialization
{
    /// <summary>
    /// 猫咪外观 DTO：序列化进 cats.appearance_json（JsonUtility 要求 [Serializable]）。
    /// Phase 1 仅选猫展示用；可视化猫咪（Live2D）后置 P3。
    /// </summary>
    [Serializable]
    public sealed class CatAppearance
    {
        /// <summary>毛色，如「橘色」「蓝灰色」。</summary>
        public string coatColor;

        /// <summary>花纹，如「纯色」「虎斑」「重点色」。</summary>
        public string pattern;

        /// <summary>眼睛颜色，如「琥珀色」「蓝色」。</summary>
        public string eyeColor;

        /// <summary>配饰（铃铛/围巾/帽子），可空。</summary>
        public string accessory;
    }
}
