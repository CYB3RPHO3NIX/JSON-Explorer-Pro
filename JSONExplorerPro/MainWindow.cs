using DevLab.JmesPath.Expressions;
using DevLab.JmesPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;

namespace JSONExplorerPro
{
    public partial class MainWindow : Form
    {
        private const int IndentationSpaces = 4;
        private string currentFilePath = string.Empty;

        private SyntaxHighlighter syntaxHighlighter;
        public MainWindow()
        {
            InitializeComponent();
            syntaxHighlighter = new SyntaxHighlighter(richTextBox);
        }

        private void richTextBox_TextChanged(object sender, EventArgs e)
        {
            syntaxHighlighter.HighlightJsonSyntax();
            Parse();
        }

        private void Parse()
        {
            int currentPosition = richTextBox.SelectionStart;
            int currentLength = richTextBox.SelectionLength;

            string jsonText = richTextBox.Text;
            try
            {
                JToken json = JToken.Parse(jsonText);
                treeView.BeginUpdate();
                treeView.Nodes.Clear();
                PopulateTreeView(json, treeView.Nodes);
            }
            catch (JsonReaderException ex)
            {
                treeView.Nodes.Clear();
                toolStripStatusLabel1.Text = $"Invalid JSON {ex.Message}";
            }
            finally
            {
                treeView.EndUpdate();
            }

            // Reset the selection to the original position
            richTextBox.Select(currentPosition, currentLength);
        }

        private void PopulateTreeView(JToken jsonToken, TreeNodeCollection treeNodes)
        {
            switch (jsonToken.Type)
            {
                case JTokenType.Object:
                    JObject jsonObject = (JObject)jsonToken;
                    foreach (var property in jsonObject.Properties())
                    {
                        TreeNode node = new TreeNode(property.Name);
                        treeNodes.Add(node);
                        PopulateTreeView(property.Value, node.Nodes);
                    }
                    break;
                case JTokenType.Array:
                    JArray jsonArray = (JArray)jsonToken;
                    for (int i = 0; i < jsonArray.Count; i++)
                    {
                        TreeNode node = new TreeNode($"[{i}]");
                        treeNodes.Add(node);
                        PopulateTreeView(jsonArray[i], node.Nodes);
                    }
                    break;
                default:
                    TreeNode valueNode = new TreeNode(jsonToken.ToString());
                    treeNodes.Add(valueNode);
                    break;
            }
        }

        private void MinifyJson()
        {
            string json = richTextBox.Text;

            // Minify JSON
            string minifiedJson = MinifyJson(json);

            // Open new window to display minified JSON
            ShowMinifiedJsonWindow(minifiedJson);
        }
        private string MinifyJson(string json)
        {
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(jsonObj);
        }

        private void ShowMinifiedJsonWindow(string minifiedJson)
        {
            // Create a new form
            Form minifiedJsonForm = new Form();
            minifiedJsonForm.Padding = new Padding(5);
            minifiedJsonForm.Text = "Minified JSON";

            // Create a new RichTextBox control
            RichTextBox rtbMinifiedJson = new RichTextBox();
            rtbMinifiedJson.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular);
            rtbMinifiedJson.Dock = DockStyle.Fill;
            rtbMinifiedJson.Text = minifiedJson;
            rtbMinifiedJson.ReadOnly = true;

            // Create a new Button control
            Button btnCopyToClipboard = new Button();
            btnCopyToClipboard.Text = "Copy to Clipboard";
            btnCopyToClipboard.Dock = DockStyle.Bottom;
            btnCopyToClipboard.Click += (sender, e) =>
            {
                Clipboard.SetText(minifiedJson);
                MessageBox.Show("Minified JSON copied to clipboard!");
            };

            // Add controls to the form
            minifiedJsonForm.Controls.Add(rtbMinifiedJson);
            minifiedJsonForm.Controls.Add(btnCopyToClipboard);

            // Set form size
            minifiedJsonForm.ClientSize = new System.Drawing.Size(400, 300);

            // Display the form
            minifiedJsonForm.ShowDialog();
        }
        private void textBoxQuery_TextChanged(object sender, EventArgs e)
        {
            string jsonData = richTextBox.Text;
            string query = textBoxQuery.Text;

            // Parse JSON data
            JToken jsonToken = null;
            try
            {
                jsonToken = JToken.Parse(jsonData);
            }
            catch (JsonReaderException)
            {
                // Invalid JSON, reset the treeView
                treeView.Nodes.Clear();
                return;
            }

            // Evaluate JMESPath query
            var jmesPath = new JmesPath();
            JToken result;
            try
            {
                result = jmesPath.Transform(jsonToken, query);
            }
            catch (Exception)
            {
                // Invalid JMESPath query, reset the treeView
                treeView.Nodes.Clear();
                return;
            }

            // Populate treeView with query results
            treeView.Nodes.Clear();
            PopulateTreeView(result, treeView.Nodes);
        }

        private void minifyJSONToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MinifyJson();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check if there are unsaved changes
            if (UnsavedChangesPrompt())
            {
                // Save the existing file before creating a new one
                SaveFile();
            }

            // Clear the editor and reset the current file path
            richTextBox.Text = string.Empty;
            currentFilePath = string.Empty;
            richTextBox.Enabled = true;
        }

        private void browseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check if there are unsaved changes
            if (UnsavedChangesPrompt())
            {
                // Save the existing file before browsing for a new one
                SaveFile();
            }

            // Open file dialog to browse for JSON file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files|*.json";
            openFileDialog.Title = "Browse JSON File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Read the selected file and display its content in the editor
                string filePath = openFileDialog.FileName;
                currentFilePath = filePath;

                try
                {
                    string json = File.ReadAllText(filePath);
                    richTextBox.Text = json;
                    richTextBox.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading the file: " + ex.Message);
                }
            }
        }

        private void loadURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check if there are unsaved changes
            if (UnsavedChangesPrompt())
            {
                // Save the existing file before loading JSON from a URL
                SaveFile();
            }

            // Prompt the user for a URL
            string url = PromptForURL();

            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    // Fetch the JSON data from the URL
                    string json = FetchJSONFromURL(url);

                    // Display the fetched JSON data in the editor
                    richTextBox.Text = json;
                    richTextBox.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading JSON from URL: " + ex.Message);
                }
            }
        }
        private bool UnsavedChangesPrompt()
        {
            // Check if there are unsaved changes in the editor
            if (richTextBox.Modified)
            {
                DialogResult result = MessageBox.Show("Do you want to save the changes?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    return SaveFile();
                }
                else if (result == DialogResult.No)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private bool SaveFile()
        {
            if (string.IsNullOrEmpty(richTextBox.Text))
            {
                // If there is no data in the RichTextBox, return false without showing the save dialog
                return false;
            }
            if (string.IsNullOrEmpty(currentFilePath))
            {
                // If the current file path is empty, prompt the user for a new file name
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "JSON Files|*.json";
                saveFileDialog.Title = "Save JSON File";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = saveFileDialog.FileName;
                }
                else
                {
                    return false;
                }
            }

            try
            {
                // Save the content of the editor to the file
                File.WriteAllText(currentFilePath, richTextBox.Text);
                richTextBox.Modified = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving the file: " + ex.Message);
                return false;
            }
        }

        private string PromptForURL()
        {
            // Prompt the user to enter a URL
            string url = string.Empty;
            InputDialog dialog = new InputDialog("Enter URL", "URL:");

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                url = dialog.InputValue;
            }

            return url;
        }

        private string FetchJSONFromURL(string url)
        {
            // Fetch JSON data from the specified URL
            using (WebClient client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check if there are unsaved changes
            if (UnsavedChangesPrompt())
            {
                // Save the existing file before closing
                SaveFile();
            }

            // Clear the editor and reset the current file path
            richTextBox.Text = string.Empty;
            currentFilePath = string.Empty;
            richTextBox.Enabled = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox.Paste();
        }

        private void wordWrapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wordWrapToolStripMenuItem.Checked = !wordWrapToolStripMenuItem.Checked;
            richTextBox.WordWrap = wordWrapToolStripMenuItem.Checked;
        }

        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CollapseTreeViewNodes(treeView.Nodes);
        }

        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExpandTreeViewNodes(treeView.Nodes);
        }

        private void CollapseTreeViewNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.Collapse();
                if (node.Nodes.Count > 0)
                {
                    CollapseTreeViewNodes(node.Nodes);
                }
            }
        }

        private void ExpandTreeViewNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.Expand();
                if (node.Nodes.Count > 0)
                {
                    ExpandTreeViewNodes(node.Nodes);
                }
            }
        }

        private void contactToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string developerName = "CyberPhoenix";
            string developerEmail = "cyberphoenix0250@gmail.com";

            // Display the contact information in a MessageBox
            string contactInfo = $"Developer Name: {developerName}\n" +
                                 $"Email: {developerEmail}\n";

            MessageBox.Show(contactInfo, "Developer Contact Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void aboutJSONExplorerProToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyDescriptionAttribute descriptionAttribute = (AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute));
            AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute));
            AssemblyProductAttribute productAttribute = (AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute));
            Version version = assembly.GetName().Version;

            // Build the about message
            string appName = titleAttribute?.Title ?? "";
            string appVersion = version.ToString();
            string appDescription = descriptionAttribute?.Description ?? "";
            string appInfo = $"Application Name: {appName}\n" +
                             $"Version: {appVersion}\n" +
                             $"Description: {appDescription}";

            MessageBox.Show(appInfo, "About JSON Explorer Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }
    }
}
