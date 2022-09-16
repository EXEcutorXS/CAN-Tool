using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CAN_Tool.Views
{
    /// <summary>
    /// Логика взаимодействия для ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : Window
    {
        private Rectangle rect;
        public ColorPicker(Rectangle r)
        {
            rect = r;

            InitializeComponent();

            for (int i = 0; i < 100; i++)
            {
                Random random = new Random(i);
                Rectangle newRect = new();
                newRect.Fill = new SolidColorBrush(Color.FromRgb((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));
                newRect.Width = 30;
                newRect.Height = 30;
                newRect.MouseDown += ClickHandler;
                Colors.Children.Add(newRect);
            }
        }

        public ColorPicker()
        {
            InitializeComponent();
            
            for (int i = 0; i < 20; i++)
            {
                Random random = new Random(i);
                Rectangle r = new();
                r.Fill = new SolidColorBrush(Color.FromRgb((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));
                r.Width = 40;
                r.Height = 40;
                r.MouseDown += ClickHandler;
                Colors.Children.Add(r);
            }
        }

        private void ClickHandler(object sender, MouseButtonEventArgs e)
        {
            rect.Fill = (sender as Rectangle).Fill;
            Close();
        }
    }
}
