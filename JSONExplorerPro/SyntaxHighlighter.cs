using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JSONExplorerPro
{
    public class SyntaxHighlighter
    {
        private RichTextBox richTextBox;
        public SyntaxHighlighter(RichTextBox rch) 
        {
            richTextBox = rch;
        }
        //HighlightJsonSyntax Code
        private const int WM_VSCROLL = 0x115;
        private const int SB_ENDSCROLL = 8;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public void HighlightJsonSyntax()
        {
            int currentPosition = richTextBox.SelectionStart;
            int currentLength = richTextBox.SelectionLength;

            // Store the current scroll position
            int scrollPos = GetScrollPos(richTextBox.Handle, SB_VERT);

            // Disable redrawing
            SendMessage(richTextBox.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

            // Regular expressions to match different JSON elements
            string stringPattern = @"""[^""\\]*(?:\\.[^""\\]*)*""";
            string numberPattern = @"\b\d+(\.\d+)?\b";
            string booleanPattern = @"\b(true|false)\b";
            string nullPattern = @"\bnull\b";

            // Match strings and apply color
            MatchCollection stringMatches = Regex.Matches(richTextBox.Text, stringPattern);
            foreach (Match match in stringMatches)
            {
                richTextBox.Select(match.Index, match.Length);
                richTextBox.SelectionColor = Color.DarkRed;
            }

            // Match numbers and apply color
            MatchCollection numberMatches = Regex.Matches(richTextBox.Text, numberPattern);
            foreach (Match match in numberMatches)
            {
                richTextBox.Select(match.Index, match.Length);
                richTextBox.SelectionColor = Color.Blue;
            }

            // Match booleans and apply color
            MatchCollection booleanMatches = Regex.Matches(richTextBox.Text, booleanPattern);
            foreach (Match match in booleanMatches)
            {
                richTextBox.Select(match.Index, match.Length);
                richTextBox.SelectionColor = Color.Green;
            }

            // Match null values and apply color
            MatchCollection nullMatches = Regex.Matches(richTextBox.Text, nullPattern);
            foreach (Match match in nullMatches)
            {
                richTextBox.Select(match.Index, match.Length);
                richTextBox.SelectionColor = Color.Gray;
            }

            // Reset the selection to the original position
            richTextBox.Select(currentPosition, currentLength);

            // Enable redrawing and refresh the control
            SendMessage(richTextBox.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            richTextBox.Invalidate();

            // Restore the scroll position
            SetScrollPos(richTextBox.Handle, SB_VERT, scrollPos, true);
            SendMessage(richTextBox.Handle, WM_VSCROLL, (IntPtr)SB_THUMBPOSITION + 0x10000 * scrollPos, IntPtr.Zero);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);

        [DllImport("user32.dll")]
        private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_SETREDRAW = 0x000B;
        private const int SB_VERT = 1;
        private const int SB_THUMBPOSITION = 4;
    }
}
