using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VpetChatWithOllama
{

    public class ChatWithOllama : MainPlugin
    {
        public MessageBar MsgBar => (MessageBar)MW.Main.MsgBar;
        public OllamaChatCore COllama;
        public ChatOllamaAPI COllamaAPI;
        private PluginInformations.PluginSettings _settings;
        public PluginInformations.PluginSettings settings
        {
            set { _settings = value; COllama = new OllamaChatCore(value); }
            get { return _settings; }
        }

        /// <summary>
        /// initialize the plugin
        /// </summary>
        /// <param name="mainwin"></param>
        public ChatWithOllama(IMainWindow mainwin) : base(mainwin) { }
        
        /// <summary>
        /// the load logic, to load user settings and initialize the plugin
        /// </summary>
        public override void LoadPlugin()
        {
            if (File.Exists(ExtensionValue.BaseDirectory + @"\OllamaSettings.json"))
                settings = PluginInformations.getFromJson(File.ReadAllText(ExtensionValue.BaseDirectory + @"\OllamaSettings.json"));

            else
                settings = new PluginInformations.PluginSettings();


            MW.TalkAPI.Add(new ChatOllamaAPI(this));
            var menuItem = new MenuItem()
            {
                Header = "ChatOllamaAPI",
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            menuItem.Click += (s, e) => { Setting(); };
            MW.Main.ToolBar.MenuMODConfig.Items.Add(menuItem);
        }

        /// <summary>
        /// save logic, used when exit, persiste the settings
        /// </summary>
        public override void Save()
        {
            if (settings != null)
            {
                _settings.chatHistory = COllama.saveHistory();
                File.WriteAllText(ExtensionValue.BaseDirectory + @"\OllamaSettings.json", JsonConvert.SerializeObject(settings));
            }        
        }

        /// <summary>
        /// the user interface for setting the plugin
        /// </summary>
        public override void Setting()
        {
            new winSetting(this).ShowDialog();
        }

        /// <summary>
        /// The name of the plugin
        /// </summary>
        public override string PluginName => "ChatWithOllama";
    }

    public class ChatOllamaAPI : TalkBox
    {
        public OllamaMessageBar ollamaMessageBar;
        protected ChatWithOllama mainPlugin;
        public override string APIName => "ChatOllama";

        public ChatOllamaAPI(ChatWithOllama mainPlugin) : base(mainPlugin)
        {
            ollamaMessageBar = new(mainPlugin);
            this.mainPlugin = mainPlugin;
        }
        public override async void Responded(string text)
        {
            Dispatcher.Invoke(() => this.IsEnabled = false);
            if (!(mainPlugin.settings.enableStream))
                DisplayThink();
            if (mainPlugin.COllama == null)
            {
                DisplayThinkToSayRnd("请先前往设置中设置 ChatOllama API");
                return;
            }
            try
            {
                if (!mainPlugin.settings.enableStream)
                {
                    Dispatcher.Invoke(() => ollamaMessageBar.ForceClose());
                    String res = await mainPlugin.COllama.Chat(text);
                    DisplayThinkToSayRnd(res);
                }
                else
                {
                    Dispatcher.Invoke(() => mainPlugin.MW.Main.MsgBar.ForceClose());
                    Dispatcher.Invoke(() => ollamaMessageBar.Show(mainPlugin.MW.Main.Core.Save.Name));

                    Action<string> action = message =>
                    {
                        ollamaMessageBar.UpdateText(message);
                    };
                    String res = await mainPlugin.COllama.ChatWithStream(text, action);

                    Dispatcher.Invoke(() => ollamaMessageBar.FinishText());
                }
            }

            catch (Exception ex)
            {
                DisplayThinkToSayRnd("ChatOllama API 出现错误: " + ex.Message);
            }
            Dispatcher.Invoke(() => this.IsEnabled = true);

        }
        public override void Setting() => mainPlugin.Setting();



    }

    public class PluginInformations
    {
        public class PluginSettings
        {
            public string url;
            public string moduleName;
            public string prompt;
            public bool addTimeAsPrompt;
            public string chatHistory;
            public bool supportTool;
            public bool enableStream;

            public PluginSettings()
            {
                this.url = "http://localhost:11434/";
                this.moduleName = "";
                this.prompt = "";
                this.addTimeAsPrompt = true;
                this.chatHistory = "[]";
                this.supportTool = false;
            }
        }

        public static PluginSettings getFromJson(string json)
        {
            var jobj = JObject.Parse(json);
            return jobj.ToObject<PluginSettings>();
        }
    }

}
