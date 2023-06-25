using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CAN_Tool.ViewModels
{
    public class DataBox:TextBox
    {
        public DataBox() {
            TextChanged += AfterChange;
            Text = "FF FF FF FF FF FF FF FF";
        }


        void AfterChange(object sender,EventArgs e)
        {
            if (Text.Replace(" ", "").Length < 17) 
                return;
            byte[] posArray = new byte[] { 0, 1, 2, 2, 3, 4, 4, 5, 6, 6, 7, 8, 8, 9, 10, 10, 11, 12, 12, 13, 14, 14, 15, 16, 16 };
            var arr = Text.ToUpper().ToCharArray();
            for (int i = 0; i < Text.Length; i++)
            {

                if (arr[i] >= '0' && arr[i] <= '9' || arr[i] >= 'A' && arr[i] <= 'F')
                    continue;
                arr[i] = ' ';
            }
            string newString = new(arr);
            newString = newString.Replace(" ", "");

            int selectionPos = SelectionStart;
            if (selectionPos >= 0x17) selectionPos = 0x17;
            if (newString.Length > 16)
                newString = newString.Remove(posArray[selectionPos + 1],1);
            if (newString.Length > 16)
                newString =  newString.Substring(0, 16);
            string retString = "";
                for (int i = 0; i < 8; i++)
            {
                retString += newString.Substring(i * 2, 2) + " ";
            }
            retString = retString.Trim();
            
            Text = retString;
            if ((selectionPos - 2) % 3 == 0) selectionPos++;
            SelectionStart= selectionPos;

        }
    }
}
