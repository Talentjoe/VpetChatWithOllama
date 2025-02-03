using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Windows.Interface;

namespace VpetChatWithOllama
{

    public class ChatWithOllama : MainPlugin
    {
        public OllamaChatCore COllama;
        public ChatOllamaAPI COllamaAPI;
        public ChatWithOllama(IMainWindow mainwin) : base(mainwin) { }
        public override void LoadPlugin()
        {
            MW.TalkAPI.Add(new ChatOllamaAPI(this));

            if (File.Exists(ExtensionValue.BaseDirectory + @"\ChatGPTSetting.json"))
                COllama = new OllamaChatCore(File.ReadAllText(ExtensionValue.BaseDirectory + @"\ChatGPTSetting.json"));


        }

        public override void Save()
        {
        }
        public override void Setting()
        {
        }
        public override string PluginName => "ChatGPT";
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
            String res =  await Plugin.COllama.Chat(text);

            DisplayThinkToSayRnd(res);
        }
        public override void Setting()
        {
         
        }

    }
}
