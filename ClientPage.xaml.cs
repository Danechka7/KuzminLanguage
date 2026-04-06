using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace KuzminLanguage
{
    public partial class ClientPage : Page
    {
        private List<Client> _allClients = new List<Client>();
        private List<Client> _filteredClients = new List<Client>();
        private int _pageSize = 10;
        private int _currentPage = 1;
        private string _searchText = "";
        private string _genderFilter = "Все";
        private string _sortType = "Нет";

        public ClientPage()
        {
            InitializeComponent();
            RefreshData();
        }

        private void RefreshData()
        {
            _allClients = BDKuzmin_LanguageSchoolEntities.GetContext().Client.ToList();
            ApplyFiltersAndSearch();
        }

        private void ApplyFiltersAndSearch()
        {
            var result = _allClients.AsEnumerable();
            if (_genderFilter == "Мужской")
                result = result.Where(c => c.GenderCode == "м");
            else if (_genderFilter == "Женский")
                result = result.Where(c => c.GenderCode == "ж");
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                result = result.Where(c =>
                    (c.LastName + " " + c.FirstName + " " + c.Patronymic).ToLower().Contains(_searchText.ToLower()) ||
                    (c.Email ?? "").ToLower().Contains(_searchText.ToLower()) ||
                    (c.currentnumber ?? "").ToLower().Contains(_searchText.ToLower())
                );
            }
            if (_sortType == "По фамилии от А до Я")
                result = result.OrderBy(c => c.LastName);
            else if (_sortType == "По дате последнего посещения")
                result = result.OrderByDescending(c => GetLastVisitDate(c.ID));
            else if (_sortType == "По количеству посещений")
                result = result.OrderByDescending(c => c.CountVisit);

            _filteredClients = result.ToList();
            _currentPage = 1;
            DisplayPage();
        }

        private DateTime GetLastVisitDate(int clientId)
        {
            var lastVisit = BDKuzmin_LanguageSchoolEntities.GetContext().ClientService
                .Where(cs => cs.ClientID == clientId)
                .OrderByDescending(cs => cs.StartTime)
                .FirstOrDefault();

            return lastVisit?.StartTime ?? DateTime.MinValue;
        }

        private void DisplayPage()
        {
            var clients = _pageSize == 0 ? _filteredClients :
                _filteredClients.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();

            ClientListView.ItemsSource = clients;

            int totalPages = _pageSize == 0 ? 1 : (int)Math.Ceiling((double)_filteredClients.Count / _pageSize);

            if (LeftDirButton != null) LeftDirButton.IsEnabled = _currentPage > 1;
            if (RightDirButton != null) RightDirButton.IsEnabled = _currentPage < totalPages;
            if (CountText != null) CountText.Text = $"{clients.Count} из {_filteredClients.Count}";

            var pages = new List<int>();
            for (int i = 1; i <= totalPages; i++)
                pages.Add(i);

            if (PageListBox != null)
            {
                PageListBox.ItemsSource = pages;
                PageListBox.SelectedItem = _currentPage;
            }
        }

        private void PageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageListBox.SelectedItem != null)
            {
                _currentPage = (int)PageListBox.SelectedItem;
                DisplayPage();
            }
        }

        private void LeftDirButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                DisplayPage();
            }
        }

        private void RightDirButton_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = _pageSize == 0 ? 1 : (int)Math.Ceiling((double)_allClients.Count / _pageSize);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                DisplayPage();
            }
        }

        private void PageSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageSizeCombo.SelectedItem == null) return;

            string val = (PageSizeCombo.SelectedItem as ComboBoxItem)?.Content.ToString();
            _pageSize = val == "Все" ? 0 : int.Parse(val);
            _currentPage = 1;
            DisplayPage();
        }


        private void TBoxSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _searchText = TBoxSearch.Text;
            ApplyFiltersAndSearch();
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _sortType = (ComboType.SelectedItem as TextBlock)?.Text ?? "Нет";
            ApplyFiltersAndSearch();
        }

        private void GenderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _genderFilter = (GenderCombo.SelectedItem as TextBlock)?.Text ?? "Все";
            ApplyFiltersAndSearch();
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddEditPage());
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedClient = ClientListView.SelectedItem as Client;
            if (selectedClient == null)
            {
                MessageBox.Show("Выберите клиента для редактирования");
                return;
            }
            NavigationService.Navigate(new AddEditPage(selectedClient));
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var client = ClientListView.SelectedItem as Client;
            if (client == null)
            {
                MessageBox.Show("Выберите клиента");
                return;
            }

            try
            {
                var context = BDKuzmin_LanguageSchoolEntities.GetContext();

                // Проверка на наличие посещений
                if (context.ClientService.Any(cs => cs.ClientID == client.ID))
                {
                    MessageBox.Show("Нельзя удалить - у клиента есть посещения");
                    return;
                }

                if (MessageBox.Show($"Удалить {client.LastName} {client.FirstName}?", "Подтверждение",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    context.Client.Remove(client);
                    context.SaveChanges(); // Убрал async/await
                    MessageBox.Show("Клиент удален");
                    RefreshData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}