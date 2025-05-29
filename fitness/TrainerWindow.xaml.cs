using System;
using System.Collections.Generic;
using System.Data;
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
    /// Логика взаимодействия для TrainerWindow.xaml
    /// </summary>
    public partial class TrainerWindow : Window
    {
        private DatabaseService _dbService;
        private int _currentTrainerId;
        private int _currentUserId;

        public TrainerWindow(int userId)
        {
            InitializeComponent();
            _dbService = new DatabaseService("localhost", "fitness", "postgres", "password123");

            // Получаем trainer_id по user_id
            string query = $"SELECT trainer_id FROM trainers WHERE user_id = {userId}";
            var result = _dbService.ExecuteQuery(query);

            if (result.Rows.Count == 0)
            {
                MessageBox.Show("Ошибка: тренер не найден!");
                Close();
                return;
            }

            _currentTrainerId = Convert.ToInt32(result.Rows[0]["trainer_id"]);
            _currentUserId = userId;

            LoadTrainerData();
            LoadWorkouts();
        }

        private void LoadTrainerData()
        {
            string query = $@"
                SELECT u.full_name, u.phone, u.email, 
                       s.name as specialization, t.hire_date, t.salary
                FROM trainers t
                JOIN users u ON t.user_id = u.user_id
                JOIN specializations s ON t.specialization_id = s.specialization_id
                WHERE t.trainer_id = {_currentTrainerId}";

            try
            {
                var data = _dbService.ExecuteQuery(query).Rows[0];

                txtFullName.Text = data["full_name"].ToString();
                txtPhone.Text = data["phone"].ToString();
                txtEmail.Text = data["email"].ToString();
                txtSpecialization.Text = data["specialization"].ToString();
                txtHireDate.Text = Convert.ToDateTime(data["hire_date"]).ToString("dd.MM.yyyy");
                txtSalary.Text = $"{Convert.ToDecimal(data["salary"]):N2} руб.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadWorkouts()
        {
            string query = $@"
                SELECT ws.session_id, wt.name as workout_name, 
                       ws.start_time, g.name as gym_name, ws.current_participants
                FROM workout_sessions ws
                JOIN workout_types wt ON ws.workout_type_id = wt.workout_type_id
                JOIN gyms g ON ws.gym_id = g.gym_id
                WHERE ws.trainer_id = {_currentTrainerId}
                AND ws.start_time > NOW()
                ORDER BY ws.start_time";

            try
            {
                dgWorkouts.ItemsSource = _dbService.ExecuteQuery(query).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки тренировок: {ex.Message}");
            }
        }

        private void DgWorkouts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgWorkouts.SelectedItem == null)
            {
                dgClients.ItemsSource = null;
                return;
            }

            DataRowView row = (DataRowView)dgWorkouts.SelectedItem;
            int sessionId = (int)row["session_id"];

            LoadClientsForSession(sessionId);
        }

        private void LoadClientsForSession(int sessionId)
        {
            string query = $@"
                SELECT u.full_name, u.phone
                FROM client_workouts cw
                JOIN clients c ON cw.client_id = c.client_id
                JOIN users u ON c.user_id = u.user_id
                WHERE cw.session_id = {sessionId}";

            try
            {
                dgClients.ItemsSource = _dbService.ExecuteQuery(query).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}");
            }
        }

        private void BtnEditContacts_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EditContactsWindow(
                phone: txtPhone.Text,
                email: txtEmail.Text
            );

            if (dialog.ShowDialog() == true)
            {
                string updateQuery = $@"
                    UPDATE users SET 
                        phone = '{dialog.Phone}',
                        email = '{dialog.Email}'
                    WHERE user_id = {_currentUserId}";

                try
                {
                    _dbService.ExecuteNonQuery(updateQuery);
                    LoadTrainerData();
                    MessageBox.Show("Контактные данные обновлены!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления: {ex.Message}");
                }
            }
        }
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }
    }
}
