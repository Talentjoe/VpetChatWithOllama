using System.Globalization;
using System.Text.RegularExpressions;
using LinePutScript.Localization.WPF;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VpetChatWithOllama;

public class ChatWithOllamaAPI : TalkBox
{
    private ChatWithOllama mainPlugin;
    private Boolean isGenerating = false;
    public override string APIName => "ChatOllama";

    public ChatWithOllamaAPI(ChatWithOllama mainPlugin) : base(mainPlugin) => this.mainPlugin = mainPlugin;

    public bool ProactiveGeneratePossible()
    {
        if (mainPlugin.MW.TalkBoxCurr.APIName != "ChatOllama")
            return false;
        if (!mainPlugin.settings.ProactiveTalking)
            return false;
        if (isGenerating)
            return false;

        return true;
    }
    
    public async void ResponseToFood(Food food)
    {
        if (!ProactiveGeneratePossible())
            return;

        Task.Delay(500).Wait();


        if (Random.Shared.Next(0,100) < mainPlugin.settings.ProactiveTalkingRate)
        {
            String text = "You have ate " + food.Name + " " + food.Description + " \n please response to it in" +
                          CultureInfo.CurrentCulture?.EnglishName + ".";
            GenText(text);
        }
    }

    public async void ResponseToMusic(MediaClient.MediaInfo info)
    {
        if (!ProactiveGeneratePossible())
            return;

        Task.Delay(500).Wait();

        if (mainPlugin.settings.CanSupportMediaBar && mainPlugin.settings.SupportMediaBar &&
            Random.Shared.Next(0,100) < mainPlugin.settings.ProactiveTalkingRate)
        {
            GenText("User is listening to " + info.Title + " by " + info.Artist +
                    ". Please respond to it in " + CultureInfo.CurrentCulture?.EnglishName + ".");
        }
    }


    public override async void Responded(string text)
    {
        GenText(text);
    }

    private async void GenText(string text, bool isSystem = false)
    {
        Dispatcher.Invoke(() => this.IsEnabled = false);
        isGenerating = true;
        try
        {
            DisplayThink();

            if (!mainPlugin.settings.EnableStream)
            {
                String res = await mainPlugin.COllama.Chat(text, isSystem);
                if (!mainPlugin.settings.ShowR1Think)
                {
                    res = Regex.Replace(res, @"<think>.*?</think>", String.Empty, RegexOptions.Singleline);
                }

                DisplayThinkToSayRnd(res);
            }
            else
            {

                SayInfoWithStream sayInfoWithStream = new();

                bool showText = true;
                bool first = true;
                
                Action<string> action = message =>
                {
                    if (!mainPlugin.settings.ShowR1Think && message.Contains("<think>"))
                    {
                        showText = false;
                    }

                    if (showText)
                    {
                        if (first)
                        {
                            DisplayThinkToSayRnd(sayInfoWithStream);
                            first = false;
                        }

                        sayInfoWithStream.UpdateText(message);
                    }

                    if (message.Contains("</think>") && !mainPlugin.settings.ShowR1Think)
                    {
                        showText = true;
                    }
                };

                await mainPlugin.COllama.Chat(text, action, isSystem);
                sayInfoWithStream.FinishGenerate();
            }
        }
        catch (Exception e)
        {
            DisplayThinkToSayRnd("ChatOllama API 出现错误: ".Translate() + e.Message);
        }
        Dispatcher.Invoke(() => this.IsEnabled = true);
        isGenerating = false;
    }

    public override void Setting() => mainPlugin.Setting();
}