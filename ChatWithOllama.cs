using LinePutScript.Localization.WPF;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.RegularExpressions;
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

            COllamaAPI = new ChatWithOllamaAPI(this);
            MW.TalkAPI.Add(COllamaAPI);
            var menuItem = new MenuItem
            {
                Header = "ChatOllamaAPI",
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            MenuItem modeset = MW.Main.ToolBar.MenuMODConfig;
            modeset.Visibility = Visibility.Visible;
            menuItem.Click += (s, e) => { Setting(); };
            MW.Main.ToolBar.MenuMODConfig.Items.Add(menuItem);

            if (settings.ProactiveTalking)
                MW.Event_TakeItem +=  (info)=>
                {
                    Task.Run(() => COllamaAPI.ResponseToFood(info));
                };


            if (MW.OnModInfo.Any(mod => mod.Name.Equals("VpetMediaBar", StringComparison.OrdinalIgnoreCase)))
            {
                settings.CanSupportMediaBar = true;
                _ = Task.Run(() => { LoadWithDelay(); });
            }
            else 
            {
                settings.CanSupportMediaBar = false;
            }
        }

        public async void LoadWithDelay()
        {
            int waitMs = 0;
            await Task.Delay(3000);
            while (!MW.DynamicResources.ContainsKey("MediaInfo") && waitMs < 5000)
            {
                await Task.Delay(200);
                waitMs += 200;
            }

            try
            {
                var a = MW.DynamicResources["MediaInfo"];
                if (a is MediaClient mediaClient)
                {
                    mediaClient.OnMediaInfoReceived += (info)=>
                    {
                        Task.Run(() => COllamaAPI.ResponseToMusic(info));
                    };
                }
            }
            catch (Exception ex)
            {
                 MessageBox.Show("VpetMediaBar not loaded, please check the plugin.".Translate() + ex.Message);
            }
        }

        /// <summary>
        /// save logic, used when exited, persist the settings
        /// </summary>
        public override void Save()
        {
            if (settings != null)
            {
                _settings.ChatHistory = COllama.saveHistory();
                _settings.TokenCount = COllama.TokenCount;
                _settings.PromptCount = COllama.PromptCount;
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

    public class PluginInformations
    {
        public class PluginSettings
        {
            public string Url = "http://localhost:11434/";
            public string ModuleName = "";
            public string Prompt = "";
            public bool EnhancePrompt = true;
            public string ChatHistory = "[]";
            public bool SupportTool = false;
            public bool EnableStream = true;
            public bool ShowR1Think = false;
            public string PromptBeforeUserInput = "";
            public long TokenCount = 0;
            public long PromptCount = 0;
            public bool ProactiveTalking = false;
            public int ProactiveTalkingRate = 20; // 20% chance to respond proactively
            public bool CanSupportMediaBar = false;
            public bool SupportMediaBar = false;

            public PluginSettings()
            {
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

            return new PluginSettings();
        }
    }
}