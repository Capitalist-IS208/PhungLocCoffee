using System.Configuration;
using System.Data;
using System.Windows;

using PhungLocCoffee_POS.Models;
using PhungLocCoffee_POS.Views;
using PhungLocCoffee_POS.Helpers;

namespace PhungLocCoffee_POS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LocalDatabaseHelper.InitializeDatabase();
        }
    }
}

