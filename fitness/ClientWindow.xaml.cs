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
    /// Логика взаимодействия для ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window
    {
        private DatabaseService _dbService;
        private int _currentClientId;
        private int _currentUserId;

        public ClientWindow(int userId)
        {
            InitializeComponent();
            _dbService = new DatabaseService("localhost", "fitness", "postgres", "password123");

            // Получаем client_id по user_id
            string query = $"SELECT client_id FROM clients WHERE user_id = {userId}";
            var result = _dbService.ExecuteQuery(query);

            if (result.Rows.Count == 0)
            {
                MessageBox.Show("Ошибка: клиент не найден!");
                Close();
                return;
            }

            _currentClientId = Convert.ToInt32(result.Rows[0]["client_id"]);
            _currentUserId = userId;

            LoadClientData();
            LoadAvailableWorkouts();
            LoadMyWorkouts();
        }

        private void LoadClientData()
        {
            string query = $@"
                SELECT u.full_name, u.phone, u.email, 
                       mt.type_name, mt.duration_days, c.registration_date
                FROM clients c
                JOIN users u ON c.user_id = u.user_id
                LEFT JOIN membership_types mt ON c.membership_type_id = mt.membership_type_id
                WHERE c.client_id = {_currentClientId}";

            try
            {
                var data = _dbService.ExecuteQuery(query).Rows[0];

                txtFullName.Text = data["full_name"].ToString();
                txtPhone.Text = data["phone"].ToString();
                txtEmail.Text = data["email"].ToString();

                string membershipInfo = data["type_name"]?.ToString() ?? "Абонемент не выбран";
                if (data["registration_date"] != DBNull.Value && data["duration_days"] != DBNull.Value)
                {
                    DateTime regDate = Convert.ToDateTime(data["registration_date"]);
                    int duration = Convert.ToInt32(data["duration_days"]);
                    DateTime expiryDate = regDate.AddDays(duration);
                    membershipInfo += $" (действителен до {expiryDate:dd.MM.yyyy})";
                }

                txtMembership.Text = membershipInfo;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadAvailableWorkouts()
        {
            string query = $@"
                SELECT ws.session_id, wt.name as workout_name, 
                       ws.start_time, g.name as gym_name,
                       u.full_name as trainer_name,
                       (ws.max_participants - ws.current_participants) as free_slots
                FROM workout_sessions ws
                JOIN workout_types wt ON ws.workout_type_id = wt.workout_type_id
                JOIN trainers t ON ws.trainer_id = t.trainer_id
                JOIN users u ON t.user_id = u.user_id
                JOIN gyms g ON ws.gym_id = g.gym_id
                WHERE ws.start_time > NOW()
                AND ws.session_id NOT IN (
                    SELECT session_id FROM client_workouts 
                    WHERE client_id = {_currentClientId}
                )
                AND (ws.max_participants - ws.current_participants) > 0";

            try
            {
                dgAvailableWorkouts.ItemsSource = _dbService.ExecuteQuery(query).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки тренировок: {ex.Message}");
            }
        }

        private void LoadMyWorkouts()
        {
            string query = $@"
                SELECT ws.session_id, wt.name as workout_name, 
                       ws.start_time, g.name as gym_name,
                       u.full_name as trainer_name
                FROM client_workouts cw
                JOIN workout_sessions ws ON cw.session_id = ws.session_id
                JOIN workout_types wt ON ws.workout_type_id = wt.workout_type_id
                JOIN trainers t ON ws.trainer_id = t.trainer_id
                JOIN users u ON t.user_id = u.user_id
                JOIN gyms g ON ws.gym_id = g.gym_id
                WHERE cw.client_id = {_currentClientId}
                AND ws.start_time > NOW()";

            try
            {
                dgMyWorkouts.ItemsSource = _dbService.ExecuteQuery(query).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}");
            }
        }

        private void BtnSignUp_Click(object sender, RoutedEventArgs e)
        {
            if (dgAvailableWorkouts.SelectedItem == null)
            {
                MessageBox.Show("Выберите тренировку!");
                return;
            }

            DataRowView row = (DataRowView)dgAvailableWorkouts.SelectedItem;
            int sessionId = (int)row["session_id"];

            string insertQuery = $@"
                INSERT INTO client_workouts (client_id, session_id)
                VALUES ({_currentClientId}, {sessionId});
                
                UPDATE workout_sessions 
                SET current_participants = current_participants + 1 
                WHERE session_id = {sessionId}";

            try
            {
                _dbService.ExecuteNonQuery(insertQuery);
                MessageBox.Show("Вы успешно записаны на тренировку!");

                // Обновляем данные
                LoadAvailableWorkouts();
                LoadMyWorkouts();
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
            {
                MessageBox.Show("Вы уже записаны на эту тренировку!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка записи: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (dgMyWorkouts.SelectedItem == null)
            {
                lblStatus.Text = "Выберите тренировку для отмены!";
                return;
            }

            DataRowView row = (DataRowView)dgMyWorkouts.SelectedItem;
            int sessionId = (int)row["session_id"];

            if (MessageBox.Show("Отменить запись на тренировку?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string deleteQuery = $@"
                    DELETE FROM client_workouts 
                    WHERE client_id = {_currentClientId} AND session_id = {sessionId};
                    
                    UPDATE workout_sessions 
                    SET current_participants = current_participants - 1 
                    WHERE session_id = {sessionId}";

                try
                {
                    _dbService.ExecuteNonQuery(deleteQuery);
                    lblStatus.Text = "Запись на тренировку отменена!";

                    // Обновляем данные
                    LoadAvailableWorkouts();
                    LoadMyWorkouts();
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void BtnEditContacts_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClientEditContactsWindow(
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
                    LoadClientData();
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