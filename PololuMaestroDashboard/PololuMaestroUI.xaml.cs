using System.Windows;
using PololuMaestro.Dashboard.ViewModel;
using W3C.Soap;

namespace PololuMaestro.Dashboard
{
    /// <summary>
    /// Interaction logic for PololuMaestroUI.xaml
    /// </summary>
    public partial class PololuMaestroUI
    {
        public PololuMaestroUI()
        {
            InitializeComponent();
        }

        public MainViewModel ViewModel
        {
            get { return (MainViewModel) DataContext; }
            set { DataContext = value; }
        }

        public void ShowFault(Fault fault)
        {
            var error = "Error occured!";

            if (fault.Reason != null && fault.Reason.Length > 0 && !string.IsNullOrEmpty(fault.Reason[0].Value))
            {
                error = fault.Reason[0].Value;
            }

            MessageBox.Show(
                this,
                error,
                Title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
