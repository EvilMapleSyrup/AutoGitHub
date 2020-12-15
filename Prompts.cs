using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoGitHub
{
    public static class Prompts
    {
        public static string ShowDialog_FolderPath()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                    return folderDialog.SelectedPath;
            }
            return "";
        }

        public static string ShowDialog_TextBox(string title)
        {
            var prompt = new Form()
            {
                Width = 400,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Text = title };
            TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 300, Height = 50, Multiline = true };
            Button confirmation = new Button() { Text = "Ok", Left = 250, Width = 100, Top = 130, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";

        }

        public static string ShowDialog_ComboBox(dynamic source, string title)
        {
            var prompt = new Form()
            {
                Width = 400,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title + " Prompt",
                StartPosition = FormStartPosition.CenterParent
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Text = title };
            ComboBox cBox = new ComboBox() { Left = 50, Top = 50, Width = 300, DataSource = source };
            Button confirmation = new Button() { Text = "Ok", Left = 250, Width = 100, Top = 130, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(cBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? cBox.SelectedItem.ToString() : "";
        }
    }
}
