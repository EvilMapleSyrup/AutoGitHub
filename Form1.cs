using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoGitHub
{
    public partial class Form1 : Form
    {
        GitHubController gc = new GitHubController();
        string mainFP;
        string selected;

        public Form1()
        {
            InitializeComponent();
        }

        private void SelectFP(object sender, EventArgs e)
        {
            var fp = Prompts.ShowDialog_FolderPath();
            mainFP = fp;
            var fpBreakdown = fp.Split(@"\"[0]);
            selected = fpBreakdown[fpBreakdown.Length - 1];
            selectedLabel.Text = "Selected : " + selected;
            outputBox.Text += string.Format("File Path Selected : \"{0}\"", mainFP);
        }

        private void Push(object sender, EventArgs e)
        {
            outputBox.Text = "Pushing update";
            gc.HandleRepo(selected, mainFP, outputBox);
        }

        private void EditDescription(object sender, EventArgs e)
        {
            var newDesc = Prompts.ShowDialog_TextBox("Enter Description");
            gc.EditDesc(selected, newDesc);
            outputBox.Text = "Description Edit Successful";

        }

    }
}
