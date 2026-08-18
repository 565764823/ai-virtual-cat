using System;
using AiCat.Core;
using AiCat.Core.DI;
using UnityEngine;

namespace AiCat
{
    /// <summary>
    /// 程序入口（替换原 ChatBootstrap）：建 RootLifetimeScope → 解析 AppBootstrapper → 异步启动。
    /// 不用 VContainer 的 IAsyncStartable（签名是 Task，混 UniTask 易绕），改显式入口。
    /// </summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static async void Build()
        {
            try
            {
                var go = new GameObject("RootLifetimeScope");
                Object.DontDestroyOnLoad(go);
                // AddComponent 会触发 LifetimeScope.Awake → Build()，容器即就绪
                var scope = go.AddComponent<RootLifetimeScope>();

                var bootstrapper = scope.Container.Resolve<AppBootstrapper>();
                await bootstrapper.StartAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AppBootstrap] 启动失败：{e}");
            }
        }
    }
}
