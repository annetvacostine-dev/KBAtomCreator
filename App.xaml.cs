using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KBAtomCreator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            // Создаем и показываем окно загрузки
            var splashScreen = new Splashscreen();
            splashScreen.Show();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Создаем главное окно (оно будет скрыто до завершения загрузки)
            var mainWindow = new MainWindow(splashScreen);
        }
    }
}
