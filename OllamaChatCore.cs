using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VPet_Simulator.Windows.Interface;


namespace VpetChatWithOllama
{
    public class OllamaChatCore
    {
        private String moduleName;
        private String terminal;
        private bool supportTool;
        private bool enhancePrompt;
        private HttpClient sharedClient;
        private List<Dictionary<String, String>> chatingHistory;
        private String prompt;
        private List<Func<String>> costomizedPropts;
        public long tockenCount { get; private set; }
        public long promptCount { get; private set; }
        private Dictionary<String, Func<String>> replacementMapping;

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
            bool enhancePrompt = true,
            List<Func<String>> costomizedPropts = null,
            String chatHistory = ""
        )
        {
            this.moduleName = moduleName.ToLower();
            this.terminal = terminal;
            this.supportTool = supportTool;
            this.chatingHistory = new List<Dictionary<String, String>>();
            this.prompt = prompt;
            this.enhancePrompt = enhancePrompt;
            this.costomizedPropts = costomizedPropts;

            if (chatHistory != "")
            {
                chatingHistory = JsonConvert.DeserializeObject<List<Dictionary<String, String>>>(chatHistory);
            }

            this.tockenCount = 0;
            this.promptCount = 0;

            sharedClient = new()
            {
                BaseAddress = new Uri(terminal),
            };

        }

        public OllamaChatCore(PluginInformations.PluginSettings settings,Dictionary<String,Func<String>> rp = default)
        {
            this.replacementMapping = rp;
            this.moduleName = settings.moduleName;
            this.terminal = settings.url;
            this.supportTool = false;
            this.chatingHistory = new List<Dictionary<String, String>>();
            this.prompt = settings.prompt;
            this.enhancePrompt = settings.enhancePrompt;
            this.tockenCount = 0;
            this.promptCount = 0;
            this.costomizedPropts = new List<Func<String>>();

            sharedClient = new()
            {
                BaseAddress = new Uri(terminal),
            };

            if (settings.chatHistory != "")
            {
                chatingHistory = JsonConvert.DeserializeObject<List<Dictionary<String, String>>>(settings.chatHistory);
            }
        }

        /// <summary>
        /// Get modules from the ollama terminal
        /// </summary>
        /// <returns>The list of module name</returns>
        /// <exception cref="Exception">When not able to get the response</exception>
        public async Task<List<String>> getAllModules()
        {
            try
            {
                JObject modules = await GetResponse(null, "api/tags");
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
        /// give the next sentence to the chat API, with history included
        /// </summary>
        /// <param name="nextSentence"> Next sentence user inputs</param>
        /// <returns></returns>
        public async Task<String> Chat(String nextSentence)
        {
            StringContent stringContent = new(GenerateContent(nextSentence, false), Encoding.UTF8, "application/json");

            JObject chatResponseJson = await GetResponse(stringContent, "api/chat");

            ChatResponse chatResponse = chatResponseJson.ToObject<ChatResponse>();

            return DealWithChatResponse(chatResponse);
        }


        /// <summary>
        /// give the next sentence to the chat API, with history included, and stream the response
        /// </summary>
        /// <param name="nextSentence"> Next sentence user inputs</param>
        /// <returns></returns>
        public async Task<String> ChatWithStream(String nextSentence, Action<string> updateTrigger)
        {

            StringContent postRequests = new(
                    GenerateContent(nextSentence, true),
                    Encoding.UTF8,
                    "application/json");

            //Console.WriteLine(postRequests.ReadAsStringAsync().Result);

            ChatResponse chatResponse = await StreamResponse(postRequests, "api/chat", updateTrigger);

            return DealWithChatResponse(chatResponse);
        }

        /// <summary>
        /// add the next sentence to the chat history and add other prompt to the chat history,
        /// return the string content ready to be sent to server
        /// </summary>
        /// <param name="nextSentence">the next sentce ready to chat</param>
        /// <returns>the content ready to sent to the server</returns>
        private String GenerateContent(string nextSentence, bool ifStream)
        {
            chatingHistory.Add(new Dictionary<String, String>() { { "role", "user" }, { "content", nextSentence } });

            List<Dictionary<String, String>> tempChat = new (SystemPrompt());
            tempChat.InsertRange(0, chatingHistory);

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                model = moduleName,
                messages = tempChat,
                stream = ifStream
            });
        }


        /// <summary>
        /// adding system prompt to the chat history
        /// </summary>
        /// <returns>
        ///     a dictionary with role and content
        /// </returns>
        private List<Dictionary<String, String>> SystemPrompt()
        {

            List<Dictionary<String, String>> systemPrompt = new();
            string tempPrompt = prompt;
            if (prompt != "")
            {
                if (replacementMapping != null && enhancePrompt)
                {
                    foreach (var replacement in replacementMapping)
                    {
                        tempPrompt = Regex.Replace(tempPrompt, replacement.Key, replacement.Value());
                    }
                }

                systemPrompt.Add(new() { { "role", "system" }, { "content", tempPrompt } });
            }
            if (costomizedPropts != null)
                foreach (var costomizedPropt in costomizedPropts)
                {
                    systemPrompt.Add(new() { { "role", "system" }, { "content", costomizedPropt() } });
                }

            return systemPrompt;
        }

        /// <summary>
        /// input chatResponse and return the content while adding prompt and tocken count
        /// </summary>
        /// <param name="chatResponse">the response returned from the chat</param>
        /// <returns>the content ai generate</returns>
        private String DealWithChatResponse(ChatResponse chatResponse)
        {
            promptCount += chatResponse.prompt_eval_count;
            tockenCount += chatResponse.eval_count;

            chatingHistory.Add(new Dictionary<string, string>(chatResponse.message));

            return chatResponse.message["content"];
        }

        /// <summary>
        /// the implimation of the stream response
        /// </summary>
        /// <param name="sendingMessage">the input from user</param>
        /// <param name="url">the url to post the message</param>
        /// <param name="updateTrigger">the trigger of updating chat content</param>
        /// <returns>Chat Response which include the response form the llm and the prompt usage</returns>
        private async Task<ChatResponse> StreamResponse(StringContent sendingMessage, String url, Action<string> updateTrigger)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = sendingMessage
            };

            HttpResponseMessage response = null;
            Stream responseStream = null;
            StreamReader reader = null;
            try
            {
                response = await sharedClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                responseStream = await response.Content.ReadAsStreamAsync();
                reader = new StreamReader(responseStream);

                StringBuilder chatResponse = new();
                int prompt_eval_count = 0;
                int eval_count = 0;

                while (!reader.EndOfStream)
                {
                    string line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    using (JsonDocument doc = JsonDocument.Parse(line))
                    {
                        string content = doc?.RootElement.GetProperty("message").GetProperty("content").GetString();
                        updateTrigger.Invoke(content);
                        chatResponse.Append(content?.Replace("\n", "\r\n"));

                        if (doc.RootElement.GetProperty("done").GetBoolean() == true)
                        {
                            prompt_eval_count = doc.RootElement.GetProperty("prompt_eval_count").GetInt32();
                            eval_count = doc.RootElement.GetProperty("eval_count").GetInt32();

                            return new ChatResponse()
                            {
                                message = new Dictionary<string, string>() {
                                    {"role", doc?.RootElement.GetProperty("message").GetProperty("role").GetString() },
                                    { "content", chatResponse.ToString() }
                                },
                                prompt_eval_count = prompt_eval_count,
                                eval_count = eval_count
                            };

                        }


                    }
                }

            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("请求超时或被取消");
            }
            return default;

        }

        public String saveHistory()
        {
            return JsonConvert.SerializeObject(chatingHistory);
        }

        public void changeModule(String moduleName)
        {
            this.moduleName = moduleName;
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
        ///     get post or get response from the ollama terminal
        /// </summary>
        /// <param name="sendingMessage"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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

            return jobject;
        }

    }

}
