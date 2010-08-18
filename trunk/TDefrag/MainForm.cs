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

            MainLib koko = new MainLib();

            koko.StartDefrag(@"E:\");
        }
    }
}
