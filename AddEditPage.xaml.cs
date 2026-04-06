using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace KuzminLanguage
{
    public partial class AddEditPage : Page
    {
        private Client _currentClient = new Client();
        private bool _isEditMode = false;
        private string _selectedPhotoPath = null;

        public AddEditPage()
        {
            InitializeComponent();

        }

        public AddEditPage(Client selectedClient)
        {
            InitializeComponent();
            _currentClient = selectedClient;
            _isEditMode = true;
            LoadClientData();
        }

        private void LoadClientData()
        {
            TBoxID.Text = _currentClient.ID.ToString();
            TBoxID.IsReadOnly = true;

            TBoxFirstName.Text = _currentClient.LastName;
            TBoxLastName.Text = _currentClient.FirstName;
            TBoxPathronic.Text = _currentClient.Patronymic;
            TBoxEmail.Text = _currentClient.Email;
            TBoxPhone.Text = _currentClient.Phone;
            BirthDate.SelectedDate = _currentClient.Birthday;

            if (_currentClient.GenderCode == "м")
                ComboGender.SelectedIndex = 0;
            else
                ComboGender.SelectedIndex = 1;

            if (!string.IsNullOrEmpty(_currentClient.PhotoPath))
            {
                _selectedPhotoPath = _currentClient.PhotoPath;
                LoadImage(_selectedPhotoPath);
            }
        }

        private void LoadImage(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ClientImage.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
            }
        }

        private bool ValidateFields()
        {
            // Проверка ФИО (только буквы, пробел, дефис)
            Regex nameRegex = new Regex(@"^[a-zA-Zа-яА-ЯёЁ\s\-]+$");

            if (string.IsNullOrWhiteSpace(TBoxFirstName.Text) || !nameRegex.IsMatch(TBoxFirstName.Text))
            {
                MessageBox.Show("Фамилия должна содержать только буквы, пробелы и дефисы");
                return false;
            }
            if (TBoxFirstName.Text.Length > 50)
            {
                MessageBox.Show("Фамилия не может быть длиннее 50 символов");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TBoxLastName.Text) || !nameRegex.IsMatch(TBoxLastName.Text))
            {
                MessageBox.Show("Имя должно содержать только буквы, пробелы и дефисы");
                return false;
            }
            if (TBoxLastName.Text.Length > 50)
            {
                MessageBox.Show("Имя не может быть длиннее 50 символов");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TBoxPathronic.Text) && !nameRegex.IsMatch(TBoxPathronic.Text))
            {
                MessageBox.Show("Отчество должно содержать только буквы, пробелы и дефисы");
                return false;
            }
            if (TBoxPathronic.Text.Length > 50)
            {
                MessageBox.Show("Отчество не может быть длиннее 50 символов");
                return false;
            }

            // Проверка email
            if (!string.IsNullOrWhiteSpace(TBoxEmail.Text))
            {
                Regex emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(TBoxEmail.Text))
                {
                    MessageBox.Show("Введите корректный email");
                    return false;
                }
            }

            // Проверка телефона (цифры, +, -, (), пробел)
            Regex phoneRegex = new Regex(@"^[\d\+\-\s\(\)]+$");
            if (string.IsNullOrWhiteSpace(TBoxPhone.Text) || !phoneRegex.IsMatch(TBoxPhone.Text))
            {
                MessageBox.Show("Телефон может содержать только цифры и символы: +, -, (), пробел");
                return false;
            }

            // Проверка даты рождения
            if (BirthDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату рождения");
                return false;
            }

            // Проверка пола
            if (ComboGender.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите пол");
                return false;
            }

            return true;
        }

        private void SelectPhotoBtn_Click(object sender, RoutedEventArgs e)
        {
            string clientsFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Клиенты");
            var openDialog = new OpenFileDialog()
            {
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp",
                InitialDirectory = clientsFolder
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {

                    string selectedFile = openDialog.FileName;
                    string fileName = System.IO.Path.GetFileName(selectedFile);
                    string destPath = System.IO.Path.Combine(clientsFolder, fileName);

                    if (!System.IO.Path.GetDirectoryName(selectedFile).Equals(clientsFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        // Проверяем дубликаты
                        int i = 1;
                        while (File.Exists(destPath))
                        {
                            string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                            string ext = System.IO.Path.GetExtension(fileName);
                            destPath = System.IO.Path.Combine(clientsFolder, $"{nameWithoutExt}_{i++}{ext}");
                        }
                        File.Copy(selectedFile, destPath);
                    }
                    else
                    {
                        destPath = selectedFile;
                    }

                    string relativePath = System.IO.Path.Combine("Клиенты", System.IO.Path.GetFileName(destPath));
                    _currentClient.PhotoPath = relativePath;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(destPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ClientImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            try
            {
                var context = BDKuzmin_LanguageSchoolEntities.GetContext();

                if (!_isEditMode)
                {
                    _currentClient = new Client();
                    context.Client.Add(_currentClient);
                }

                _currentClient.LastName = TBoxFirstName.Text;
                _currentClient.FirstName = TBoxLastName.Text;
                _currentClient.Patronymic = TBoxPathronic.Text;
                _currentClient.Email = TBoxEmail.Text;
                _currentClient.Phone = TBoxPhone.Text;
                _currentClient.Birthday = BirthDate.SelectedDate.Value;
                _currentClient.GenderCode = ComboGender.SelectedIndex == 0 ? "м" : "ж";
                _currentClient.RegistrationDate = _isEditMode ? _currentClient.RegistrationDate : DateTime.Now;

                if (!string.IsNullOrEmpty(_selectedPhotoPath) && _selectedPhotoPath != _currentClient.PhotoPath)
                {
                    string fileName = $"client_{DateTime.Now.Ticks}_{System.IO.Path.GetFileName(_selectedPhotoPath)}";
                    string destPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "Клиенты", fileName);

                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destPath));
                    File.Copy(_selectedPhotoPath, destPath, true);
                    _currentClient.PhotoPath = destPath;
                }

                context.SaveChanges();
                MessageBox.Show("Данные успешно сохранены!");

                if (NavigationService != null)
                    NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void ComboGender_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}