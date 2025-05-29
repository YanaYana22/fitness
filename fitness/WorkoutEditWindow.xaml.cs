using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Логика взаимодействия для WorkoutEditWindow.xaml
    /// </summary>
    public partial class WorkoutEditWindow : Window
    {
        public int SessionId { get; private set; }
        public int WorkoutTypeId => (int)cmbWorkoutType.SelectedValue;
        public int TrainerId => (int)cmbTrainer.SelectedValue;
        public int GymId => (int)cmbGym.SelectedValue;
        public DateTime StartTime => GetCombinedDateTime();
        public DateTime EndTime => StartTime.AddHours(1); // Фиксированная длительность 1 час
        public int MaxParticipants => int.Parse(txtMaxParticipants.Text);

        public WorkoutEditWindow(int sessionId = 0, int workoutTypeId = 1, int trainerId = 1,
                               int gymId = 1, DateTime startTime = default, DateTime endTime = default,
                               int maxParticipants = 10)
        {
            InitializeComponent();
            SessionId = sessionId;

            LoadComboBoxData();

            cmbWorkoutType.SelectedValue = workoutTypeId;
            cmbTrainer.SelectedValue = trainerId;
            cmbGym.SelectedValue = gymId;

            if (startTime == default) startTime = DateTime.Now.AddDays(1);
            dpDate.SelectedDate = startTime.Date;
            txtTime.Text = startTime.ToString("HH:mm");

            txtMaxParticipants.Text = maxParticipants.ToString();
        }

        private DateTime GetCombinedDateTime()
        {
            if (!DateTime.TryParseExact(txtTime.Text, "HH:mm",
                  CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            {
                throw new FormatException("Некорректный формат времени");
            }

            return dpDate.SelectedDate.Value.Date + time.TimeOfDay;
        }

        private void LoadComboBoxData()
        {
            var db = new DatabaseService("localhost", "fitness", "postgres", "password123");

            cmbWorkoutType.ItemsSource = db.ExecuteQuery("SELECT * FROM workout_types").DefaultView;
            cmbWorkoutType.DisplayMemberPath = "name";
            cmbWorkoutType.SelectedValuePath = "workout_type_id";

            cmbTrainer.ItemsSource = db.ExecuteQuery(@"
                SELECT t.trainer_id, u.full_name 
                FROM trainers t
                JOIN users u ON t.user_id = u.user_id").DefaultView;
            cmbTrainer.DisplayMemberPath = "full_name";
            cmbTrainer.SelectedValuePath = "trainer_id";

            cmbGym.ItemsSource = db.ExecuteQuery("SELECT * FROM gyms").DefaultView;
            cmbGym.DisplayMemberPath = "name";
            cmbGym.SelectedValuePath = "gym_id";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInputs()) return;

                // Проверка времени
                var _ = GetCombinedDateTime(); // Вызовет исключение при ошибке формата

                DialogResult = true;
                Close();
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"Ошибка ввода времени: {ex.Message}\nИспользуйте формат ЧЧ:ММ (например, 14:30)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private bool ValidateInputs()
        {
            if (cmbWorkoutType.SelectedValue == null ||
                cmbTrainer.SelectedValue == null ||
                cmbGym.SelectedValue == null)
            {
                MessageBox.Show("Выберите все значения из списков!");
                return false;
            }

            if (!int.TryParse(txtMaxParticipants.Text, out _) ||
                dpDate.SelectedDate == null)
            {
                MessageBox.Show("Проверьте правильность данных!");
                return false;
            }

            if (dpDate.SelectedDate < DateTime.Today)
            {
                MessageBox.Show("Дата тренировки не может быть в прошлом!");
                return false;
            }

            return true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
