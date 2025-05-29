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

namespace fitness
{
    /// <summary>
    /// Логика взаимодействия для EditContactsWindow.xaml
    /// </summary>
    public partial class EditContactsWindow : Window
    {
        public string Phone => txtPhone.Text;
        public string Email => txtEmail.Text;

        public EditContactsWindow(string phone = "", string email = "")
        {
            InitializeComponent();
            txtPhone.Text = phone;
            txtEmail.Text = email;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Телефон не может быть пустым!");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
