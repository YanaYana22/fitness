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
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private DatabaseService _dbService;

        public LoginWindow()
        {
            InitializeComponent();
            _dbService = new DatabaseService("localhost", "fitness", "postgres", "password123");
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Password;

            var query = $@"
            SELECT u.user_id, u.role_id, r.role_name 
            FROM users u
            JOIN roles r ON u.role_id = r.role_id
            WHERE username = '{username}' AND password = '{password}'";

            try
            {
                var result = _dbService.ExecuteQuery(query);

                if (result.Rows.Count == 1)
                {
                    int roleId = Convert.ToInt32(result.Rows[0]["role_id"]);
                    int userId = Convert.ToInt32(result.Rows[0]["user_id"]);

                    OpenRoleWindow(roleId, userId);
                    this.Close();
                }
                else
                {
                    lblError.Text = "Неверный логин или пароль";
                }
            }
            catch (Exception ex)
            {
                lblError.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void OpenRoleWindow(int roleId, int userId)
        {
            switch (roleId)
            {
                case 1: // Админ
                    new AdminWindow(userId).Show();
                    break;
                case 2: // Тренер
                    new TrainerWindow(userId).Show();
                    break;
                case 3: // Клиент
                    new ClientWindow(userId).Show();
                    break;
            }
        }
    }
}
