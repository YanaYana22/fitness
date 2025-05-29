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
    /// Логика взаимодействия для ClientEditWindow.xaml
    /// </summary>
    public partial class ClientEditWindow : Window
    {
        public int ClientId { get; private set; }
        public string Username { get; private set; }
        public string FullName { get; private set; }
        public string Phone { get; private set; }
        public int MembershipTypeId { get; private set; }

        public ClientEditWindow(int clientId = 0, string username = "", string fullName = "", string phone = "", int membershipTypeId = 1)
        {
            InitializeComponent();
            ClientId = clientId;
            txtFullName.Text = fullName;
            txtPhone.Text = phone;
            cmbMembershipType.SelectedValue = membershipTypeId;

            // Загрузка типов абонементов
            var db = new DatabaseService("localhost", "fitness", "postgres", "password123");
            cmbMembershipType.ItemsSource = db.ExecuteQuery("SELECT * FROM membership_types").DefaultView;
            cmbMembershipType.DisplayMemberPath = "type_name";
            cmbMembershipType.SelectedValuePath = "membership_type_id";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            FullName = txtFullName.Text;
            Phone = txtPhone.Text;
            MembershipTypeId = (int)cmbMembershipType.SelectedValue;

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
