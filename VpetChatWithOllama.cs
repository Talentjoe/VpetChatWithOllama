using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VPet.Plugin.ChatGPTPlugin;
using VPet_Simulator.Windows.Interface;

namespace VpetChatWithOllama
{

    public class ChatWithOllama : MainPlugin
    {
        public OllamaChatCore COllama;
        public ChatOllamaAPI COllamaAPI;
        public PluginInformations.PluginSettings settings;

        public ChatWithOllama(IMainWindow mainwin) : base(mainwin) { }
        public override void LoadPlugin()
        {
            MW.TalkAPI.Add(new ChatOllamaAPI(this));
            var menuItem = new MenuItem()
            {
                Header = "ChatOllamaAPI",
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            menuItem.Click += (s, e) => { Setting(); };
            MW.Main.ToolBar.MenuMODConfig.Items.Add(menuItem);

            COllama = new OllamaChatCore(prompt:"你是一个猫娘 请简短的回答主人的问题");
                

        }

        public override void Save()
        {
        }
        public override void Setting()
        {
            new winSetting(this).ShowDialog();
        }
        public override string PluginName => "ChatWithOllama";
    }

    public class ChatOllamaAPI: TalkBox
    {
        public ChatOllamaAPI(ChatWithOllama mainPlugin) : base(mainPlugin)
        {
            Plugin = mainPlugin;
        }
        protected ChatWithOllama Plugin;
        public override string APIName => "ChatOllama";
        public override async void Responded(string text)
        {
            DisplayThink();
            if(Plugin.COllama == null)
            {
                DisplayThinkToSayRnd("请先前往设置中设置 ChatOllama API");
                return;
            }
            String res =  await Plugin.COllama.Chat(text);

            DisplayThinkToSayRnd(res);
        }
        public override void Setting() => Plugin.Setting();
        

    }

    public class  PluginInformations
    {
        public struct PluginSettings
        {
            public string url;
            public string moduleName;
            public string prompt;
            public bool addTimeAsPrompt;
            public string chatHistory;
            public int historyLength;
            public int supportTool;
        }
        
    }
}
