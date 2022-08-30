using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAN_Tool.ViewModels.Base;
namespace CAN_Tool.ViewModels
{
    internal class MainWindowViewModel:ViewModel
    {
        #region Title property
        /// <summary>
        /// MainWindow title
        /// </summary>
        private string _Title = "Advers CAN Tool";

        public string Title
        {
            get => _Title;
            set => Set(ref _Title, value);
        }
        #endregion
    }
}
