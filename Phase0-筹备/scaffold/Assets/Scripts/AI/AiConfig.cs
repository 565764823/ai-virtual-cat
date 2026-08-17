using System;

namespace AiCat.AI
{
    /// <summary>
    /// 本地 AI 引擎连接与采样参数。
    /// MVP 用代码默认值（开箱即跑）；未来可序列化成 ScriptableObject 资产供策划/玩家调节。
    /// </summary>
    [Serializable]
    public sealed class AiConfig
    {
        /// <summary>llama-server 的 OpenAI 兼容接口根地址（本地，无 SSL）。</summary>
        public string baseUrl = "http://127.0.0.1:8080";

        /// <summary>采样温度，越高越发散。</summary>
        public float temperature = 0.8f;

        /// <summary>核采样 top_p。</summary>
        public float topP = 0.9f;

        /// <summary>重复惩罚，抑制复读。</summary>
        public float repeatPenalty = 1.15f;

        /// <summary>单次回复最大 token 数。</summary>
        public int maxTokens = 256;

        /// <summary>StreamingAssets 下的人设模板文件名。</summary>
        public string personaFile = "persona_prompt.md";
    }
}
