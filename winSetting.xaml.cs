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
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace VpetChatWithOllama
{
    public partial class winSetting : Window
    {
        ChatWithOllama plugin;
        public winSetting(ChatWithOllama plugin)
        {
            InitializeComponent();
            this.plugin = plugin;
            cbModel.ItemsSource = _models; // Bind models to ComboBox
        }
        // Observable collection for dynamic model options
        private ObservableCollection<string> _models = new ObservableCollection<string>
        {
            "gpt-3.5-turbo",
            "gpt-4-turbo",
            "gpt-4"
        };


        // Example method to dynamically add a model
        public void AddModel(string modelName)
        {
            if (!_models.Contains(modelName))
            {
                _models.Add(modelName);
            }
        }

        // Save button logic
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Extract and save settings
            var settings = new PluginInformations.PluginSettings
            {
                url = tbAPIURL.Text,
                moduleName = cbModel.SelectedItem?.ToString(),
                prompt = tbPromptTemplate.Text,
                historyLength = (int)niHistoryLength.Value,
                chatHistory = tbChatHistory.Text
            };

            // Handle saving settings logic here
            MessageBox.Show("Settings saved successfully!");
        }
    }
}