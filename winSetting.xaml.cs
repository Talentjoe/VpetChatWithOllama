using Newtonsoft.Json;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using LinePutScript.Localization.WPF;

namespace VpetChatWithOllama
{
    public partial class winSetting : Window
    {
        ChatWithOllama plugin;

            public winSetting(ChatWithOllama plugin)
            {
                this.plugin = plugin;
                InitializeComponent();

                tbAPIURL.Text = plugin.settings.url;
                tbPromptTemplate.Text = plugin.settings.prompt;
                cbModel.Text = plugin.settings.moduleName;
                ckAddTime.IsChecked = plugin.settings.addTimeAsPrompt;
                tbChatHistory.Text = plugin.COllama.saveHistory();
                //cbModel.SelectedIndex = 0;

                this.Loaded += WinSetting_Loaded;  
            }

            private async void WinSetting_Loaded(object sender, RoutedEventArgs e)
            {
                await LoadModulesAsync();
            }

            private async Task LoadModulesAsync()
            {
                try
                {
                    List<string> modules = await plugin.COllama.getAllModules();
                    cbModel.ItemsSource = new ObservableCollection<string>(modules);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ollama 可能未启动,加载模型列表失败: ".Translate() + ex.Message);
                }
            }


        // Save button logic
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            WinSetting_Loaded(sender, e);
            if (JsonConvert.DeserializeObject<List<Dictionary<String, String>>>(tbChatHistory.Text) == null)
            {
                MessageBox.Show("出问题了 请联系开发者!".Translate());
                return;
            }
            // Extract and save settings
            plugin.settings = new PluginInformations.PluginSettings
            {
                addTimeAsPrompt = ckAddTime.IsChecked ?? false,
                url = tbAPIURL.Text,
                moduleName = cbModel.Text?.ToString(),
                prompt = tbPromptTemplate.Text,
                chatHistory = tbChatHistory.Text
            };
            

            MessageBox.Show("设置保存成功");
            this.Close();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("你确定要删除历史对话吗？".Translate(), "删除历史对话".Translate(), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if(res == MessageBoxResult.Yes)
                tbChatHistory.Text = "[]";
        }
    }
}