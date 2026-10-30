using System.Globalization;

namespace Ml.Models
{
    public class ShopList
    {
        public bool active { get; set; }
        public int id { get; set; }
        public string listDate { get; set; } = "";
        [NoDb]
        public DateTime listDateAsDate
        {
            get {
                var ukCulture = new CultureInfo("en-GB");
                try
                {
                    DateTime parsedDate = DateTime.Parse(listDate, ukCulture);
                    return parsedDate;
                }
                catch { return DateTime.MinValue; }
            }
        }
    }
}
