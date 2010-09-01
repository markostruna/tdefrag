using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TDefragLib;

namespace TDefrag
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            defragLib = new MainLib(this);

            FillDiskArray();
        }

        private void startDefrag_Click(object sender, EventArgs e)
        {
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
            //defragLog.Lines[defragLog.Lines.Count()] = line;
            //defragLog.Text += "\n" + line;
            //defragLog.Text += String.Format("\n\r{0:S}", line);

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

            diskArray.Items.AddRange(DriveList);
        }

        private MainLib defragLib;
    }
}
