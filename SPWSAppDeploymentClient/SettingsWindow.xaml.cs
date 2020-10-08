using Microsoft.Win32;
using SPWSAppDeploymentClient.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
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

namespace SPWSAppDeploymentClient
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public Settings settings;
        SqlConnection sqlConnection;
        public SettingsWindow(Settings settings)
        {
            InitializeComponent();
            this.settings = settings;
        }

        private async void TxtServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            


        }

        private async void TxtUsername_PasswordChanged(object sender, RoutedEventArgs e)
        {

        }

        private async void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
           

        }

        private async Task<bool> Validate(int AppId = 0)
        {
            cmbApps.Items.Clear();
            if (!string.IsNullOrEmpty(txtServer.Text) &&
                !string.IsNullOrEmpty(txtUsername.Password) &&
                !string.IsNullOrEmpty(txtPassword.Password))
            {
                try
                {
                    string connString = string.Format(@"Data Source={0};Initial Catalog=SPWSAppDeployment;User Id={1};Password={2}", txtServer.Text, txtUsername.Password, txtPassword.Password);
                    sqlConnection = new SqlConnection(connString);
                    sqlConnection.Open();
                    if (sqlConnection.State == System.Data.ConnectionState.Open)
                    {
                        var apps = await Apps.GetAllApps(new Settings()
                        {
                            Server = txtServer.Text,
                            Username = txtUsername.Password,
                            Password = txtPassword.Password
                        });
                        cmbApps.ItemsSource = apps;
                        if (AppId != 0)
                        {
                            cmbApps.SelectedItem = apps.FirstOrDefault(a => a.AppId == AppId);
                        }
                        cmbApps.Items.Refresh();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                    //throw;
                }

            }
            else
            {
                return false;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            //sw.WriteLine("Server=" + txtServer.Text);
            //sw.WriteLine("Username=" + txtUsername.Password);
            //sw.WriteLine("Password=" + txtPassword.Password);
            if (cmbApps.SelectedIndex != -1)
            {
                var selectedApp = cmbApps.SelectedItem as Apps;

                //sw.Close();
                RegistryKey sadcKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\SPWS\AppDeploymentClient");
                string idString = "";
                if (sadcKey == null)
                {
                    sadcKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\SPWS\AppDeploymentClient");
                }
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\settings.ini"))
                {
                    using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "\\settings.ini"))
                    {
                        idString = sr.ReadLine().Split('=')[1];

                    }
                }
                else
                {
                    int i = 0;
                    RegistryKey rk;
                    do
                    {
                        i++;
                        rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\SPWS\AppDeploymentClient\" + i);
                    } while (rk != null);
                    rk = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\SPWS\AppDeploymentClient\" + i);
                    using (StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\settings.ini"))
                    {
                        sw.WriteLine("ADCId=" + i);
                    }
                    rk.SetValue("Server", txtServer.Text);
                    rk.SetValue("Username", txtUsername.Password);
                    rk.SetValue("Password", txtPassword.Password);
                    rk.SetValue("AppId", selectedApp.AppId);

                }

                DialogResult = true;
            }
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (settings.AppId != 0)
            {
                txtServer.Text = settings.Server;
                txtPassword.Password = settings.Password;
                txtUsername.Password = settings.Username;
            }
        }

        private async void BtnValidate_Click(object sender, RoutedEventArgs e)
        {
            if (await Validate())
            {
                MessageBox.Show("Connection opened on " + txtServer.Text, "Connection Success", MessageBoxButton.OK);
            }
        }
    }
}
