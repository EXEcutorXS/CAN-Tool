using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CAN_Tool.ViewModels
{
    public class DataBox : TextBox
    {
        public DataBox()
        {
            TextChanged += AfterChange;
            SelectionChanged += removeSelection;
            Loaded += Init;
        }

        void Init(object sender, EventArgs e)
        {
            Text = "FF FF FF FF FF FF FF FF";
        }

        void removeSelection(object sender, EventArgs e)
        {
            if (SelectionLength > 0)
                SelectionLength = 0;
        }

        void AfterChange(object sender, EventArgs e)
        {
            if (Text.Replace(" ", "").Length < 17)
                return;
            byte[] posArray = new byte[] { 0, 1, 2, 2, 3, 4, 4, 5, 6, 6, 7, 8, 8, 9, 10, 10, 11, 12, 12, 13, 14, 14, 15, 16, 16 };
            string newString = new String(Text.ToUpper().Where(x => char.IsDigit(x) || char.ToUpper(x) >= 'A' && char.ToUpper(x) <= 'F').ToArray());

            int selectionPos = SelectionStart;
            if (selectionPos >= 23) selectionPos = 23;
            if (newString.Length > 16)
                newString = newString.Remove(posArray[selectionPos], 1);
            if (newString.Length > 16)
                newString = newString.Substring(0, 16);
            string retString = "";
            for (int i = 0; i < newString.Length / 2; i++)
            {
                retString += newString.Substring(i * 2, 2) + " ";
            }
            retString = retString.Trim();

            Text = retString;
            if ((selectionPos - 2) % 3 == 0) selectionPos++;
            SelectionStart = selectionPos;

        }
    }
}
