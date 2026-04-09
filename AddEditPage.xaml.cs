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
            // При добавлении - скрываем ID
            IdLabel.Visibility = Visibility.Collapsed;
            TBoxID.Visibility = Visibility.Collapsed;
        }

        public AddEditPage(Client selectedClient)
        {
            InitializeComponent();
            _currentClient = selectedClient;
            _isEditMode = true;
            // При редактировании - показываем ID только для чтения
            IdLabel.Visibility = Visibility.Visible;
            TBoxID.Visibility = Visibility.Visible;
            TBoxID.IsReadOnly = true;
            LoadClientData();
        }

        private void LoadClientData()
        {
            TBoxID.Text = _currentClient.ID.ToString();
            TBoxID.IsReadOnly = true;
            TBoxID.Text = _currentClient.ID.ToString();
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
                if (string.IsNullOrEmpty(path))
                {
                    ClientImage.Source = null;
                    return;
                }

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();

                // Проверяем, является ли путь абсолютным или относительным
                if (path.StartsWith("/") || path.StartsWith("Клиенты") || path.StartsWith("res"))
                {
                    // Относительный путь - строим полный
                    string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                    if (System.IO.File.Exists(fullPath))
                    {
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    }
                    else
                    {
                        // Пробуем найти файл
                        string[] possiblePaths = {
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Клиенты", System.IO.Path.GetFileName(path)),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "Клиенты", System.IO.Path.GetFileName(path)),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path)
                };

                        string existingPath = possiblePaths.FirstOrDefault(p => System.IO.File.Exists(p));
                        if (existingPath != null)
                        {
                            bitmap.UriSource = new Uri(existingPath, UriKind.Absolute);
                        }
                        else
                        {
                            ClientImage.Source = null;
                            return;
                        }
                    }
                }
                else if (System.IO.File.Exists(path))
                {
                    // Абсолютный путь
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                }
                else
                {
                    ClientImage.Source = null;
                    return;
                }

                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ClientImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                ClientImage.Source = null;
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
                // Только английские буквы, цифры и точки до @, после @ только английские буквы и точки
                Regex emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                if (!emailRegex.IsMatch(TBoxEmail.Text))
                {
                    MessageBox.Show("Введите корректный email (только английские буквы, пример: name@mail.ru)");
                    return false;
                }
            }

            // Проверка телефона (цифры, +, -, (), пробел)
            // Проверка телефона (только цифры и символы: +, -, (), пробел)
            // Проверка телефона (ровно 10 цифр)
            if (string.IsNullOrWhiteSpace(TBoxPhone.Text))
            {
                MessageBox.Show("Введите номер телефона!");
                return false;
            }

            // Запрещаем русские буквы
            Regex rusRegex = new Regex(@"[а-яА-ЯёЁ]");
            if (rusRegex.IsMatch(TBoxPhone.Text))
            {
                MessageBox.Show("Номер телефона не может содержать русские буквы!");
                return false;
            }

            // Разрешаем только цифры и символы: +, -, (, ), пробел
            Regex phoneRegex = new Regex(@"^[\d\+\-\s\(\)]+$");
            if (!phoneRegex.IsMatch(TBoxPhone.Text))
            {
                MessageBox.Show("Телефон может содержать только цифры и символы: +, -, (), пробел");
                return false;
            }

            // Подсчитываем количество цифр в номере
            int digitCount = TBoxPhone.Text.Count(char.IsDigit);
            if (digitCount != 10)
            {
                MessageBox.Show($"Номер телефона должен содержать ровно 10 цифр! Сейчас {digitCount} цифр.");
                return false;
            }

            // Проверка даты рождения
            if (BirthDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату рождения");
                return false;
            }

            if (BirthDate.SelectedDate > DateTime.Today)
            {
                MessageBox.Show("Дата рождения не может быть больше сегодняшней даты!");
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
            var openDialog = new OpenFileDialog();
            openDialog.Filter = "Image files (*.jpg, *.png, *.jpeg)|*.jpg;*.png;*.jpeg|All files (*.*)|*.*";

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    string selectedFile = openDialog.FileName;

                    // Создаем уникальное имя для фото
                    string photosDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Клиенты");
                    if (!System.IO.Directory.Exists(photosDir))
                        System.IO.Directory.CreateDirectory(photosDir);

                    // Используем временное уникальное имя
                    string extension = System.IO.Path.GetExtension(selectedFile);
                    string newFileName = $"temp_{DateTime.Now.Ticks}{extension}";
                    string destPath = System.IO.Path.Combine(photosDir, newFileName);

                    // Копируем файл
                    System.IO.File.Copy(selectedFile, destPath, true);

                    // Сохраняем путь
                    _selectedPhotoPath = destPath;

                    // Загружаем изображение
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(destPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ClientImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}");
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

                // Сохраняем фото - просто берем путь
                if (!string.IsNullOrEmpty(_selectedPhotoPath))
                {
                    _currentClient.PhotoPath = _selectedPhotoPath;
                }
                else if (string.IsNullOrEmpty(_currentClient.PhotoPath))
                {
                    _currentClient.PhotoPath = System.IO.Path.Combine("Клиенты", "picture.png");
                }

                context.SaveChanges();
                MessageBox.Show("Данные успешно сохранены!");
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
        private void ComboGender_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}