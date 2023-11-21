using System.ComponentModel;
using System.Windows;

namespace Bio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowVM vm;
        public MainWindow()
        {
            InitializeComponent();
            vm = new MainWindowVM();
            DataContext = vm;
            vm.RestoreData();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            vm.SaveData();
        }
    }
}
