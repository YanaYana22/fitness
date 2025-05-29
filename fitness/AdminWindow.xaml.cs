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
    /// Логика взаимодействия для AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        private DatabaseService _dbService;
        private int _adminId;

        public AdminWindow(int adminId)
        {
            InitializeComponent();
            _adminId = adminId;
            _dbService = new DatabaseService("localhost", "fitness", "postgres", "password123");
            LoadAllData();
        }

        private void LoadAllData()
        {
            LoadClients();
            LoadTrainers();
            LoadWorkouts();
        }

        // === Клиенты ===
        private void LoadClients()
        {
            string query = @"
                SELECT c.client_id, u.full_name, mt.type_name, u.phone
                FROM clients c
                JOIN users u ON c.user_id = u.user_id
                LEFT JOIN membership_types mt ON c.membership_type_id = mt.membership_type_id";

            dgClients.ItemsSource = _dbService.ExecuteQuery(query).DefaultView;
        }

        private void BtnAddClient_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClientEditWindow();
            if (dialog.ShowDialog() == true)
            {
                string query = $@"
                    INSERT INTO users (role_id, full_name, phone)
                    VALUES (3, '{dialog.FullName}', '{dialog.Phone}');
                    
                    INSERT INTO clients (user_id, membership_type_id)
                    VALUES (currval('users_user_id_seq'), {dialog.MembershipTypeId})";

                _dbService.ExecuteNonQuery(query);
                LoadClients();
            }
        }

        private void BtnEditClient_Click(object sender, RoutedEventArgs e)
        {
            if (dgClients.SelectedItem == null)
            {
                lblClientStatus.Text = "Выберите клиента!";
                return;
            }

            DataRowView row = (DataRowView)dgClients.SelectedItem;
            var dialog = new ClientEditWindow(
                clientId: (int)row["client_id"],
                fullName: row["full_name"].ToString(),
                phone: row["phone"].ToString(),
                membershipTypeId: 1 // В реальном коде нужно получать из БД
            );

            if (dialog.ShowDialog() == true)
            {
                string query = $@"
                    UPDATE clients SET membership_type_id = {dialog.MembershipTypeId} 
                    WHERE client_id = {dialog.ClientId};
                    
                    UPDATE users SET
                        full_name = '{dialog.FullName}',
                        phone = '{dialog.Phone}'
                    WHERE user_id = (SELECT user_id FROM clients WHERE client_id = {dialog.ClientId})";

                _dbService.ExecuteNonQuery(query);
                LoadClients();
            }
        }

        private void BtnDeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (dgClients.SelectedItem == null)
            {
                lblClientStatus.Text = "Выберите клиента!";
                return;
            }

            DataRowView row = (DataRowView)dgClients.SelectedItem;
            int clientId = (int)row["client_id"];

            if (MessageBox.Show("Удалить клиента и все связанные данные?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    // Получаем user_id перед удалением
                    string getUserIdQuery = $"SELECT user_id FROM clients WHERE client_id = {clientId}";
                    var userIdResult = _dbService.ExecuteQuery(getUserIdQuery);

                    if (userIdResult.Rows.Count == 0)
                    {
                        MessageBox.Show("Клиент не найден!");
                        return;
                    }

                    int userId = (int)userIdResult.Rows[0]["user_id"];

                    // Удаляем в правильном порядке (сначала зависимые таблицы)
                    string deleteQuery = $@"
                DELETE FROM client_workouts WHERE client_id = {clientId};
                DELETE FROM clients WHERE client_id = {clientId};
                DELETE FROM users WHERE user_id = {userId}";

                    int affectedRows = _dbService.ExecuteNonQuery(deleteQuery);

                    if (affectedRows > 0)
                    {
                        LoadClients();
                        lblClientStatus.Text = "Клиент и все связанные данные удалены!";
                    }
                    else
                    {
                        lblClientStatus.Text = "Ошибка при удалении!";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        // === Тренеры ===
        private void LoadTrainers()
        {
            string query = @"
                SELECT t.trainer_id, u.full_name, s.name, t.salary
                FROM trainers t
                JOIN users u ON t.user_id = u.user_id
                JOIN specializations s ON t.specialization_id = s.specialization_id";

            dgTrainers.ItemsSource = _dbService.ExecuteQuery(query).DefaultView;
        }

        private void BtnAddTrainer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TrainerEditWindow();
            if (dialog.ShowDialog() == true)
            {
                string query = $@"
                    INSERT INTO users (role_id, full_name, phone)
                    VALUES (2, '{dialog.FullName}', '{dialog.Phone}');
                    
                    INSERT INTO trainers (user_id, specialization_id, salary, hire_date)
                    VALUES (currval('users_user_id_seq'), '{dialog.SpecializationId}', '{dialog.Salary}', CURRENT_DATE)";

                _dbService.ExecuteNonQuery(query);
                LoadTrainers();
            }
        }

        // === Тренеры: Редактирование ===
        private void BtnEditTrainer_Click(object sender, RoutedEventArgs e)
        {
            if (dgTrainers.SelectedItem == null)
            {
                lblTrainerStatus.Text = "Выберите тренера!";
                return;
            }

            DataRowView row = (DataRowView)dgTrainers.SelectedItem;
            int trainerId = (int)row["trainer_id"];

            // Загружаем текущие данные тренера
            string query = $@"
        SELECT u.full_name, u.phone, t.salary, t.specialization_id
        FROM trainers t
        JOIN users u ON t.user_id = u.user_id
        WHERE t.trainer_id = {trainerId}";

            var trainerData = _dbService.ExecuteQuery(query).Rows[0];

            var dialog = new TrainerEditWindow(
                trainerId: trainerId,
                fullName: trainerData["full_name"].ToString(),
                phone: trainerData["phone"].ToString(),
                salary: (decimal)trainerData["salary"],
                specializationId: (int)trainerData["specialization_id"]
            );

            if (dialog.ShowDialog() == true)
            {
                string updateQuery = $@"
            UPDATE trainers SET 
                salary = {dialog.Salary},
                specialization_id = {dialog.SpecializationId}
            WHERE trainer_id = {trainerId};
            
            UPDATE users SET 
                full_name = '{dialog.FullName}',
                phone = {dialog.Phone}
            WHERE user_id = (SELECT user_id FROM trainers WHERE trainer_id = {trainerId})";

                _dbService.ExecuteNonQuery(updateQuery);
                LoadTrainers();
                lblTrainerStatus.Text = "Данные тренера обновлены!";
            }
        }

        // === Тренеры: Удаление ===
        private void BtnDeleteTrainer_Click(object sender, RoutedEventArgs e)
        {
            if (dgTrainers.SelectedItem == null)
            {
                lblTrainerStatus.Text = "Выберите тренера!";
                return;
            }

            DataRowView row = (DataRowView)dgTrainers.SelectedItem;
            int trainerId = (int)row["trainer_id"];

            if (MessageBox.Show("Удалить тренера и все связанные данные?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    // Получаем user_id перед удалением
                    string getUserIdQuery = $"SELECT user_id FROM trainers WHERE trainer_id = {trainerId}";
                    var userIdResult = _dbService.ExecuteQuery(getUserIdQuery);

                    if (userIdResult.Rows.Count == 0)
                    {
                        MessageBox.Show("Тренер не найден!");
                        return;
                    }

                    int userId = (int)userIdResult.Rows[0]["user_id"];

                    // Удаляем в правильном порядке (сначала зависимые таблицы)
                    string deleteQuery = $@"
                DELETE FROM workout_sessions WHERE trainer_id = {trainerId};
                DELETE FROM trainers WHERE trainer_id = {trainerId};
                DELETE FROM users WHERE user_id = {userId}";

                    int affectedRows = _dbService.ExecuteNonQuery(deleteQuery);

                    if (affectedRows > 0)
                    {
                        LoadTrainers();
                        lblTrainerStatus.Text = "Тренер и все связанные данные удалены!";
                    }
                    else
                    {
                        lblTrainerStatus.Text = "Ошибка при удалении!";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        // === Тренировки ===
        private void LoadWorkouts()
        {
            string query = @"
                SELECT ws.session_id, wt.name as workout_name, 
                       u.full_name as trainer_name, g.name as gym_name,
                       ws.start_time
                FROM workout_sessions ws
                JOIN workout_types wt ON ws.workout_type_id = wt.workout_type_id
                JOIN trainers t ON ws.trainer_id = t.trainer_id
                JOIN users u ON t.user_id = u.user_id
                JOIN gyms g ON ws.gym_id = g.gym_id";

            dgWorkouts.ItemsSource = _dbService.ExecuteQuery(query).DefaultView;
        }

        private void BtnAddWorkout_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WorkoutEditWindow();
            if (dialog.ShowDialog() == true)
            {
                string query = $@"
                    INSERT INTO workout_sessions (
                        workout_type_id, trainer_id, gym_id, 
                        start_time, end_time, max_participants
                    ) VALUES (
                        {dialog.WorkoutTypeId}, {dialog.TrainerId}, {dialog.GymId},
                        '{dialog.StartTime:yyyy-MM-dd HH:mm:ss}', '{dialog.EndTime:yyyy-MM-dd HH:mm:ss}', 
                        {dialog.MaxParticipants}
                    )";

                _dbService.ExecuteNonQuery(query);
                LoadWorkouts();
            }
        }

        // === Тренировки: Редактирование ===
        private void BtnEditWorkout_Click(object sender, RoutedEventArgs e)
        {
            if (dgWorkouts.SelectedItem == null)
            {
                lblWorkoutStatus.Text = "Выберите тренировку!";
                return;
            }

            DataRowView row = (DataRowView)dgWorkouts.SelectedItem;
            int sessionId = (int)row["session_id"];

            // Загружаем текущие данные тренировки
            string query = $@"
        SELECT ws.workout_type_id, ws.trainer_id, ws.gym_id, 
               ws.start_time, ws.end_time, ws.max_participants
        FROM workout_sessions ws
        WHERE ws.session_id = {sessionId}";

            var workoutData = _dbService.ExecuteQuery(query).Rows[0];

            var dialog = new WorkoutEditWindow(
                sessionId: sessionId,
                workoutTypeId: (int)workoutData["workout_type_id"],
                trainerId: (int)workoutData["trainer_id"],
                gymId: (int)workoutData["gym_id"],
                startTime: (DateTime)workoutData["start_time"],
                endTime: (DateTime)workoutData["end_time"],
                maxParticipants: (int)workoutData["max_participants"]
            );

            if (dialog.ShowDialog() == true)
            {
                string updateQuery = $@"
            UPDATE workout_sessions SET 
                workout_type_id = {dialog.WorkoutTypeId},
                trainer_id = {dialog.TrainerId},
                gym_id = {dialog.GymId},
                start_time = '{dialog.StartTime:yyyy-MM-dd HH:mm:ss}',
                end_time = '{dialog.EndTime:yyyy-MM-dd HH:mm:ss}',
                max_participants = {dialog.MaxParticipants}
            WHERE session_id = {sessionId}";

                _dbService.ExecuteNonQuery(updateQuery);
                LoadWorkouts();
                lblWorkoutStatus.Text = "Тренировка обновлена!";
            }
        }

        // === Тренировки: Удаление ===
        private void BtnDeleteWorkout_Click(object sender, RoutedEventArgs e)
        {
            if (dgWorkouts.SelectedItem == null)
            {
                lblWorkoutStatus.Text = "Выберите тренировку!";
                return;
            }

            DataRowView row = (DataRowView)dgWorkouts.SelectedItem;
            int sessionId = (int)row["session_id"];

            // Проверка, есть ли записи клиентов
            string checkQuery = $@"
        SELECT COUNT(*) 
        FROM client_workouts 
        WHERE session_id = {sessionId}";

            int clientCount = Convert.ToInt32(_dbService.ExecuteQuery(checkQuery).Rows[0][0]);

            if (clientCount > 0)
            {
                MessageBox.Show("Нельзя удалить тренировку с записанными клиентами!");
                return;
            }

            if (MessageBox.Show("Удалить тренировку?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string deleteQuery = $"DELETE FROM workout_sessions WHERE session_id = {sessionId}";
                _dbService.ExecuteNonQuery(deleteQuery);
                LoadWorkouts();
                lblWorkoutStatus.Text = "Тренировка удалена!";
            }
        }
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }
    }
}
