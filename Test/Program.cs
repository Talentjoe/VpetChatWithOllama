
using System.Threading.Tasks.Dataflow;
using System.Windows.Interop;
using VPet_Simulator.Windows.Interface;
using VpetChatWithOllama;


public class Program
{
    public static event Action<string> a;
    static async Task Main()
    {
        OllamaChatCore chat = new(PluginInformations.getFromJson("{\"url\":\"http://localhost:11434/\",\"moduleName\":\"deepseek-r1:14b\",\"prompt\":\"你是一只猫娘\",\"addTimeAsPrompt\":true,\"chatHistory\":\"[]\",\"supportTool\":false,\"enableStream\":false}"));

        a += msg=>Console.Write(msg);
        while(true)
        {
            String b = await chat.Chat(Console.ReadLine(), a);
            Console.WriteLine();
        }
    }

}


