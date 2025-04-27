using LinePutScript.Localization.WPF;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VpetChatWithOllama
{
    public class ChatWithOllama : MainPlugin
    {
        public MessageBar MsgBar => (MessageBar)MW.Main.MsgBar;
        public OllamaChatCore COllama;
        public ChatWithOllamaAPI COllamaAPI;
        private PluginInformations.PluginSettings _settings;

        public PluginInformations.PluginSettings settings
        {
            set
            {
                _settings = value;
                COllama = new OllamaChatCore(value, GetMapping());
            }
            get { return _settings; }
        }

        public Dictionary<String, Func<string>> GetMapping()
        {
            return new()
            {
                { "{Name}", () => MW.Main.Core.Save.Name },
                { "{CurTime}", () => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                { "{Money}", () => MW.Main.Core.Save.Money.ToString() },
                { "{HostName}", () => MW.Main.Core.Save.HostName },
                { "{Exp}", () => MW.Main.Core.Save.Exp.ToString() },
                { "{ExpBonus}", () => MW.Main.Core.Save.ExpBonus.ToString() },
                { "{Strength}", () => MW.Main.Core.Save.Strength.ToString() },
                { "{StrengthFood}", () => MW.Main.Core.Save.StrengthFood.ToString() },
                { "{Feeling}", () => MW.Main.Core.Save.Feeling.ToString() },
                { "{Health}", () => MW.Main.Core.Save.Health.ToString() }
            };
        }

        /// <summary>
        /// initialize the plugin
        /// </summary>
        /// <param name="mainwin"></param>
        public ChatWithOllama(IMainWindow mainwin) : base(mainwin)
        {
        }

        /// <summary>
        /// the load logic, to load user settings and initialize the plugin
        /// </summary>
        public override void LoadPlugin()
        {
            if (File.Exists(ExtensionValue.BaseDirectory + @"\OllamaSettings.json"))
            {
                PluginInformations.PluginSettings tempSetting =
                    PluginInformations.getFromJson(
                        File.ReadAllText(ExtensionValue.BaseDirectory + @"\OllamaSettings.json"));

                if (tempSetting != null)
                    settings = tempSetting;
            }

            if (settings == null)
                settings = new PluginInformations.PluginSettings();


            MW.TalkAPI.Add(new ChatWithOllamaAPI(this));
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
                _settings.tokenCount = COllama.TokenCount;
                File.WriteAllText(ExtensionValue.BaseDirectory + @"\OllamaSettings.json",
                    JsonConvert.SerializeObject(settings));
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

    public class ChatWithOllamaAPI : TalkBox
    {
        private OllamaMessageBar ollamaMessageBar;
        private ChatWithOllama mainPlugin;
        public override string APIName => "ChatOllama";

        public ChatWithOllamaAPI(ChatWithOllama mainPlugin) : base(mainPlugin)
        {
            ollamaMessageBar = new(mainPlugin);
            this.mainPlugin = mainPlugin;
        }

        public override async void Responded(string text)
        {
            Dispatcher.Invoke(() => this.IsEnabled = false);
            DisplayThink();

            if (mainPlugin.COllama == null)
            {
                DisplayThinkToSayRnd("请先前往设置中设置 ChatWithOllama API".Translate());
                return;
            }

            try
            {
                if (!mainPlugin.settings.enableStream)
                {
                    Dispatcher.Invoke(() => ollamaMessageBar.ForceClose());
                    String res = await mainPlugin.COllama.Chat(text);
                    if (!mainPlugin.settings.showR1Think)
                    {
                        res = Regex.Replace(res, @"<think>.*?</think>", String.Empty, RegexOptions.Singleline);
                    }

                    DisplayThinkToSayRnd(res);
                }
                else
                {
                    Dispatcher.Invoke(() => mainPlugin.MW.Main.MsgBar.ForceClose());
                    Dispatcher.Invoke(() => ollamaMessageBar.Show(mainPlugin.MW.Main.Core.Save.Name));
                    
                    var graphname = mainPlugin.MW.Core.Graph.FindName(GraphInfo.GraphType.Say);

                    bool showText = true;
                    bool first = true;
                    Action<string> action = message =>
                    {
                        if (message.Contains("<think>") && !mainPlugin.settings.showR1Think)
                        {
                            showText = false;
                        }

                        if (showText)
                        {
                            if (first)
                            {
                                Dispatcher.Invoke(
                                    () => mainPlugin.MW.Main.Display(
                                        graphname,
                                        GraphInfo.AnimatType.A_Start,
                                        () => mainPlugin.MW.Main.DisplayBLoopingForce(graphname))
                                );
                                first = false;
                            }
                            ollamaMessageBar.UpdateText(message);
                        }

                        else
                            ollamaMessageBar.UpdateText("");

                        if (message.Contains("</think>") && !mainPlugin.settings.showR1Think)
                        {
                            showText = true;
                        }
                    };
                    String res = await mainPlugin.COllama.Chat(text, action);

                    Dispatcher.Invoke(() => mainPlugin.MW.Main.DisplayDefault());
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
            public bool enhancePrompt;
            public string chatHistory;
            public bool supportTool;
            public bool enableStream;
            public bool showR1Think;
            public string promptBeforeUserInput;
            public long tokenCount;

            public PluginSettings()
            {
                this.url = "http://localhost:11434/";
                this.moduleName = "";
                this.prompt = "";
                this.enhancePrompt = true;
                this.chatHistory = "[]";
                this.supportTool = false;
                this.enableStream = false;
                this.showR1Think = true;
                this.promptBeforeUserInput = "";
                this.tokenCount = 0;
            }
        }

        public static PluginSettings getFromJson(string json)
        {
            try
            {
                var jobj = JObject.Parse(json);
                return jobj.ToObject<PluginSettings>();
            }
            catch (Exception e)
            {
                MessageBox.Show("读取文档失败".Translate() + e.Message);
            }

            return null;
        }
    }
}