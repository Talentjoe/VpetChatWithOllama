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
        private string _moduleName;
        private string _terminal;
        private bool _supportTool;
        private readonly bool _enhancePrompt;
        private readonly HttpClient _sharedClient;
        private readonly List<Dictionary<string, string>> _chattingHistory;
        private readonly string _prompt;
        private readonly List<Func<string>> _customizedPrompts;
        public long TokenCount { get; set; }
        public long PromptCount { get; set; }
        private readonly Dictionary<string, Func<string>> _replacementMapping;
        private readonly string _promptBeforeUserInput;

        /// <summary>
        /// Initialize the chat with ollama terminal
        /// </summary>
        /// <param name="prompt">The system prompt</param>
        /// <param name="moduleName">The module to run with</param>
        /// <param name="terminal">Ollama api address and port</param>
        /// <param name="supportTool">If use tool to support more features</param>
        /// <param name="chatHistory">Previous chat</param>
        /// <param name="enhancePrompt">Use enhance prompt mode</param>
        /// <param name="customizedPrompts">Customized prompts input</param>
        /// <param name="promptBeforeUserInput">The prompt before user input</param>
        /// <param name="replacementMapping">The words to be replaced by auto mapping </param>
        public OllamaChatCore(
            string prompt = "",
            string moduleName = "Qwen2.5:7b",
            string terminal = "http://localhost:11434/",
            bool supportTool = true,
            bool enhancePrompt = true,
            List<Func<string>> customizedPrompts = null,
            string chatHistory = "",
            string promptBeforeUserInput = "",
            Dictionary<string, Func<string>> replacementMapping = null
        )
        {
            this._moduleName = moduleName.ToLower();
            this._terminal = terminal;
            this._supportTool = supportTool;
            this._chattingHistory = new List<Dictionary<string, string>>();
            this._prompt = prompt;
            this._enhancePrompt = enhancePrompt;
            this._customizedPrompts = customizedPrompts;
            this._promptBeforeUserInput = promptBeforeUserInput;
            this._replacementMapping = replacementMapping;

            _chattingHistory = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(chatHistory)??new List<Dictionary<string, string>>();

            this.TokenCount = 0;
            this.PromptCount = 0;

            _sharedClient = new()
            {
                BaseAddress = new Uri(terminal),
            };
        }

        public OllamaChatCore(PluginInformations.PluginSettings settings, Dictionary<string, Func<string>> rp = default)
        {
            this._replacementMapping = rp;
            this._moduleName = settings.moduleName;
            this._terminal = settings.url;
            this._supportTool = false;
            this._chattingHistory = new List<Dictionary<string, string>>();
            this._prompt = settings.prompt;
            this._enhancePrompt = settings.enhancePrompt;
            this.TokenCount = settings.tokenCount;
            this.PromptCount = 0;
            this._customizedPrompts = new List<Func<string>>();
            this._promptBeforeUserInput = settings.promptBeforeUserInput;

            _sharedClient = new()
            {
                BaseAddress = new Uri(_terminal),
            };

            if (settings.chatHistory != "")
            {
                _chattingHistory =
                    JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(settings.chatHistory) ?? 
                        new List<Dictionary<string, string>>();
            }
        }

        /// <summary>
        /// Get modules from the ollama terminal
        /// </summary>
        /// <returns>The list of module name</returns>
        /// <exception cref="Exception">When not able to get the response</exception>
        public async Task<List<string>> getAllModules()
        {
            try
            {
                JObject modules = await GetResponse(null, "api/tags");
                List<string> moduleNames = new List<string>();

                foreach (var module in modules["models"])
                {
                    moduleNames.Add(((string)module["name"]).ToLower());
                }

                return moduleNames;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to get modules, Check if ollama is running.");
            }
        }

        /// <summary>
        /// Get modules from the ollama terminal
        /// </summary>
        /// <returns>The list of module name</returns>
        /// <exception cref="Exception">When not able to get the response</exception>
        public static async Task<List<string>> getAllModules(string url)
        {
            try
            {
                HttpClient client = new()
                {
                    BaseAddress = new Uri(url),
                };
                HttpResponseMessage response = await client.GetAsync("api/tags");
                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(jsonResponse);
                }

                if (jsonResponse == null)
                {
                    return default;
                }

                JObject modules = JObject.Parse(jsonResponse);
                List<string> moduleNames = new List<string>();

                foreach (var module in modules["models"])
                {
                    moduleNames.Add(((string)module["name"]).ToLower());
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
        public async Task<string> Chat(string nextSentence)
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
        public async Task<string> Chat(string nextSentence, Action<string> updateTrigger)
        {
            StringContent postRequests = new(
                GenerateContent(nextSentence, true),
                Encoding.UTF8,
                "application/json");

            ChatResponse chatResponse = await StreamResponse(postRequests, "api/chat", updateTrigger);

            return DealWithChatResponse(chatResponse);
        }

        /// <summary>
        /// add the next sentence to the chat history and add other prompt to the chat history,
        /// return the string content ready to be sent to server
        /// </summary>
        /// <param name="nextSentence">the next sentce ready to chat</param>
        /// <param name="ifStream">If use stream mode</param>
        /// <returns>the content ready to sent to the server</returns>
        private string GenerateContent(string nextSentence, bool ifStream)
        {
            _chattingHistory.Add(new Dictionary<string, string>()
                { { "role", "user" }, { "content", getAccuralPrompt(_promptBeforeUserInput) + nextSentence } });

            List<Dictionary<string, string>> tempChat = new(SystemPrompt());
            tempChat.InsertRange(0, _chattingHistory);

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                model = _moduleName,
                messages = tempChat,
                stream = ifStream
            });
        }

        private string getAccuralPrompt(string s)
        {
            if (_enhancePrompt)
            {
                foreach (var replacement in _replacementMapping)
                {
                    s = Regex.Replace(s, replacement.Key, replacement.Value());
                }
            }

            return s;
        }

        /// <summary>
        /// adding system prompt to the chat history
        /// </summary>
        /// <returns>
        ///     a dictionary with role and content
        /// </returns>
        private List<Dictionary<string, string>> SystemPrompt()
        {
            List<Dictionary<string, string>> systemPrompt = new();
            var tempPrompt = _prompt;
            if (_prompt != "")
            {
                systemPrompt.Add(new() { { "role", "system" }, { "content", getAccuralPrompt(tempPrompt) } });
            }

            if (!_customizedPrompts.Any())
                return systemPrompt;
            
            foreach (var customizedPrompt in _customizedPrompts)
            {
                systemPrompt.Add(new() { { "role", "system" }, { "content", customizedPrompt() } });
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
            PromptCount += chatResponse.prompt_eval_count;
            TokenCount += chatResponse.eval_count;

            _chattingHistory.Add(new Dictionary<string, string>(chatResponse.message));

            return chatResponse.message["content"];
        }

        /// <summary>
        /// the implimation of the stream response
        /// </summary>
        /// <param name="sendingMessage">the input from user</param>
        /// <param name="url">the url to post the message</param>
        /// <param name="updateTrigger">the trigger of updating chat content</param>
        /// <returns>Chat Response which include the response form the llm and the prompt usage</returns>
        private async Task<ChatResponse> StreamResponse(StringContent sendingMessage, string url,
            Action<string> updateTrigger)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = sendingMessage
            };

            try
            {
                var response = await _sharedClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead) ;
                var responseStream = await response.Content.ReadAsStreamAsync();
                var reader = new StreamReader(responseStream);

                StringBuilder chatResponse = new();
                var promptEvalCount = 0;
                var evalCount = 0;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
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
                            promptEvalCount = doc.RootElement.GetProperty("prompt_eval_count").GetInt32();
                            evalCount = doc.RootElement.GetProperty("eval_count").GetInt32();

                            return new ChatResponse()
                            {
                                message = new Dictionary<string, string>()
                                {
                                    { "role", doc?.RootElement.GetProperty("message").GetProperty("role").GetString() },
                                    { "content", chatResponse.ToString() }
                                },
                                prompt_eval_count = promptEvalCount,
                                eval_count = evalCount
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
            return JsonConvert.SerializeObject(_chattingHistory);
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
                response = await _sharedClient.GetAsync(url);
            }
            else
            {
                response = await _sharedClient.PostAsync(url, sendingMessage);
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