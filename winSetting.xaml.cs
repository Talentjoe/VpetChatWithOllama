﻿using Newtonsoft.Json;
using System.Windows;
using System.Collections.ObjectModel;
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

            tbAPIURL.Text = plugin.settings.url == "" ? "http://localhost:11434/" : plugin.settings.url;
            tbPromptTemplate.Text = plugin.settings.prompt;
            ckEnableStream.IsChecked = plugin.settings.enableStream;
            cbModel.Text = plugin.settings.moduleName;
            ckEnhancePrompt.IsChecked = plugin.settings.enhancePrompt;
            tbUserPromptTemplate.Text = plugin.settings.promptBeforeUserInput;
            tbTokenCount.Text = plugin.COllama.tockenCount.ToString();
            try
            {
                tbChatHistory.Text = plugin.COllama.saveHistory();
            }
            catch
            {
                tbChatHistory.Text = "[]";
            }
            ckShowR1Think.IsChecked = plugin.settings.showR1Think;

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
                List<string> modules = await OllamaChatCore.getAllModules(tbAPIURL.Text);
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
                enhancePrompt = ckEnhancePrompt.IsChecked ?? false,
                url = tbAPIURL.Text,
                moduleName = cbModel.Text?.ToString(),
                prompt = tbPromptTemplate.Text,
                chatHistory = tbChatHistory.Text,
                enableStream = ckEnableStream.IsChecked ?? false,
                showR1Think = ckShowR1Think.IsChecked ?? false,
                promptBeforeUserInput = tbUserPromptTemplate.Text
            };
            
           
            MessageBox.Show("设置保存成功");
            this.Close();
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
                tbChatHistory.Text = "[]";
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