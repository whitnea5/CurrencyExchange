using FixerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyExchange.Models
{
    public class Transaction
    {
        public Transaction(string Source_Currency, string Destination_Currency,  string Source_Amount)
        {
            this.Source_Currency = Source_Currency;
            this.Destination_Currency = Destination_Currency;
            this.Source_Amount = Source_Amount;
            ConfigureFixer();
        }

        private void ConfigureFixer()
        {
            Fixer.SetApiKey("0386b579beba8865938548fe44841a8d");
        }
        public int ID { get; set; }
        public string Source_Currency { get; set; }
        public string Destination_Currency { get; set; }
        public string Source_Amount { get; set; }
        public string Destination_Amount { get; set; }
        public string FX_Rate { get; set; }

        public Transaction Exchange(Transaction trans)
        {
            ExchangeRate excRate = Fixer.Rate(trans.Source_Currency, trans.Destination_Currency);
            trans.FX_Rate = excRate.Rate.ToString("0.00");
            trans.Destination_Amount = Fixer.Convert(trans.Source_Currency, trans.Destination_Currency, Double.Parse(trans.Source_Amount)).ToString("0.00");
            return trans;
        }
    }
}
