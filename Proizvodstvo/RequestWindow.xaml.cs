using System;
using System.Linq;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Proizvodstvo.models;

namespace Proizvodstvo
{
    
    public class ProductDisplayModel : INotifyPropertyChanged
    {
        private int _quantity;
        public int ProductId { get; set; }
        public decimal MinPrice { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value >= 0 && _quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(TotalCost)); 
                }
            }
        }

        public decimal TotalCost => Math.Max(0m, Math.Round(Quantity * MinPrice, 2));

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class RequestWindow : Window
    {
        private int? _requestId;
        private int? _partnerId;

        
        private ObservableCollection<ProductDisplayModel> _requestProducts = new ObservableCollection<ProductDisplayModel>();

        public RequestWindow(int? requestId)
        {
            InitializeComponent();
            _requestId = requestId;
            ProductsGrid.ItemsSource = _requestProducts;

            LoadComboBoxes();

            if (_requestId.HasValue)
            {
                Title = "Редактирование заявки";
                LoadRequestDetails(_requestId.Value);
            }
        }

        private void LoadComboBoxes()
        {
            using (var db = new proizvodstoEntities())
            {
                CmbType.ItemsSource = db.partner_types.ToList();
                CmbProducts.ItemsSource = db.products.ToList();
            }
        }

        private void LoadRequestDetails(int requestId)
        {
            using (var db = new proizvodstoEntities())
            {
                var request = db.requests
                    .Include("partners")
                    .Include("request_products.products")
                    .FirstOrDefault(r => r.id == requestId);

                if (request != null && request.partners != null)
                {
                    _partnerId = request.partner_id;
                    var partner = request.partners;

                    CmbType.SelectedValue = partner.type_id;
                    TxtName.Text = partner.name;
                    TxtDirector.Text = $"{partner.Director_last_name} {partner.Director_first_name} {partner.Director_patronymic}".Trim();
                    TxtPhone.Text = partner.phone_number;
                    TxtRating.Text = partner.rating.ToString();

                    if (request.request_products != null)
                    {
                        foreach (var rp in request.request_products)
                        {
                            _requestProducts.Add(new ProductDisplayModel
                            {
                                ProductId = rp.product_id,
                                Quantity = rp.quantity,
                                MinPrice = rp.products.min_price
                            });
                        }
                    }
                }
            }
        }

        // Добавление товара
        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (CmbProducts.SelectedItem is products selectedProduct)
            {
                if (!int.TryParse(TxtQuantity.Text, out int qty) || qty <= 0)
                {
                    MessageBox.Show("Введите корректное количество.");
                    return;
                }

                // Проверка, нет ли этого товара в заявке\
                
                var existingItem = _requestProducts.FirstOrDefault(p => p.ProductId == selectedProduct.id);
                if (existingItem != null)
                {
                    existingItem.Quantity += qty;
                }
                else
                {
                    _requestProducts.Add(new ProductDisplayModel
                    {
                        ProductId = selectedProduct.id,
                        MinPrice = selectedProduct.min_price,
                        Quantity = qty
                    });
                }
            }
            else
            {
                MessageBox.Show("Выберите товар из списка.");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtRating.Text, out int rating) || rating < 0)
            {
                MessageBox.Show("Рейтинг должен быть положительным числом.");
                return;
            }

            string[] names = TxtDirector.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string lastName = names.Length > 0 ? names[0] : "";
            string firstName = names.Length > 1 ? names[1] : "";
            string patronymic = names.Length > 2 ? names[2] : "";

            using (var db = new proizvodstoEntities())
            {
                requests targetRequest;

                if (!_requestId.HasValue)
                {
                    var newPartner = new partners
                    {
                        type_id = CmbType.SelectedValue != null ? (int)CmbType.SelectedValue : 1,
                        name = TxtName.Text,
                        Director_last_name = lastName,
                        Director_first_name = firstName,
                        Director_patronymic = patronymic,
                        phone_number = TxtPhone.Text,
                        email = "info@partner.com",
                        rating = rating,
                        id_adress = 1,
                        TIN = "0000000000"
                    };
                    db.partners.Add(newPartner);
                    db.SaveChanges();

                    targetRequest = new requests
                    {
                        partner_id = newPartner.id,
                        created_at = DateTime.Now,
                        id_status = 1
                    };
                    db.requests.Add(targetRequest);
                    db.SaveChanges(); 
                }
                else
                {
                    targetRequest = db.requests.FirstOrDefault(r => r.id == _requestId.Value);

                    var partner = db.partners.FirstOrDefault(p => p.id == _partnerId.Value);
                    if (partner != null)
                    {
                        partner.type_id = CmbType.SelectedValue != null ? (int)CmbType.SelectedValue : partner.type_id;
                        partner.name = TxtName.Text;
                        partner.Director_last_name = lastName;
                        partner.Director_first_name = firstName;
                        partner.Director_patronymic = patronymic;
                        partner.phone_number = TxtPhone.Text;
                        partner.rating = rating;
                    }

                    var oldProducts = db.request_products.Where(rp => rp.request_id == _requestId.Value).ToList();
                    foreach (var oldProduct in oldProducts)
                    {
                        db.request_products.Remove(oldProduct);
                    }
                }

                
                foreach (var item in _requestProducts)
                {
                    db.request_products.Add(new request_products
                    {
                        request_id = targetRequest.id,
                        product_id = item.ProductId,
                        quantity = item.Quantity,
                        price = item.MinPrice
                    });
                }

                db.SaveChanges();
                DialogResult = true;
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}