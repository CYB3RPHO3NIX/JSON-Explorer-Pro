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

namespace JSONExplorerPro
{
    public partial class MainWindow : Form
    {
        private const int IndentationSpaces = 4;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void richTextBox_TextChanged(object sender, EventArgs e)
        {
            HighlightJsonSyntax();
            Parse();
        }

        private void Parse()
        {
            string jsonText = richTextBox.Text;
            try
            {
                JToken json = JToken.Parse(jsonText);
                treeView.Nodes.Clear();
                PopulateTreeView(json, treeView.Nodes);
            }
            catch (JsonReaderException ex)
            {
                treeView.Nodes.Clear();
                toolStripStatusLabel1.Text = $"Invalid JSON {ex.Message}";
            }
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
        
        private void HighlightJsonSyntax()
        {
            string json = richTextBox.Text;
            int currentPosition = richTextBox.SelectionStart;

            richTextBox.SuspendLayout();
            richTextBox.SelectionStart = 0;
            richTextBox.SelectionLength = richTextBox.TextLength;
            richTextBox.SelectionColor = Color.Black;

            // Regular expressions to match different JSON elements
            string stringPattern = @"""[^""\\]*(?:\\.[^""\\]*)*""";
            string numberPattern = @"\b\d+(\.\d+)?\b";
            string booleanPattern = @"\b(true|false)\b";
            string nullPattern = @"\bnull\b";

            // Match strings and apply color
            MatchCollection stringMatches = Regex.Matches(json, stringPattern);
            foreach (Match match in stringMatches)
            {
                richTextBox.SelectionStart = match.Index;
                richTextBox.SelectionLength = match.Length;
                richTextBox.SelectionColor = Color.DarkRed;
            }

            // Match numbers and apply color
            MatchCollection numberMatches = Regex.Matches(json, numberPattern);
            foreach (Match match in numberMatches)
            {
                richTextBox.SelectionStart = match.Index;
                richTextBox.SelectionLength = match.Length;
                richTextBox.SelectionColor = Color.Blue;
            }

            // Match booleans and apply color
            MatchCollection booleanMatches = Regex.Matches(json, booleanPattern);
            foreach (Match match in booleanMatches)
            {
                richTextBox.SelectionStart = match.Index;
                richTextBox.SelectionLength = match.Length;
                richTextBox.SelectionColor = Color.Green;
            }

            // Match null values and apply color
            MatchCollection nullMatches = Regex.Matches(json, nullPattern);
            foreach (Match match in nullMatches)
            {
                richTextBox.SelectionStart = match.Index;
                richTextBox.SelectionLength = match.Length;
                richTextBox.SelectionColor = Color.Gray;
            }

            richTextBox.SelectionStart = currentPosition;
            richTextBox.SelectionLength = 0;
            richTextBox.ResumeLayout();
            richTextBox.Refresh();
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
    }
}
