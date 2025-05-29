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
    /// Логика взаимодействия для TrainerEditWindow.xaml
    /// </summary>
    public partial class TrainerEditWindow : Window
    {
        public int TrainerId { get; private set; }
        public string FullName => txtFullName.Text;
        public string Phone => txtPhone.Text;
        public decimal Salary => decimal.Parse(txtSalary.Text);
        public int SpecializationId => (int)cmbSpecialization.SelectedValue;

        public TrainerEditWindow(int trainerId = 0, string fullName = "", string phone = "",
                               decimal salary = 0, int specializationId = 1)
        {
            InitializeComponent();
            TrainerId = trainerId;

            txtFullName.Text = fullName;
            txtPhone.Text = phone;
            txtSalary.Text = salary.ToString();

            LoadSpecializations();
            cmbSpecialization.SelectedValue = specializationId;
        }

        private void LoadSpecializations()
        {
            var db = new DatabaseService("localhost", "fitness", "postgres", "password123");
            var specs = db.ExecuteQuery("SELECT * FROM specializations");
            cmbSpecialization.ItemsSource = specs.DefaultView;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtSalary.Text, out _) || cmbSpecialization.SelectedValue == null)
            {
                MessageBox.Show("Проверьте правильность данных!");
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
