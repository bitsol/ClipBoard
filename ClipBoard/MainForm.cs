using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClipBoard
{
    public partial class MainForm : Form
    {
        public ListView list;
        private static string contentFileName = "ClipBoard_data/content.csv";
        private List<string> savedItems;
        private List<string> recentItems;
        public MainForm()
        {
            InitializeComponent();
            list = this.listView;
            savedItems = new List<string>(10);
            recentItems = new List<string>(10);
            string keyName = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            string valueName = "ClipBoard";
            if (Registry.GetValue(keyName, valueName, null) != null)
            {
                checkStartup.Checked = true;
            }
           
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            list.Columns[1].Width = this.list.Width - 50;
            loadContent(contentFileName);
            updateList();
        }

        private void updateList()
        {
            removeDuplicates();
            list.Items.Clear();
            int i = 1;
            foreach (string s in savedItems)
            {
                ListViewItem lvi = 
                    new ListViewItem(new string[] { (i++).ToString(), s });
                list.Items.Add(lvi);
                list.Groups[0].Items.Add(lvi);
            }
            foreach (string s in recentItems)
            {
                ListViewItem lvi = 
                    new ListViewItem(new string[] { (i++).ToString(), s });
                list.Items.Add(lvi);
                list.Groups[0].Items.Add(lvi);
            }

            //frequently write to csv
            writeToCsv();
        }

        private void removeDuplicates()
        {
            for (int i = savedItems.Count - 1; i >= 0; i--)
            {
                if (savedItems.IndexOf(savedItems[i]) != i)
                {
                    savedItems.RemoveAt(i);
                }
            }
            for (int i = recentItems.Count - 1; i >= 0; i--)
            {
                if (recentItems.IndexOf(recentItems[i]) != i 
                    || savedItems.IndexOf(recentItems[i]) >= 0 )
                {
                    recentItems.RemoveAt(i);
                }
            }
        }

        private void writeToCsv()
        {
            string[] lines = new string[savedItems.Count + Math.Min(recentItems.Count, 30)];
            int i = 0;
            foreach (string s in savedItems)
            {
                lines[i++] = "saved: " + Regex.Escape(s);
            }
            foreach (string s in recentItems)
            {
                lines[i++] = "recent:" + Regex.Escape(s);
                if (i >= savedItems.Count + 30)
                {
                    break;
                }
            }
            File.WriteAllLines(contentFileName, lines);
        }

        private void loadContent(string contentFileName)
        {
            Directory.CreateDirectory("ClipBoard_data");
            if (!File.Exists("ClipBoard_data/content.csv"))
            {
                File.Create("ClipBoard_data/content.csv");
            }
            string[] lines = File.ReadAllLines(contentFileName);
            foreach (string s in lines)
            {
                if (s.StartsWith("saved:"))
                {
                    savedItems.Add(Regex.Unescape(s.Substring(7)));
                }
                else if (s.StartsWith("recent:"))
                {
                    recentItems.Add(Regex.Unescape(s.Substring(7)));
                }
            }
        }

        public void keyPressedHandler(Keys keys)
        {
            //control-c pressed
            if ((ModifierKeys & Keys.Control) == Keys.Control && keys == Keys.C)
            {
                string content = Clipboard.GetText();
                if (content.Length != 0)
                {
                    //accept content only if not empty and not too big
                    if (content.Length < 10000)
                    {
                        recentItems.Insert(0, content); //add to top

                        //limit number of recent items
                        if (recentItems.Count > 100)
                        {
                            recentItems.RemoveAt(recentItems.Count - 1);
                        }
                        updateList();
                    }
                }

            }

            //control-` pressed
            if ((ModifierKeys & Keys.Control) == Keys.Control && keys == Keys.Oemtilde)
            {
      
                this.Show();
                this.WindowState = FormWindowState.Normal;

                //bring to front if not
                this.TopMost = true;
                this.TopMost = false;
            }
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            list.Columns[1].Width = this.list.Width - 50;
        }

        private async void listView_DoubleClick(object sender, EventArgs e)
        {
            copyTextToClipBoard();

            //hide after text copied to clipboard
            this.WindowState = FormWindowState.Minimized;

            // paste to curreMonkey talk font cursor
            await Task.Delay(500);
            SendKeys.Send("^V");
        }

        private void copyTextToClipBoard()
        {
            if (this.list.SelectedIndices.Count > 0)
            {
                int index = this.list.SelectedIndices[0];
                string content = this.list.Items[index].SubItems[1].Text;
                Clipboard.SetText(content);
            }
        }

        private void listView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                contextMenuStrip.Show(list, e.X, e.Y);

                if (list.SelectedItems[0].Group.Equals(list.Groups[0])) // in Saved Group
                {
                    saveToolStripMenuItem.Enabled = false;
                }
                else // in Recent group
                {
                    saveToolStripMenuItem.Enabled = true;
                }
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.listView_DoubleClick(sender, e);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = list.SelectedIndices[0] - list.Groups[0].Items.Count;
            savedItems.Add(recentItems[index]);
            updateList();
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (list.SelectedItems[0].Group.Equals(list.Groups[0])) // in Saved Group
            {
                int index = list.SelectedIndices[0];
                savedItems.RemoveAt(index);
            }
            else // in Recent group
            {
                int index = list.SelectedIndices[0] - list.Groups[0].Items.Count;
                recentItems.RemoveAt(index);
            }
            updateList();
        }

        private void checkStartup_CheckedChanged(object sender, EventArgs e)
        {
            if (checkStartup.Checked)
            {
                var path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                RegistryKey key = Registry.CurrentUser.OpenSubKey(path, true);
                key.SetValue("ClipBoard", Application.ExecutablePath.ToString());
            }else
            {
                var path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                RegistryKey key = Registry.CurrentUser.OpenSubKey(path, true);
                key.DeleteValue("Clipboard", false);
            }
        }

        private void checkPortable_CheckedChanged(object sender, EventArgs e)
        {
            if (checkPortable.Checked)
            {
                checkStartup.Checked = false;
                checkStartup.Enabled = false;
            }
            else
            {
                checkStartup.Enabled = true;
            }
        }
    }
}
