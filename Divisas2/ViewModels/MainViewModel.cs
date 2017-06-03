using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Windows.Input;
using Divisas2.Models;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;

namespace Divisas2.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Attributes
        private bool isRunning;
        private bool isEnabled;
        private string message;
        private ExchangeRates exchangeRates;
        #endregion

        #region Properties
        public ObservableCollection<Rate> Rates 
        { 
            get; 
            set; 
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

        public bool IsEnabled
        {
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsEnabled"));
                }
            }
            get
            {
                return isEnabled;
            }
        }

		public string Message
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

		public decimal Amount
        {
            get;
            set;
        }

        public double SourceRate
        {
            get;
            set;
        }

        public double TargetRate
        {
            get;
            set;
        }

        #endregion

        #region Constructors
        public MainViewModel()
        {
            Rates = new ObservableCollection<Rate>();

            GetRates();
        }
        #endregion

        #region Methods
        private async void GetRates()
        {
            IsRunning = true;
            IsEnabled = false;

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
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "Aceptar");
                IsRunning = false;
                IsEnabled = false;
                return;
            }

            LoadRates();
            IsRunning = false;
            IsEnabled = true;
        }

        private void LoadRates()
        {
            Rates.Clear();
            var type = typeof(Rates);
            var properties = type.GetRuntimeFields();

            foreach (var property in properties)
            {
                var code = property.Name.Substring(1, 3);
                Rates.Add(new Rate
                {
                    Code = code,
                    TaxRate = (double)property.GetValue(exchangeRates.Rates),
                });
            }
        }
        #endregion

        #region Commands
        public ICommand ConvertMoneyCommand
        {
            get { return new RelayCommand(ConvertMoney); }
        }

		private async void ConvertMoney()
		{
			if (Amount <= 0)
			{
				await App.Current.MainPage.DisplayAlert(
                    "Error", 
                    "Debes ingresar un valor a convertir", 
                    "Aceptar");
				return;
			}

			if (SourceRate == 0)
			{
				await App.Current.MainPage.DisplayAlert(
                    "Error", 
                    "Debes seleccionar la moneda origen", 
                    "Aceptar");
				return;
			}

			if (TargetRate == 0)
			{
				await App.Current.MainPage.DisplayAlert(
                    "Error", 
                    "Debes seleccionar la moneda destino", 
                    "Aceptar");
				return;
			}

			decimal amountConverted = Amount / (decimal)SourceRate * (decimal)TargetRate;

			Message = string.Format("{0:N2} = {1:N2}", Amount, amountConverted);
		}
		#endregion
	}
}
