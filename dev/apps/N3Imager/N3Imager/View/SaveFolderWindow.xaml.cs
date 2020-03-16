using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace N3Imager.View
{
    /// <summary>
    /// Interaction logic for SaveFolderWindow.xaml
    /// </summary>
    public partial class SaveFolderWindow : Window
    {
        string _extension = "";

        public SaveFolderWindow(string currentName, string ext)
        {
            InitializeComponent();
            _extension = ext;
            txtSaveFolderName.Text = currentName;
            txtSaveFolderPath.Text = GlobalVariables.globalSettings.SaveFolderPath;
        }

        public string SaveFolderName
        {
            get { return txtSaveFolderName.Text; }
        }

        public bool SaveFolderNameSet { get; set; }

        private void btnFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.RootFolder = Environment.SpecialFolder.MyComputer;
                                
                var result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && 
                    !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    txtSaveFolderPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SaveFolderName))
            {
                System.Windows.MessageBox.Show("Please enter a valid save folder name", "Save Folder Name is needed");
                return;
            }

            GlobalVariables.globalSettings.SaveFolderPath = txtSaveFolderPath.Text;

            var root = GlobalVariables.globalSettings.SaveFolderPath + @"\" + SaveFolderName;
            if (System.IO.Directory.Exists(root))
            {
                string fileName = new System.IO.DirectoryInfo(root).Name;
                var fileNameSearchStr = fileName + "_*" + _extension;
                System.IO.DirectoryInfo hdDirectoryInWhichToSearch =
                    new System.IO.DirectoryInfo(root);
                var filesInDir = hdDirectoryInWhichToSearch.GetFiles(fileNameSearchStr);
                if (filesInDir.Count() > 0)
                {
                    System.Windows.MessageBox.Show("Please enter a different save folder name", "Save Folder Name already exists");
                    return;
                }
            }

            GlobalVariables.globalSettings.Save();
            SaveFolderNameSet = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
