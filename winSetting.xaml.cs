using LinePutScript.Localization.WPF;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VPet_Simulator.Windows.Interface;
using VpetChatWithOllama;

namespace VPet.Plugin.ChatGPTPlugin
{
    /// <summary>
    /// winSetting.xaml 的交互逻辑
    /// </summary>
    public partial class winSetting : Window
    {
        ChatWithOllama plugin;
        long totalused = 0;
        public winSetting(ChatWithOllama plugin)
        {

        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

