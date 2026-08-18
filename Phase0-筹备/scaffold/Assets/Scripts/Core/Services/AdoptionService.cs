using System;
using System.Linq;
using AiCat.Data.Models;
using AiCat.Data.Repositories;
using AiCat.Data.Serialization;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AiCat.Core.Services
{
    /// <summary>领养领域服务（选猫 → 命名 → 落库）。</summary>
    public interface IAdoptionService
    {
        /// <summary>查当前已领养的猫（is_adopted=1），无则 null。</summary>
        UniTask<Cat> GetAdoptedCatAsync(long userId);

        /// <summary>领养：校验命名 → 新建 Cat 并落库 → 返回带 Id 的猫。</summary>
        UniTask<Cat> AdoptAsync(long userId, CatDefinition definition, string rawName);
    }

    public sealed class AdoptionService : IAdoptionService
    {
        private readonly ICatRepository _cats;

        public AdoptionService(ICatRepository cats)
        {
            _cats = cats;
        }

        public UniTask<Cat> GetAdoptedCatAsync(long userId)
        {
            return _cats.GetAdoptedCatAsync(userId);
        }

        public async UniTask<Cat> AdoptAsync(long userId, CatDefinition definition, string rawName)
        {
            // ① 命名格式校验
            var (valid, error) = CatNameValidator.Validate(rawName);
            if (!valid)
                throw new ArgumentException(error, nameof(rawName));

            string name = CatNameValidator.Trim(rawName);

            // ② 同 user 下不重名
            var existing = await _cats.GetCatsByUserAsync(userId);
            if (existing.Any(c => string.Equals(c.Name, name, StringComparison.Ordinal)))
                throw new ArgumentException("已经有同名的猫啦，换个名字吧", nameof(rawName));

            // ③ 新建猫并落库（GrowthStage/Level/三态用模型的新生默认值）
            var now = TimeUtil.NowString();
            var cat = new Cat
            {
                UserId = userId,
                Name = name,
                Breed = definition.Breed,
                Personality = definition.Personality,
                AppearanceJson = JsonUtility.ToJson(definition.Appearance),
                IsAdopted = true,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _cats.AdoptAsync(cat);
            return cat;
        }
    }
}
