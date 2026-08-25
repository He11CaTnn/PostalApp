using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp
{
    public partial class loginForm : Form
    {
        // ── Константы ─────────────────────────────────────────────
        private const string DevToken = "6589a749fe244d32005c3150e322931d5c1ea962e8f43332e01d8127669b908c1c7da51d7b920a3af607e6a0211c5214";

        // Fingerprint сертификата сервера (SHA-256 от raw DER)
        private const string ExpectedCertFingerprint =
            "BC14A0466B54BFB96C9F2B116C519104B9B357374A50DF08FB537C496016008D";

        public loginForm()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var body = await PostDevLoginRequest("1", "1");

            // 1. Парсим строку подключения
            var mCfg = Regex.Match(body, "\"config\"\\s*:\\s*\"([^\"]+)\"");
            string[] parts = mCfg.Groups[1].Value.Split('|');
            Program.ServerIP = parts[0];
            Program.ServerPort = int.Parse(parts[1]);
            Program.ServerDatabase = parts[2];
            Program.ServerUser = parts[3];
            Program.ServerPassword = parts[4];

            // 2. Парсим координаты и устанавливаем стартовую позицию карты
            var mLat = Regex.Match(body, "\"lat\"\\s*:\\s*([\\d\\.\\-]+)");
            var mLng = Regex.Match(body, "\"lng\"\\s*:\\s*([\\d\\.\\-]+)");
            if (mLat.Success && mLng.Success &&
                double.TryParse(mLat.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(mLng.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lng))
            {
                Map.startPosition = new GMap.NET.PointLatLng(lat, lng);
            }

            // 3. Подключаемся к БД
            await DataBase.TryConnectAsync();

            // 4. Загружаем данные сотрудника в CurrentUser
            await UserData.VerifyUser("1", "1");

            PostmanForm postmanForm = new PostmanForm();
            postmanForm.Show();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            var body = await PostDevLoginRequest("2", "2");

            // 1. Парсим строку подключения
            var mCfg = Regex.Match(body, "\"config\"\\s*:\\s*\"([^\"]+)\"");
            string[] parts = mCfg.Groups[1].Value.Split('|');
            Program.ServerIP = parts[0];
            Program.ServerPort = int.Parse(parts[1]);
            Program.ServerDatabase = parts[2];
            Program.ServerUser = parts[3];
            Program.ServerPassword = parts[4];

            // 2. Парсим координаты и устанавливаем стартовую позицию карты
            var mLat = Regex.Match(body, "\"lat\"\\s*:\\s*([\\d\\.\\-]+)");
            var mLng = Regex.Match(body, "\"lng\"\\s*:\\s*([\\d\\.\\-]+)");
            if (mLat.Success && mLng.Success &&
                double.TryParse(mLat.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(mLng.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lng))
            {
                Map.startPosition = new GMap.NET.PointLatLng(lat, lng);
            }

            // 3. Подключаемся к БД
            await DataBase.TryConnectAsync();

            // 4. Загружаем данные сотрудника в CurrentUser
            await UserData.VerifyUser("2", "2");

            OperatorForm operatorForm = new OperatorForm();
            operatorForm.Show();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            var body = await PostDevLoginRequest("3", "3");

            // 1. Парсим строку подключения
            var mCfg = Regex.Match(body, "\"config\"\\s*:\\s*\"([^\"]+)\"");
            string[] parts = mCfg.Groups[1].Value.Split('|');
            Program.ServerIP = parts[0];
            Program.ServerPort = int.Parse(parts[1]);
            Program.ServerDatabase = parts[2];
            Program.ServerUser = parts[3];
            Program.ServerPassword = parts[4];

            // 2. Парсим координаты и устанавливаем стартовую позицию карты
            var mLat = Regex.Match(body, "\"lat\"\\s*:\\s*([\\d\\.\\-]+)");
            var mLng = Regex.Match(body, "\"lng\"\\s*:\\s*([\\d\\.\\-]+)");
            if (mLat.Success && mLng.Success &&
                double.TryParse(mLat.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(mLng.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lng))
            {
                Map.startPosition = new GMap.NET.PointLatLng(lat, lng);
            }

            // 3. Подключаемся к БД
            await DataBase.TryConnectAsync();

            // 4. Загружаем данные сотрудника в CurrentUser
            await UserData.VerifyUser("3", "3");

            ManagerForm managerForm = new ManagerForm();
            managerForm.Show();
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            var body = await PostDevLoginRequest("4", "4");

            // 1. Парсим строку подключения
            var mCfg = Regex.Match(body, "\"config\"\\s*:\\s*\"([^\"]+)\"");
            string[] parts = mCfg.Groups[1].Value.Split('|');
            Program.ServerIP = parts[0];
            Program.ServerPort = int.Parse(parts[1]);
            Program.ServerDatabase = parts[2];
            Program.ServerUser = parts[3];
            Program.ServerPassword = parts[4];

            // 2. Парсим координаты и устанавливаем стартовую позицию карты
            var mLat = Regex.Match(body, "\"lat\"\\s*:\\s*([\\d\\.\\-]+)");
            var mLng = Regex.Match(body, "\"lng\"\\s*:\\s*([\\d\\.\\-]+)");
            if (mLat.Success && mLng.Success &&
                double.TryParse(mLat.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(mLng.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lng))
            {
                Map.startPosition = new GMap.NET.PointLatLng(lat, lng);
            }

            // 3. Подключаемся к БД
            await DataBase.TryConnectAsync();

            // 4. Загружаем данные сотрудника в CurrentUser
            await UserData.VerifyUser("4", "4");

            DirectorForm directorForm = new DirectorForm();
            directorForm.Show();
        }




        // связь с сервером

        private async Task<string> PostDevLoginRequest(string email, string password)
        {
            using (var http = new HttpClient(CreateSslHandler()))
            {
                http.Timeout = TimeSpan.FromSeconds(15);

                string json = "{" +
                    $"\"dev_token\":\"{EscapeJson(DevToken)}\"," +
                    $"\"login\":\"{EscapeJson(email)}\"," +
                    $"\"password\":\"{EscapeJson(password)}\"" +
                    "}";

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await http.PostAsync(
                    "https://81.90.25.60/api/getconfig_extra_dev", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException();

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        // ── SSL pinning ───────────────────────────────────────────────

        private static HttpClientHandler CreateSslHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(cert.RawData);
                    string actual = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                    return string.Equals(actual, ExpectedCertFingerprint,
                                         StringComparison.OrdinalIgnoreCase);
                }
            };
            return handler;
        }

        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        // связь с сервером
    }
}
