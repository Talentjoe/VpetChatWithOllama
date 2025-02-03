using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace VpetChatWithOllama
{
    public class OllamaChatCore
    {
        private String moduleName;
        private String terminal;
        private bool supportTool;
        private bool AddTimeToPrompt;
        private HttpClient sharedClient;
        private List<Dictionary<String, String>> chatingHistory;
        private String prompt;
        private List<Func<String>> costomizedPropts;
        public long tockenCount { get; private set; }
        public long promptCount { get; private set; }

        /// <summary>
        /// Initialize the chat with ollama terminal
        /// </summary>
        /// <param name="prompt">The system prompt</param>
        /// <param name="moduleName">The module to run with</param>
        /// <param name="terminal">Ollama api address and port</param>
        /// <param name="supportTool">If use tool to suport more features</param>
        /// <param name="AddTime">Add time to the prompt</param>
        /// <param name="chatHistory">Previous chat</param>
        public OllamaChatCore(
            String prompt = "",
            String moduleName = "Qwen2.5:7b",
            String terminal = "http://localhost:11434/",
            bool supportTool = true,
            bool AddTime = true,
            List<Func<String>> costomizedPropts = null,
            String chatHistory = ""
        )
        {
            this.moduleName = moduleName.ToLower();
            this.terminal = terminal;
            this.supportTool = supportTool;
            this.chatingHistory = new List<Dictionary<String, String>>();
            this.prompt = prompt;
            this.AddTimeToPrompt = AddTime;
            this.costomizedPropts = costomizedPropts;

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

        }
        /// <summary>
        /// The structure to deserialize the response from the chat API
        /// </summary>
        struct ChatResponse
        {
            public Dictionary<String, String> message { get; set; }
            public int prompt_eval_count { get; set; }
            public int eval_count { get; set; }
        }

        /// <summary>
        /// The structure to deserialize the response from the module exist API
        /// </summary>
        struct ModuleExistResponse
        {
            public struct Module
            {
                public String name { get; set; }
                public Dictionary<String, String> details { get; set; }
            }
            public List<Module> models { get; set; }
        }
        /// <summary>
        /// Get modules from the ollama terminal
        /// </summary>
        /// <returns>The list of module name</returns>
        /// <exception cref="Exception">When not able to get the response</exception>
        public List<String> getAllModules()
        {
            try
            {
                JObject modules = GetResponse(null, "api/tags").Result;
                List<String> moduleNames = new List<String>();

                foreach (var module in modules["models"])
                {
                    moduleNames.Add(((String)module["name"]).ToLower());
                }
                return moduleNames;

            }
            catch (Exception e)
            {
                throw new Exception("Failed to get modules, Check if ollama is running.");
            }
        }
        /// <summary>
        /// adding system prompt to the chat history
        /// </summary>
        /// <returns>
        ///     a dictionary with role and content
        /// </returns>
        private Dictionary<String, String> SystemPrompt()
        {
            StringBuilder systemPrompt = new StringBuilder();
            if (AddTimeToPrompt)
            {
                systemPrompt.AppendLine("current time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            foreach (var costomizedPropt in costomizedPropts)
            {
                systemPrompt.AppendLine(costomizedPropt());
            }

            return new Dictionary<String, String>() { { "role", "system" }, { "content", systemPrompt.ToString() } };
        }
        /// <summary>
        /// give the next sentence to the chat API, with history included
        /// </summary>
        /// <param name="nextSentence"> Next sentence user inputs</param>
        /// <returns></returns>
        public async Task<String> Chat(String nextSentence)
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

        public String saveHistory()
        {
            return JsonConvert.SerializeObject(chatingHistory);
        }

        public void changeModule(String moduleName)
        {
            this.moduleName = moduleName;
        }
    }
}
