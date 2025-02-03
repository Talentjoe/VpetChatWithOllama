using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace VpetChatWithOllama
{
    class OllamaChatCore
    {

        private String moduleName;
        private String terminal;
        private bool supportTool;
        private bool AddTimeToPrompt;
        private HttpClient sharedClient;
        private List<Dictionary<String, String>> chatingHistory;
        private String prompt;
        public long tockenCount { get; private set; }
        public long promptCount { get; private set; }

        public OllamaChatCore(
            String prompt = "",
            String moduleName = "Qwen2.5:7b",
            String terminal = "http://localhost:11434/",
            bool supportTool = true,
            bool AddTime = true
        )
        {
            this.moduleName = moduleName.ToLower();
            this.terminal = terminal;
            this.supportTool = supportTool;
            this.chatingHistory = new List<Dictionary<String, String>>();
            this.prompt = prompt;
            this.AddTimeToPrompt = AddTime;

            if (prompt != "")
            {
                chatingHistory.Add(new Dictionary<String, String>() { { "role", "system" }, { "content", prompt } });
            }

            this.tockenCount = 0;
            this.promptCount = 0;

            sharedClient = new()
            {
                BaseAddress = new Uri(terminal),
            };

            //if (!CheckModuleExist().Result)
            //{
            //    throw new Exception("Module not found");
            //}
        }
        struct ChatResponse
        {
            public Dictionary<String, String> message { get; set; }
            public int prompt_eval_count { get; set; }
            public int eval_count { get; set; }
        }

        struct ModuleExistResponse
        {
            public struct Module
            {
                public String name { get; set; }
                public Dictionary<String, String> details { get; set; }
            }
            public List<Module> models { get; set; }
        }

        private async Task<bool> CheckModuleExist()
        {
            try
            {
                JObject modules = await GetResponse(null, "api/tags");

                foreach (var module in modules["models"])
                {
                    String currentName = ((String)module["name"]).ToLower();
                    if (currentName == moduleName)
                    {
                        return true;
                    }
                }
                return false;

            }
            catch (Exception e)
            {
                throw new Exception("Failed to get modules, Check if ollama is running.");
            }
        }

        private Dictionary<String, String> SystemPrompt()
        {
            StringBuilder systemPrompt = new StringBuilder();
            if (AddTimeToPrompt)
            {
                systemPrompt.AppendLine("current time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            }

            return new Dictionary<String, String>() { { "role", "system" }, { "content", systemPrompt.ToString() } };
        }

        public async Task<String> chat(String nextSentence)
        {

            chatingHistory.Add(new Dictionary<String, String>() { { "role", "user" }, { "content", nextSentence } });

            chatingHistory.Add(SystemPrompt());

            using StringContent jsonContent = new(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    model = moduleName,
                    messages = chatingHistory,
                    stream = false
                }),
                Encoding.UTF8,
                "application/json");

            //ChatResponse chatResponse = await GetResponse<ChatResponse>(jsonContent, "api/chat");
            JObject chatResponseJson = await GetResponse(jsonContent, "api/chat");

            ChatResponse chatResponse = chatResponseJson.ToObject<ChatResponse>();

            promptCount += chatResponse.prompt_eval_count;
            tockenCount += chatResponse.eval_count;

            chatingHistory.Add(new Dictionary<string, string>(chatResponse.message));

            return chatResponse.message["content"];
        }


        private async Task<JObject> GetResponse(StringContent sendingMessage, String url)
        {
            HttpResponseMessage response;

            if (sendingMessage == null)
            {
                response = await sharedClient.GetAsync(url);
            }
            else
            {
                response = await sharedClient.PostAsync(url, sendingMessage);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(jsonResponse);
            }

            if (jsonResponse == null)
            {
                return default;
            }

            JObject jobject = JObject.Parse(jsonResponse);
            //T formatedResponse = JsonConvert.DeserializeObject<T>(jsonResponse);

            return jobject;
        }
    }
}
