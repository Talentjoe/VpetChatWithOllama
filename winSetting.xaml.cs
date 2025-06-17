using Newtonsoft.Json;
using System.Windows;
using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using LinePutScript.Localization.WPF;
using System.Text.RegularExpressions;

namespace VpetChatWithOllama
{
    public partial class winSetting : Window
    {
        ChatWithOllama plugin;

        /// <summary>
        /// Initialize the setting window
        /// </summary>
        /// <param name="plugin">the main plugin</param>
        public winSetting(ChatWithOllama plugin)
        {
            this.plugin = plugin;
            InitializeComponent();

            tbAPIURL.Text = plugin.settings.Url == "" ? "http://localhost:11434/" : plugin.settings.Url;
            tbPromptTemplate.Text = plugin.settings.Prompt;
            ckEnableStream.IsChecked = plugin.settings.EnableStream;
            cbModel.Text = plugin.settings.ModuleName;
            ckEnhancePrompt.IsChecked = plugin.settings.EnhancePrompt;
            tbUserPromptTemplate.Text = plugin.settings.PromptBeforeUserInput;
            ckProactiveConversations.IsChecked = plugin.settings.ProactiveTalking;
            tbTokenCount.Text = plugin.COllama.TokenCount.ToString();
            tbPromptCount.Text = plugin.COllama.PromptCount.ToString();
            ckSupportMediaBar.IsEnabled = plugin.settings.CanSupportMediaBar;
            ckSupportMediaBar.IsChecked = plugin.settings.SupportMediaBar && plugin.settings.CanSupportMediaBar;
            ntbProactiveTalkingRate.Value = plugin.settings.ProactiveTalkingRate;
            try
            {
                tbChatHistory.Text = plugin.COllama.saveHistory();
            }
            catch
            {
                tbChatHistory.Text = "[]";
            }
            ckShowR1Think.IsChecked = plugin.settings.ShowR1Think;

            this.Loaded += WinSetting_Loaded;
        }

        public void LostFocus(object sender, RoutedEventArgs e)
        {
            LoadModulesAsync();
        }

        /// <summary>
        /// use an async method to load the modules list to avoid blocking the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void WinSetting_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadModulesAsync();
        }

        /// <summary>
        /// Load local modules list to the combobox to let user choose
        /// </summary>
        /// <returns></returns>
        private async Task LoadModulesAsync()
        {
            try
            {
                List<string> modules = await OllamaChatCore.GetAllModules(tbAPIURL.Text);
                cbModel.ItemsSource = new ObservableCollection<string>(modules);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ollama 可能未启动,加载模型列表失败: ".Translate() + ex.Message);
            }
        }

        /// <summary>
        /// Dealing with the save button, save the settings to the plugin
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            WinSetting_Loaded(sender, e);
            if (JsonConvert.DeserializeObject<List<Dictionary<String, String>>>(tbChatHistory.Text) == null)
            {
                MessageBox.Show("出问题了 请联系开发者!".Translate());
                return;
            }
            plugin.settings = new PluginInformations.PluginSettings
            {
                EnhancePrompt = ckEnhancePrompt.IsChecked ?? false,
                Url = tbAPIURL.Text,
                ModuleName = cbModel.Text?.ToString(),
                Prompt = tbPromptTemplate.Text,
                ChatHistory = tbChatHistory.Text,
                EnableStream = ckEnableStream.IsChecked ?? false,
                ShowR1Think = ckShowR1Think.IsChecked ?? false,
                PromptBeforeUserInput = tbUserPromptTemplate.Text,
                TokenCount = long.TryParse(tbTokenCount.Text, out long tokenCount) ? tokenCount : 0,
                PromptCount = long.TryParse(tbPromptCount.Text, out long promptCount) ? promptCount : 0,
                ProactiveTalking = ckProactiveConversations.IsChecked ?? false,
                SupportMediaBar = (ckSupportMediaBar.IsChecked ?? false) && plugin.settings.CanSupportMediaBar,
                CanSupportMediaBar = plugin.settings.CanSupportMediaBar,
                ProactiveTalkingRate = (int) (ntbProactiveTalkingRate.Value ?? 20.0) , // Default to 20% if not set
            };

            if (plugin.MW.TalkBoxCurr.APIName == "ChatOllama" )
            {
                MessageBox.Show("设置保存成功".Translate());
                this.Close();
            }
            else
            {
                MessageBox.Show("当前聊天API非 ChatWithOllama 请前往设置".Translate());
            }
        }

        /// <summary>
        /// Clear the chat history, ask if the user really want to delete the history
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("你确定要删除历史对话吗？".Translate(), "删除历史对话".Translate(), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                tbChatHistory.Text = "[]";
                tbTokenCount.Text = 0.ToString();
            }

        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            String PromptTemp = tbPromptTemplate.Text;
            String UserPromptTemp = tbUserPromptTemplate.Text;

            foreach (var item in plugin.GetMapping())
            {
                PromptTemp = Regex.Replace(PromptTemp, item.Key, item.Value());
                UserPromptTemp = Regex.Replace(UserPromptTemp, item.Key, item.Value());
            }

            MessageBox.Show("System Prompt:\n"+ PromptTemp + "\nUser Prompt:\n"+ UserPromptTemp, "Sample Prompt");
        }
    }
}