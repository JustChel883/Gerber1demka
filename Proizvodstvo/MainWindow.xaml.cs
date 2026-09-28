using System;
using System.Linq;
using System.Windows;
using Proizvodstvo.models;

namespace Proizvodstvo
{
    public class RequestDisplayModel
    {
        public int RequestId { get; set; }
        public string TypeAndName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public int Rating { get; set; }
        public decimal TotalCost { get; set; }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var db = new proizvodstoEntities())
                {
                   
                    var rawRequests = db.requests
                        .Include("partners.partner_types")
                        .Include("partners.adresses.cities")
                        .Include("request_products.products")
                        .ToList();

                    var displayData = rawRequests.Select(r =>
                    {
                        // Расчёт стоимости
                        decimal calculatedCost = 0m;
                        if (r.request_products != null && r.request_products.Any())
                        {
                            calculatedCost = r.request_products.Sum(rp => rp.quantity * rp.products.min_price);
                        }

                        // Строка "Тип | Наименование"
                        string typeName = r.partners?.partner_types?.name ?? "Тип не указан";
                        string partnerName = r.partners?.name ?? "Без названия";

                        // Адрес
                        string address = "Юридический адрес не указан";
                        if (r.partners?.adresses != null)
                        {
                            string city = r.partners.adresses.cities?.city_name ?? "";
                            string street = r.partners.adresses.street ?? "";
                            address = $"{city}, {street}".Trim(new[] { ' ', ',' });
                        }

                        return new RequestDisplayModel
                        {
                            RequestId = r.id,
                            TypeAndName = $"{typeName} | {partnerName}",
                            Address = address,
                            Phone = FormatPhoneNumber(r.partners?.phone_number),
                            Rating = r.partners?.rating ?? 0,
                            TotalCost = Math.Max(0m, Math.Round(calculatedCost, 2))
                        };
                    }).ToList();

                    RequestsList.ItemsSource = displayData;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        // Чтобы номер выглядел вот так +X XXX XXX XX XX
        private string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "Нет телефона";

            
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 11)
            {
                return $"+{digits[0]} {digits.Substring(1, 3)} {digits.Substring(4, 3)} {digits.Substring(7, 2)} {digits.Substring(9, 2)}";
            }
            else if (digits.Length == 10) // Если без кода страны
            {
                return $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)} {digits.Substring(6, 2)} {digits.Substring(8, 2)}";
            }

            return phone;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            RequestWindow window = new RequestWindow(null);
            if (window.ShowDialog() == true) LoadData();
        }

        private void RequestsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (RequestsList.SelectedItem is RequestDisplayModel selected)
            {
                RequestWindow window = new RequestWindow(selected.RequestId);
                if (window.ShowDialog() == true) LoadData();
            }
        }
    }
}