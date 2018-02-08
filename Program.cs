using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Windows.Forms;

namespace TurtleOgre
{
    class Program
    {
        private const string btc = " Ƀ";
        private const string up = "▲";
        private const string down = "▼";
        private static string byteArray = null;

        private const string api = "https://tradeogre.com/api/v1/";
        private const string market = "BTC-TRTL";
        private const string apiTicker = "ticker/" + market;
        private const string apiHistory = "history/" + market;
        private const string apiOrders = "account/orders";
        private const string apiOrderCancel = "order/cancel";
        private const string apiOrderSubmitBuy = "order/buy";
        private const string apiOrderSubmitSell = "order/sell";

        private const string cfgfile = "auth.cfg";
        private const string apiLocation = "https://tradeogre.com/account/settings";

        private const string titleWaiting = "Waiting for key press...";

        private const string donationAddress = "TRTLv1oRF2WBuNUYT9eLb1fhqHFht6nU2fAzuGwoATV23dHMeeBmLbMiatkv3V1iAUVTWduX2HUB8KbWAKqks9bq8xHHyVLf4gr";

        public enum RequestType { GET, POST }

        private const string hintUUID = "UUID pattern: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";
        private const string hintQuantity = "Example Quantity TRTL: 200000";
        private const string hintPrice = "Price in Ƀ (BTC), NOT SATOSHI! Example: 0.00000420";

        [STAThreadAttribute]
        static void Main(string[] args)
        {
            Console.Title = titleWaiting;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Welcome to TurtleOgre CLI\n");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("Donations -> " + donationAddress + "\n");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Format("Public Actions:", 40));
            Console.Write("[T]icker, [H]istory\n");
            Console.Write(Format("Private Actions [AUTH]:", 40));
            Console.Write("[O]rders, [C]ancel, [S]ell, [B]uy\n");
            Console.Write(Format("Other Actions:", 40));
            Console.Write("[D]onate :)");
            Console.WriteLine("\n");

            Auth();

            ConsoleKeyInfo keyinfo;
            do
            {
                keyinfo = Console.ReadKey();
                ConsoleKey key = keyinfo.Key;
                ClearCurrentConsoleLine();
                switch (key)
                {
                    case ConsoleKey.T:
                        Line();
                        Request(APIType.PUBLIC_TICKER);
                        break;
                    case ConsoleKey.H:
                        Line();
                        Request(APIType.PUBLIC_HISTORY);
                        break;
                    case ConsoleKey.O:
                        Line();
                        Request(APIType.PRIVATE_ORDERS, new string[] { "market:" + market });
                        break;
                    case ConsoleKey.L:
                        Line();
                        Login();
                        break;
                    case ConsoleKey.C:
                        Line();
                        Request(APIType.PRIVATE_CANCEL, new string[] { "uuid:" + AskVar("UUID", hintUUID) });
                        break;
                    case ConsoleKey.S:
                        Line();
                        Request(APIType.PRIVATE_SELL, new string[] { "market:" + market, "quantity:" + AskVar("Quantity", hintQuantity), "price:" + AskVar("Price", hintPrice) });
                        break;
                    case ConsoleKey.B:
                        Line();
                        Request(APIType.PRIVATE_BUY, new string[] { "market:" + market, "quantity:" + AskVar("Quantity", hintQuantity), "price:" + AskVar("Price", hintPrice) });
                        break;
                    case ConsoleKey.D:
                        Line();
                        Clipboard.SetText(donationAddress);
                        Console.WriteLine("Address copied to clipboard.");
                        Console.WriteLine();
                        break;
                    default:
                        //Line();
                        break;
                }
            }
            while (keyinfo.Key != ConsoleKey.Escape);
        }

        public class Ticker
        {
            [JsonProperty("initialprice")]
            public string Initialprice { get; set; }
            [JsonProperty("price")]
            public string Price { get; set; }
            [JsonProperty("high")]
            public string High { get; set; }
            [JsonProperty("low")]
            public string Low { get; set; }
            [JsonProperty("volume")]
            public string Volume { get; set; }
        }

        private static String Format(string s, int amt = 20)
        {
            while (s.Length < amt) { s += " "; }
            return s;
        }

        private static void Login()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("KEEP YOUR API KEYS SECRET!");
            Console.WriteLine("Can be found here: " + apiLocation);
            Console.WriteLine();
            Console.Write("Key: ");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            string key = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Secret: ");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            string secret = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            string formattedAuth = String.Format("{0}:{1}", key, secret);
            byteArray = Convert.ToBase64String(Encoding.ASCII.GetBytes(formattedAuth));
            File.WriteAllText(cfgfile, formattedAuth);
            Console.WriteLine("Successfully saved API credentials.");
            Console.WriteLine();
        }

        public class Order
        {
            [JsonProperty("uuid")]
            public string ID { get; set; }
            [JsonProperty("date")]
            public long Date { get; set; }
            [JsonProperty("type")]
            public string Type { get; set; }
            [JsonProperty("price")]
            public string Price { get; set; }
            [JsonProperty("quantity")]
            public string Quantity { get; set; }
            [JsonProperty("market")]
            public string Market { get; set; }
        }

        public static string FromUnixTime(long unixTime)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTime).ToLocalTime().ToString();
        }

        public static void Line()
        {
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine();
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private static async void Request(APICall call, string[] param = null)
        {
            Console.Title = call.Endpoint;

            var client = new HttpClient();

            if (call.Auth)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", byteArray);
            }

            HttpResponseMessage response = null;

            if (call.rt == RequestType.POST)
            {
                KeyValuePair<string, string>[] content = null;
                if (param != null)
                {
                    content = new KeyValuePair<string, string>[param.Length];
                    for (int i = 0; i < param.Length; i++)
                    {
                        content[i] = new KeyValuePair<string, string>(param[i].Split(':')[0], param[i].Split(':')[1]);
                    }
                }
                var requestContent = new FormUrlEncodedContent(content);
                response = await client.PostAsync(call.Endpoint, requestContent);
            }
            else
            {
                response = await client.GetAsync(call.Endpoint);
            }

            HttpContent responseContent = response.Content;
            using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                Handle(call.id, await reader.ReadToEndAsync());
            }
        }

        public class APICall
        {
            public bool Auth;
            public string Endpoint;
            public string id;
            public RequestType rt;
            public APICall(bool auth, string endpoint, RequestType rt)
            {
                this.Auth = auth;
                this.id = endpoint;
                this.Endpoint = api + endpoint;
                this.rt = rt;
            }
        }

        public class APIType
        {
            public static APICall PUBLIC_TICKER = new APICall(false, apiTicker, RequestType.GET);
            public static APICall PUBLIC_HISTORY = new APICall(false, apiHistory, RequestType.GET);
            public static APICall PRIVATE_ORDERS = new APICall(true, apiOrders, RequestType.POST);
            public static APICall PRIVATE_CANCEL = new APICall(true, apiOrderCancel, RequestType.POST);
            public static APICall PRIVATE_BUY = new APICall(true, apiOrderSubmitBuy, RequestType.POST);
            public static APICall PRIVATE_SELL = new APICall(true, apiOrderSubmitSell, RequestType.POST);
        }

        private static string AskVar(string s, string hint)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(Format("Hint: " + hint));
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Format(s));
            return Console.ReadLine();
        }

        private static void Handle(string id, string response)
        {
            Console.Title = titleWaiting;
            switch (id)
            {
                case apiOrders:
                    HandleOrders(response);
                    break;
                case apiOrderCancel:
                    HandleOrderCancel(response);
                    break;
                case apiOrderSubmitSell:
                    HandleOrderSubmit(response);
                    break;
                case apiOrderSubmitBuy:
                    HandleOrderSubmit(response);
                    break;
                case apiTicker:
                    HandleTicker(response);
                    break;
                case apiHistory:
                    HandleHistory(response);
                    break;
            }
        }

        private static void HandleOrders(string response)
        {
            List<Order> orders = JsonConvert.DeserializeObject<List<Order>>(response);
            foreach (Order order in orders)
            {
                ConsoleColor color1 = ConsoleColor.Yellow;
                ConsoleColor color2 = ConsoleColor.DarkYellow;
                string buysell = "?";
                string sym2 = " TRTL";
                string sym1 = btc;
                if (order.Type == "buy")
                {
                    buysell = "Buy";
                    color1 = ConsoleColor.Green;
                    color2 = ConsoleColor.DarkGreen;
                }
                else if (order.Type == "sell")
                {
                    buysell = "Sell";
                    color1 = ConsoleColor.Red;
                    color2 = ConsoleColor.DarkRed;
                }
                Console.ForegroundColor = color1;
                Console.Write(buysell + " Order: ");
                Console.ForegroundColor = color2;
                Console.Write("[" + order.ID + "]\n");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("\t" + Format("Time") + FromUnixTime(order.Date));
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\t" + Format("Price") + order.Price + sym1);
                Console.WriteLine("\t" + Format("Quantity") + order.Quantity + sym2);
                Console.WriteLine("\t" + Format("Total") + (Double.Parse(order.Price.Replace(".", ",")) * Double.Parse(order.Quantity.Replace(".", ","))) + sym1);
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        private static void HandleOrderCancel(string response)
        {
            if (response.Contains("\"success\":true"))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Order successfully canceled.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Error cancelling order.");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }

        private static void HandleOrderSubmit(string response)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            NewOrderResponse nor = JsonConvert.DeserializeObject<NewOrderResponse>(response);
            if (nor.Success)
            {
                Console.WriteLine("Order successfully submitted.");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Balance available: " + nor.BuyBalance + btc + ", " + nor.SellBalance + " TRTL");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(nor.Error);
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }

        public class NewOrderResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }
            [JsonProperty("uuid")]
            public string ID { get; set; }
            [JsonProperty("bnewbalavail")]
            public string BuyBalance { get; set; }
            [JsonProperty("snewbalavail")]
            public string SellBalance { get; set; }
            [JsonProperty("error")]
            public string Error { get; set; }
        }

        private static void HandleTicker(string response)
        {
            Ticker ticker = JsonConvert.DeserializeObject<Ticker>(response);

            Console.WriteLine(Format("Initial price") + ticker.Initialprice + btc);
            Console.WriteLine(Format("Price") + ticker.Price + btc);

            double p = Double.Parse(ticker.Price.Replace(".", ","));
            double i = Double.Parse(ticker.Initialprice.Replace(".", ","));

            double d = 0;
            string sym = up;

            if (p > i)
            { // in plus
                d = (p-i) / p;
                sym = up;
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else if (p < i)
            { // in minus
                d = (i-p) / i;
                sym = down;
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (p == i)
            { // same price
                d = 0;
                Console.ForegroundColor = ConsoleColor.Cyan;
            }

            Console.WriteLine(Format("Change") + String.Format(sym + " {0:P2}", d));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();

            Console.WriteLine(Format("24h High") + ticker.High + btc);
            Console.WriteLine(Format("24h Low") + ticker.Low + btc);

            double h = Double.Parse(ticker.High.Replace(".", ","));
            double l = Double.Parse(ticker.Low.Replace(".", ","));

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(Format("24h Volatility") + String.Format("{0:F8}", h - l).Replace(",", ".") + btc);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();

            Console.WriteLine(Format("Volume") + ticker.Volume + btc);
            Console.WriteLine();
        }

        private static void HandleHistory(string response)
        {
            List<Trade> trades = JsonConvert.DeserializeObject<List<Trade>>(response);
            foreach (Trade trade in trades)
            {
                ConsoleColor color1 = ConsoleColor.Yellow;
                ConsoleColor color2 = ConsoleColor.DarkYellow;
                string buysell = "?";
                string sym2 = " TRTL";
                string sym1 = btc;
                if (trade.Type == "buy")
                {
                    buysell = "Buy";
                    color1 = ConsoleColor.Green;
                    color2 = ConsoleColor.DarkGreen;
                }
                else if (trade.Type == "sell")
                {
                    buysell = "Sell";
                    color1 = ConsoleColor.Red;
                    color2 = ConsoleColor.DarkRed;
                }
                Console.ForegroundColor = color1;
                Console.Write(buysell + ": ");
                Console.ForegroundColor = color2;
                Console.Write("[" + FromUnixTime(trade.Date) + "]\n");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\t" + Format("Price") + trade.Price + sym1);
                Console.WriteLine("\t" + Format("Quantity") + trade.Quantity + sym2);
                Console.WriteLine("\t" + Format("Total") + (Double.Parse(trade.Price.Replace(".", ",")) * Double.Parse(trade.Quantity.Replace(".", ","))) + sym1);
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        public class Trade
        {
            [JsonProperty("date")]
            public long Date { get; set; }
            [JsonProperty("type")]
            public string Type { get; set; }
            [JsonProperty("price")]
            public string Price { get; set; }
            [JsonProperty("quantity")]
            public string Quantity { get; set; }
        }

        private static void Auth()
        {
            if (File.Exists(cfgfile))
            {
                string authFormatted = File.ReadAllText(cfgfile);
                byteArray = Convert.ToBase64String(Encoding.ASCII.GetBytes(authFormatted));
            }
            else
            {
                File.Create(cfgfile).Close();
                Line();
                Login();
            }
        }
    }
}
