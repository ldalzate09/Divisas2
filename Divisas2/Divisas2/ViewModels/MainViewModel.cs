using Divisas2.Models;
using Divisas2.Services;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Windows.Input;

namespace Divisas2.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Attributes
        private ExchangeRates exchangeRates;
        private RatesDescrption ratesDescription;
        private Rate rate;
        private ApiService apiService;
        private DialogService dialogService;
        private DataService dataService;
        private bool isRunning;
        private bool isEnabled;
        private String message;
        private double sourceRate;
        private double targetRate;
        private Rate origenRate;
        private Rate destinoRate;
        #endregion

        #region Properties
        public ObservableCollection<Rate> Rates { get; set; }
        public decimal Amount { get; set; }
        public double SourceRate {
            set
            {
                if (sourceRate != value)
                {
                    sourceRate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceRate"));
                }
            }
            get
            {
                return sourceRate;
            }
        }
        public double TargetRate {
            set
            {
                if (targetRate != value)
                {
                    targetRate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetRate"));
                }
            }
            get
            {
                return targetRate;
            }
        }

        public bool IsRunning
        {
            set
            {
                if (isRunning != value)
                {
                    isRunning = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRunning"));
                }
            }
            get
            {
                return isRunning;
            }
        }

        public String Message
        {
            set
            {
                if (message != value)
                {
                    message = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Message"));
                }
            }
            get
            {
                return message;
            }
        }

        public bool IsEnabled
        {
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("isEnabled"));
                }
            }
            get
            {
                return isEnabled;
            }
        }

        public Rate OrigenRate
        {
            set
            {
                if (origenRate != value)
                {
                    origenRate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OrigenRate"));
                }
            }
            get
            {
                return origenRate;
            }
        }

        public Rate DestinoRate
        {
            set
            {
                if (destinoRate != value)
                {
                    destinoRate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DestinoRate"));
                }
            }
            get
            {
                return destinoRate;
            }
        }
        #endregion

        #region Constructors
        public MainViewModel()
        {
            Rates = new ObservableCollection<Rate>();
            apiService = new ApiService();
            dialogService = new DialogService();
            dataService = new DataService();

            GetModeRates();
        }
        #endregion

        #region Methods
        private async void GetModeRates()
        {
            IsRunning = true;
            IsEnabled = true;

            var checkConnetion = await apiService.CheckConnection();
            if (!checkConnetion.IsSuccess)
            {
                rate = dataService.First<Rate>(false);
                if (rate != null)
                {
                    //await App.Current.MainPage.DisplayAlert("Informacion", "Las tasas a convertir no estan actualizadas", "Accept");
                    LoadRatesOffLine();
                    IsRunning = false;
                    IsEnabled = true;
                }
                else
                {
                    IsRunning = false;
                    IsEnabled = false;
                    await dialogService.ShowMessage("Error", checkConnetion.Message);
                    return;
                }
            }
            else
            {
                GetRates();
            }
        }

        private async void GetRates()
        {
            IsRunning = true;
            IsEnabled = true;

            try
            {          
                var client = new HttpClient();
                client.BaseAddress = new Uri("https://openexchangerates.org");
                var url = "/api/latest.json?app_id=f490efbcd52d48ee98fd62cf33c47b9e";
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    await App.Current.MainPage.DisplayAlert("Error", response.StatusCode.ToString(), "Aceptar");
                    IsRunning = false;
                    IsEnabled = false;
                    return;
                }

                var result = await response.Content.ReadAsStringAsync();
                exchangeRates = JsonConvert.DeserializeObject<ExchangeRates>(result);

                // Traer las descripción de las tasas
                client.BaseAddress = new Uri("https://gist.githubusercontent.com");
                url = "picodotdev/88512f73b61bc11a2da4/raw/9407514be22a2f1d569e75d6b5a58bd5f0ebbad8";
                response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    await App.Current.MainPage.DisplayAlert("Error", response.StatusCode.ToString(), "Aceptar");
                    IsRunning = false;
                    IsEnabled = false;
                    return;
                }

                result = await response.Content.ReadAsStringAsync();
                ratesDescription = JsonConvert.DeserializeObject<RatesDescrption>(result);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "Aceptar");
                IsRunning = false;
                IsEnabled = false;
                return;
            }

            LoadRatesOnLine();
            IsRunning = false;
            IsEnabled = true;
        }

        private void LoadRatesOnLine()
        {
            Rates.Clear();
            var type = typeof(Rates);
            var properties = type.GetRuntimeFields();
            var typeDescription = typeof(RatesDescrption);
            var propertiesDescription = typeDescription.GetRuntimeFields();

            foreach (var property in properties)
            {
                var code = property.Name.Substring(1, 3);
                foreach(var description in propertiesDescription)
                {
                    var codeDescription = description.Name.Substring(1, 3);
                    if (code.Equals(codeDescription))
                    {
                        rate = new Rate();
                        rate.Code = code;
                        rate.TaxRate = (double)property.GetValue(exchangeRates.Rates);
                        rate.Name = code + " - " + (string)description.GetValue(ratesDescription);

                        //Almacenar en BD
                        dataService.InsertOrUpdate(rate);
                        // Se carga a la ObservableCollection
                        Rates.Add(rate);
                        break;
                    }
                }
            }
        }

        private void LoadRatesOffLine()
        {
            Rates.Clear();
            List<Rate> rateList = new List<Rate>();
            rateList = dataService.Get<Rate>(false);

            for (int i = 0; i < rateList.Count; i++)
            {
                Rates.Add(rateList[i]);
            }
        }
        #endregion

        #region Commands
        public ICommand ChangeCommand
        {
            get { return new RelayCommand(Change); }
        }

        public void Change()
        {
            var aux = SourceRate;
            SourceRate = TargetRate;
            TargetRate = aux;
        }

        public ICommand ConvertCommand
        {
            get { return new RelayCommand(ConvertMoney); }
        }

        private async void ConvertMoney()
        {
            if (Amount <= 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Debes ingresar un valor a convertir", "Aceptar");
                return;
            }

            if (SourceRate == 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Debes seleccionar la moneda origen", "Aceptar");
                return;
            }

            if (TargetRate == 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Debes seleccionar la moneda destino", "Aceptar");
                return;
            }
            // Se almacena el Item Seleccionado
            dataService.InsertOrUpdate(rate);

            decimal amountConverted = Amount / (decimal)SourceRate * (decimal)TargetRate;
            Message = string.Format("{0:N2} = {1:N2}", Amount, amountConverted);
        }
        #endregion
    }
}
