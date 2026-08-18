using System.Collections.Generic;
using AiCat.Data.Serialization;

namespace AiCat.Core.Services
{
    /// <summary>选猫目录：返回可领养的猫定义列表。</summary>
    public interface ICatCatalog
    {
        IReadOnlyList<CatDefinition> GetDefinitions();
    }

    /// <summary>
    /// 代码常量版选猫目录（符合「程序化、避免二进制资产入库」基调）。
    /// 数据量小（预设 ≥3 只）；未来可换 ScriptableObjectCatCatalog 由策划配置。
    /// </summary>
    public sealed class CatCatalog : ICatCatalog
    {
        public IReadOnlyList<CatDefinition> GetDefinitions()
        {
            return new[]
            {
                new CatDefinition(
                    "橘猫", "话痨",
                    new CatAppearance { coatColor = "橘色", pattern = "虎斑", eyeColor = "琥珀色" },
                    "干饭第一名的暖男话痨，一开口就停不下来。"),

                new CatDefinition(
                    "布偶", "黏人",
                    new CatAppearance { coatColor = "奶白色", pattern = "重点色", eyeColor = "蓝色", accessory = "铃铛" },
                    "温柔黏人的大布偶，走哪跟到哪。"),

                new CatDefinition(
                    "英短", "高冷",
                    new CatAppearance { coatColor = "蓝灰色", pattern = "纯色", eyeColor = "琥珀色" },
                    "傲娇高冷的英短，其实心里只有你。"),
            };
        }
    }
}
