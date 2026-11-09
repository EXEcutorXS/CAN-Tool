using System.Windows;

namespace CAN_Tool.CustomControls
{
    /// <summary>
    /// Настройки MBC-2 по RV-C (проприетарный пакет 0xC0/0xC1, аналог PGN 7). DataContext - Timberline20RvcViewModel.
    /// </summary>
    public partial class RvcSettingsWindow : Window
    {
        public RvcSettingsWindow()
        {
            InitializeComponent();
        }
    }
}
