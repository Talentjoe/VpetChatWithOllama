using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Windows.Interface;

namespace VpetChatWithOllama
{
    public class VpetChatWithOllama : MainPlugin
    {
        public VpetChatWithOllama(IMainWindow mainwin) : base(mainwin) { }
        public override void LoadPlugin()
        {
            MW.TalkAPI.Add(new ChatOllamaAPI(this));
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
        OllamaChatCore OllamaChatCorer;
        public ChatOllamaAPI(VpetChatWithOllama mainPlugin) : base(mainPlugin)
        {
            try
            {
                OllamaChatCorer = new OllamaChatCore(prompt: "你是一只猫娘,你叫爱丽丝");

            }
            catch (Exception e)
            {
                DisplayThinkToSayRnd("OllamaChatCore初始化失败");
            }
            Plugin = mainPlugin;
        }
        protected VpetChatWithOllama Plugin;
        public override string APIName => "ChatOllama";
        public override async void Responded(string text)
        {
            DisplayThink();
            String res =  await OllamaChatCorer.chat(text);

            DisplayThinkToSayRnd(res);
        }
        public override void Setting()
        {
         
        }
    }
}
