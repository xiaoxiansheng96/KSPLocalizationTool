using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace KSPLocalizationTool.Services
{
    /// <summary>
    /// 翻译服务，用于对接豆包AI接口进行翻译
    /// </summary>
    public class TranslationService
    {
        private readonly LogService _logService;
        private readonly HttpClient _httpClient;

        // 注意：这里需要替换为实际的API密钥和端点
        private const string API_ENDPOINT = "https://api.doubao.com/translate";
        private const string API_KEY = "YOUR_API_KEY";

        public TranslationService(LogService logService)
        {
            _logService = logService;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
        }

        /// <summary>
        /// 翻译文本
        /// </summary>
        public string Translate(string text, string sourceLang, string targetLang)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            try
            {
                // 调用API进行翻译（同步）
                return TranslateAsync(text, sourceLang, targetLang).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"翻译失败: {ex.Message}");
                return text; // 翻译失败时返回原文
            }
        }

        /// <summary>
        /// 异步翻译文本
        /// </summary>
        public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang)
        {
            try
            {
                // 构建请求数据
                var requestData = new
                {
                    text = text,
                    source = sourceLang,
                    target = targetLang
                };

                string jsonData = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // 发送请求
                var response = await _httpClient.PostAsync(API_ENDPOINT, content);
                response.EnsureSuccessStatusCode();

                // 处理响应
                // 修改反序列化代码，使用 ApiTranslationResponse 类
                string responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<ApiTranslationResponse>(responseContent);

                if (responseData != null && !string.IsNullOrEmpty(responseData.TranslatedText))
                {
                    return responseData.TranslatedText;
                }
                else
                {
                    _logService.LogMessage("翻译API返回空结果");
                    return text;
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"翻译API调用失败: {ex.Message}");
                return text;
            }
        }
        // 翻译请求模型
        private class TranslationRequest
        {
            [JsonPropertyName("prompt")]
            public string Prompt { get; set; }
        }

        // 删除或注释掉这个类
        // 翻译响应模型
        // private class TranslationResponse
        // {
        //     [JsonPropertyName("result")]
        //     public string Result { get; set; }
        // }

        // 修改外部的 TranslationResponse 类名称
        /// <summary>
        /// 翻译API响应模型
        /// </summary>
        public class ApiTranslationResponse
        {
            [JsonPropertyName("translated_text")]
            public string TranslatedText { get; set; } = string.Empty;
        }
    }
}
// 移除文件末尾多余的右大括号
    