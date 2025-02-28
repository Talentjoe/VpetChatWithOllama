
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using System.Windows.Interop;
using VPet_Simulator.Windows.Interface;
using VpetChatWithOllama;


public class Program
{
    public static event Action<string> a;
    static async Task Main()
    {

        //Regex regex = new Regex( @"<h1\s*([^>]*)>");
        //Console.WriteLine(
        //                regex.Match("<h1 1 >").Groups[1].Value);
        OllamaChatCore chat = new(PluginInformations.getFromJson("{\"url\":\"http://localhost:11434/\",\"moduleName\":\"deepseek-r1:14b\",\"prompt\":\"你是一只猫娘\",\"addTimeAsPrompt\":true,\"chatHistory\":\"[]\",\"supportTool\":false,\"enableStream\":false}"));


        a += msg=>Console.Write(msg);
        while(true)
        {
            String b = await chat.ChatWithStream(Console.ReadLine(), a);
            Console.WriteLine();
        }
    }

}


