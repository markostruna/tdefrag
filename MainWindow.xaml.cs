using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TDefragLib;

namespace TDefragWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            defragLib = new MainLib(this);

            FillDiskArray();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            defragLog.Text = String.Empty;

            try
            {
                String drive = diskArray.Text;
                defragLib.StartDefrag(drive);
            }
            catch (Exception e1)
            {
                AddLine(e1.Message);
            }
        }

        public void AddLine(String line)
        {
            if (String.IsNullOrEmpty(defragLog.Text))
            {
                defragLog.Text = line;
            }
            else
            {
                defragLog.Text = String.Concat(defragLog.Text, Environment.NewLine, line);
            }
        }

        private void FillDiskArray()
        {
            String[] DriveList = Environment.GetLogicalDrives();

            foreach (String drive in DriveList)
                diskArray.Items.Add(drive);

            diskArray.SelectedIndex = 0;
        }

        private MainLib defragLib;

    }
}
