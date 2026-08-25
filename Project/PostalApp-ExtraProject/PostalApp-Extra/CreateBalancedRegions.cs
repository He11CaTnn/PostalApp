using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp_Extra
{
    public partial class CreateBalancedRegions : Form
    {
        // Относительный путь к файлу addresses.txt в папке data рядом с exe
        string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "addresses.txt");
        string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SearchAddresses.py");
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "BD.accdb");
        string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "BD.accdb")}";
        private List<Region> balancedRegions = null;

        // Константы для расчета времени
        private double METER_PER_SECOND = 0.25; // 2 секунды на 1 метр
        private double TIME_PER_HOUSE = 30; // 30 секунд на дом
        private int numberOfRegions = 7; // Количество участков

        // Начальная точка (почта)
        private PointLatLng postOfficeLocation = new PointLatLng(Program.StartLat, Program.StartLng);

        // Прозрачность заливки полигонов участков (0=прозрачный, 255=непрозрачный)
        private const int PolygonFillAlpha = 50;

        // Цвета для разных участков
        private Color[] regionColors = new Color[]
        {
            Color.Red,
            Color.Blue,
            Color.Green,
            Color.Orange,
            Color.Purple,
            Color.Cyan,
            Color.Magenta,
            Color.Yellow,
            Color.Lime,
            Color.Brown,
            Color.Pink,
            Color.Teal,
            Color.Lavender,
            Color.Olive,
            Color.Maroon
        };

        // Для отмены операции
        private CancellationTokenSource cancellationTokenSource;

        // Режимы работы с картой
        private enum MapMode
        {
            None,
            CreateMarker,
            MoveObject,
            DeleteObject,
            CreateNode
        }

        private MapMode currentMapMode = MapMode.None;
        private GMarkerGoogle selectedMarkerToMove = null;

        // Поля для редактирования маркера через panel3
        private GMarkerGoogle _selectedMarker = null;
        private bool _isCreatingMarker = false;
        private PointLatLng _newMarkerPoint;
        private GMapPolygon _selectedPolygon = null; // Выделенный полигон для panel4
        private bool _isCreatingRegion = false;     // Флаг режима создания нового участка

        // Словарь: название внешней БД → путь к .accdb файлу
        // Заполняется когда пользователь выбирает "Импорт данных Access..." в comboBox2
        private Dictionary<string, string> _externalDbPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Единый тег для ВСЕХ маркеров на карте. Хранит структурированные данные адреса.
        /// Используйте его везде вместо анонимных типов, AddressPoint и строкового разбора тултипа.
        /// </summary>
        public class MarkerTag
        {
            public string Street { get; set; } = "";
            public string House { get; set; } = "";
            public string Corpus { get; set; } = "";
            public string Flat { get; set; } = "";
            public string BuildingType { get; set; } = "";
            /// <summary>Идентификатор записи в БД (если есть).</summary>
            public string DbId { get; set; } = "";
        }

        /// <summary>
        /// Тег для GMapPolygon — хранит название, цвет и DbId участка.
        /// Используется при клике на полигон для заполнения panel4.
        /// </summary>
        public class PolygonTag
        {
            public string DbId { get; set; } = "";
            public string Name { get; set; } = "";
            public Color Color { get; set; }
        }

        public CreateBalancedRegions()
        {
            InitializeComponent();
            InitializeMap();
            ApplyTheme();
            VersionLabel.Text = Program.version;
            comboBox2.SelectedItem = comboBox2.Items[0];
            comboBox3.SelectedItem = comboBox3.Items[0];

            // Типы зданий в comboBox5
            comboBox5.Items.Clear();
            comboBox5.Items.AddRange(new object[]
            {
                "Не указан",
                "Частный дом",
                "Многоквартирный дом",
                "Здание организаций",
                "Почтовое отделение"
            });
            comboBox5.SelectedIndex = 0;
            panel3.Visible = false;
            panel4.Visible = false;

            // Добавляем пункт импорта внешней БД в конец comboBox2
            if (!comboBox2.Items.Contains("Импорт данных Access..."))
                comboBox2.Items.Add("Импорт данных Access...");

            button18_Click(null, null);
        }

        // ─── Hover-эффекты для тёмной темы ───────────────────────────────
        private void ApplyTheme()
        {
            foreach (Control c in this.Controls)
            {
                if (c is Button btn)
                {
                    Color normal = btn.BackColor;
                    Color hover = LightenColor(normal, 22);
                    btn.MouseEnter += (s, e) => btn.BackColor = hover;
                    btn.MouseLeave += (s, e) => btn.BackColor = normal;
                }
            }
        }

        private static Color LightenColor(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Min(255, color.R + amount),
                Math.Min(255, color.G + amount),
                Math.Min(255, color.B + amount));
        }
        // ─────────────────────────────────────────────────────────────────

        private void InitializeMap()
        {
            gMapControl1.MapProvider = GMapProviders.OpenCycleMap;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            gMapControl1.Position = postOfficeLocation;
            gMapControl1.ShowCenter = false;
            gMapControl1.MinZoom = 5;
            gMapControl1.MaxZoom = 18;
            gMapControl1.Zoom = 12;
            gMapControl1.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
            gMapControl1.CanDragMap = true;
            gMapControl1.DragButton = MouseButtons.Left;
        }

        private void GMapControl1_OnMapClick(PointLatLng pointClick, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            // Любой клик на карту скрывает panel3/panel4, кроме режима CreateMarker
            if (currentMapMode != MapMode.CreateMarker)
            {
                panel3.Visible = false;
                panel4.Visible = false;
                _selectedPolygon = null;
            }

            switch (currentMapMode)
            {
                case MapMode.CreateMarker:
                    HandleCreateMarkerClick(pointClick);
                    break;

                case MapMode.MoveObject:
                    HandleMoveObjectClick(pointClick, e.X, e.Y);
                    break;

                case MapMode.DeleteObject:
                    HandleDeleteObjectClick(e.X, e.Y);
                    break;

                case MapMode.CreateNode:
                    HandleCreateNodeClick(pointClick);
                    break;
            }
        }

        private void HandleCreateMarkerClick(PointLatLng point)
        {
            _newMarkerPoint = point;
            _isCreatingMarker = true;
            _selectedMarker = null;

            // Дефолтные значения для новой метки
            SetComboBox5Value("Не указан");
            textBox1.Text = "Улица";
            textBox2.Text = "Дом";
            textBox3.Text = "";
            textBox4.Text = "";

            panel3.Visible = true;

            // Деактивируем режим создания — сохранение произойдёт через кнопку panel3
            DeactivateMapMode();
        }

        private void HandleMoveObjectClick(PointLatLng point, int mouseX, int mouseY)
        {
            if (selectedMarkerToMove == null)
            {
                // Первый клик - выбираем объект для перемещения

                // Сначала проверяем клик по узлу участка
                var nodeInfo = FindNearestNode(mouseX, mouseY);
                if (nodeInfo.HasValue)
                {
                    // Сохраняем информацию об узле в Tag
                    selectedMarkerToMove = new GMarkerGoogle(nodeInfo.Value.nodePoint, GMarkerGoogleType.blue_small);
                    selectedMarkerToMove.Tag = new { IsNode = true, RegionId = nodeInfo.Value.regionId, OldPoint = nodeInfo.Value.nodePoint };
                    gMapControl1.Cursor = Cursors.Cross;
                    return;
                }

                // Проверяем клик по маркеру
                foreach (var overlay in gMapControl1.Overlays)
                {
                    foreach (var marker in overlay.Markers.OfType<GMarkerGoogle>())
                    {
                        var markerPoint = gMapControl1.FromLatLngToLocal(marker.Position);
                        double distance = Math.Sqrt(Math.Pow(markerPoint.X - mouseX, 2) + Math.Pow(markerPoint.Y - mouseY, 2));

                        if (distance < 10) // 10 пикселей радиус клика
                        {
                            selectedMarkerToMove = marker;
                            gMapControl1.Cursor = Cursors.Cross;
                            return;
                        }
                    }
                }
            }
            else
            {
                // Второй клик - перемещаем объект

                // Проверяем, это узел или маркер
                if (selectedMarkerToMove.Tag != null)
                {
                    var tagType = selectedMarkerToMove.Tag.GetType();
                    var isNodeProp = tagType.GetProperty("IsNode");

                    if (isNodeProp != null && (bool)isNodeProp.GetValue(selectedMarkerToMove.Tag))
                    {
                        // Это узел участка
                        var regionIdProp = tagType.GetProperty("RegionId");
                        var oldPointProp = tagType.GetProperty("OldPoint");

                        string regionId = regionIdProp?.GetValue(selectedMarkerToMove.Tag)?.ToString();
                        PointLatLng oldPoint = (PointLatLng)(oldPointProp?.GetValue(selectedMarkerToMove.Tag) ?? default(PointLatLng));

                        if (!string.IsNullOrEmpty(regionId))
                        {
                            // Обновляем узел в БД
                            UpdateNodeInDatabase(regionId, oldPoint, point);

                            // Если включена автооптимизация - оптимизируем границы
                            if (checkBox1 != null && checkBox1.Checked)
                            {
                                OptimizeSingleRegionBoundary(regionId);
                            }

                            // Перезагружаем участки
                            try
                            {
                                var regions = LoadRegionsFromNewTables();
                                balancedRegions = regions;
                                DisplayRegionsOnMap(regions);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Ошибка перезагрузки участков: {ex.Message}");
                            }
                        }

                        selectedMarkerToMove = null;
                        gMapControl1.Cursor = Cursors.Default;
                        return;
                    }
                }

                // Это обычный маркер
                var overlay = gMapControl1.Overlays.FirstOrDefault(o => o.Markers.Contains(selectedMarkerToMove));
                if (overlay != null)
                {
                    overlay.Markers.Remove(selectedMarkerToMove);

                    // Определяем цвет маркера по положению на карте
                    Color markerColor = Color.Gray;
                    if (balancedRegions != null)
                    {
                        foreach (var region in balancedRegions)
                        {
                            if (region.PolygonPoints.Count >= 3 &&
                                IsPointInPolygon(point.Lat, point.Lng, region.PolygonPoints))
                            {
                                markerColor = region.Color;
                                break;
                            }
                        }
                    }

                    // Создаем новый маркер на новой позиции с кастомным цветом
                    Bitmap customMarker = CreateSmallCircleBitmap(markerColor);
                    var newMarker = new GMarkerGoogle(point, customMarker);
                    newMarker.Tag = selectedMarkerToMove.Tag;
                    newMarker.ToolTipText = selectedMarkerToMove.ToolTipText;
                    newMarker.ToolTipMode = selectedMarkerToMove.ToolTipMode;
                    newMarker.ToolTip.Fill = Brushes.White;
                    newMarker.ToolTip.Foreground = Brushes.Black;
                    newMarker.ToolTip.Stroke = Pens.Black;
                    newMarker.ToolTip.TextPadding = new Size(10, 10);

                    overlay.Markers.Add(newMarker);
                    gMapControl1.Refresh();
                }

                selectedMarkerToMove = null;
                gMapControl1.Cursor = Cursors.Default;
            }
        }

        private void HandleDeleteObjectClick(int mouseX, int mouseY)
        {
            // Сначала проверяем клик по узлу участка
            var nodeInfo = FindNearestNode(mouseX, mouseY);
            if (nodeInfo.HasValue)
            {
                // Удаляем узел из БД
                DeleteNodeFromDatabase(nodeInfo.Value.regionId, nodeInfo.Value.nodePoint);

                // Если включена автооптимизация - оптимизируем границы
                if (checkBox1 != null && checkBox1.Checked)
                {
                    OptimizeSingleRegionBoundary(nodeInfo.Value.regionId);
                }

                // Перезагружаем участки
                try
                {
                    var regions = LoadRegionsFromNewTables();
                    balancedRegions = regions;
                    DisplayRegionsOnMap(regions);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка перезагрузки участков: {ex.Message}");
                }

                return;
            }

            // Проверяем клик по маркеру
            foreach (var overlay in gMapControl1.Overlays.ToList())
            {
                foreach (var marker in overlay.Markers.OfType<GMarkerGoogle>().ToList())
                {
                    var markerPoint = gMapControl1.FromLatLngToLocal(marker.Position);
                    double distance = Math.Sqrt(Math.Pow(markerPoint.X - mouseX, 2) + Math.Pow(markerPoint.Y - mouseY, 2));

                    if (distance < 10) // 10 пикселей радиус клика
                    {
                        overlay.Markers.Remove(marker);
                        gMapControl1.Refresh();
                        return;
                    }
                }
            }
        }

        private void HandleCreateNodeClick(PointLatLng point)
        {
            if (comboBox1 == null || comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Выберите участок в списке!");
                return;
            }

            string selectedRegionName = comboBox1.SelectedItem.ToString();

            // Находим ID участка по названию
            string regionId = null;
            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                string selectRegion = "SELECT [id] FROM Участки WHERE [Название] = ?";

                using (var cmd = new OleDbCommand(selectRegion, connection))
                {
                    cmd.Parameters.AddWithValue("@Название", selectedRegionName);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                        regionId = result.ToString();
                }
            }

            if (regionId == null)
            {
                MessageBox.Show("Участок не найден в БД!");
                return;
            }

            // Добавляем узел в БД
            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                // Получаем максимальный номер узла для этого участка
                string getMaxNumber = "SELECT MAX([Номер]) FROM Узлы WHERE [Id участка] = ?";
                int nextNumber = 1;

                using (var cmd = new OleDbCommand(getMaxNumber, connection))
                {
                    cmd.Parameters.AddWithValue("@id_участка", regionId);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        nextNumber = Convert.ToInt32(result) + 1;
                }

                // Вставляем новый узел
                string insertNode = @"
                    INSERT INTO Узлы ([id], [Долгота], [Широта], [Id участка], [Номер])
                    VALUES (?, ?, ?, ?, ?)";

                using (var cmd = new OleDbCommand(insertNode, connection))
                {
                    cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                    cmd.Parameters.AddWithValue("@Долгота", Math.Round(point.Lng, 6));
                    cmd.Parameters.AddWithValue("@Широта", Math.Round(point.Lat, 6));
                    cmd.Parameters.AddWithValue("@id_участка", regionId);
                    cmd.Parameters.AddWithValue("@Номер", nextNumber);
                    cmd.ExecuteNonQuery();
                }
            }

            // Если включена автооптимизация - оптимизируем границы этого участка
            if (checkBox1 != null && checkBox1.Checked)
            {
                OptimizeSingleRegionBoundary(regionId);
            }

            MessageBox.Show($"Узел добавлен к участку '{selectedRegionName}'");

            // Перезагружаем участки для отображения изменений
            try
            {
                var regions = LoadRegionsFromNewTables();
                balancedRegions = regions;
                DisplayRegionsOnMap(regions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка перезагрузки участков: {ex.Message}");
            }
        }

        // Класс для хранения информации о точке
        public class AddressPoint
        {
            public int Id { get; set; }
            public string Address { get; set; }   // полная строка адреса (для обратной совместимости)
            public string Street { get; set; }    // улица
            public string House { get; set; }     // дом
            public string Corpus { get; set; }    // корпус
            public string Flat { get; set; }      // квартира
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string BuildingType { get; set; }
            public int RegionId { get; set; }
            public double DistanceToPostOffice { get; set; }
        }

        // Класс для хранения информации о регионе (ДОБАВЛЕНО поле DbId)
        public new class Region
        {
            public int Id { get; set; }
            public string DbId { get; set; }  // Guid из БД
            public string Name { get; set; } = ""; // Название участка
            public List<AddressPoint> Points { get; set; } = new List<AddressPoint>();
            public Color Color { get; set; }
            public double TotalTime { get; set; }
            public PointLatLng? CenterPoint { get; set; }
            public List<PointLatLng> PolygonPoints { get; set; } = new List<PointLatLng>();
            public List<PointLatLng> ConvexHullPoints { get; set; } = new List<PointLatLng>();

            /// <summary>Количество уникальных улиц в участке (чем меньше — тем лучше).</summary>
            public int UniqueStreetCount =>
                Points.Select(p => p.Street ?? "").Where(s => s.Length > 0).Distinct().Count();
        }

        private string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }

        private void LoadMarkersFromAddresses()
        {
            try
            {
                gMapControl1.Overlays.Clear();
                var overlay = new GMapOverlay("addresses");

                if (!File.Exists(fileName))
                {
                    MessageBox.Show("Файл addresses.txt не найден!\nВыполните поиск адресов (OSM)!");
                    return;
                }

                string[] lines = File.ReadAllLines(fileName);
                int pointId = 0;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = SplitCsvLine(line);
                    if (parts.Length < 7)
                        continue;

                    if (!double.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                                         System.Globalization.CultureInfo.InvariantCulture, out double lon) ||
                        !double.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                                         System.Globalization.CultureInfo.InvariantCulture, out double lat))
                        continue;

                    // Создаем полный AddressPoint для балансировки
                    var addressPoint = new AddressPoint
                    {
                        Id = pointId++,
                        Latitude = lat,
                        Longitude = lon,
                        BuildingType = parts[2].Trim('"'),
                        Address = BuildFullAddress(parts[3].Trim('"'), parts[4].Trim('"'),
                                                 parts[5].Trim('"'), parts[6].Trim('"')),
                        RegionId = 0, // Будет заполнено при балансировке
                        DistanceToPostOffice = 0 // Будет рассчитано позже
                    };

                    var point = new PointLatLng(lat, lon);

                    // Создаем кастомный серый маркер
                    Bitmap grayMarker = CreateSmallCircleBitmap(Color.Gray);
                    var marker = new GMarkerGoogle(point, grayMarker);

                    // Единый MarkerTag — именно его читает panel3 при клике
                    var tag = new MarkerTag
                    {
                        Street = parts[3].Trim('"'),
                        House = parts[4].Trim('"'),
                        Corpus = parts[5].Trim('"'),
                        Flat = parts[6].Trim('"'),
                        BuildingType = parts[2].Trim('"')
                    };
                    ApplyMarkerTag(marker, tag);

                    overlay.Markers.Add(marker);
                }

                gMapControl1.Overlays.Add(overlay);
                gMapControl1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке меток из файла addresses.txt: " + ex.Message);
            }
        }

        // Вспомогательный метод для формирования полного адреса
        private string BuildFullAddress(string street, string house, string corpus, string flat)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
            if (!string.IsNullOrWhiteSpace(house)) parts.Add($"д. {house}");
            if (!string.IsNullOrWhiteSpace(corpus)) parts.Add($"корп. {corpus}");
            if (!string.IsNullOrWhiteSpace(flat)) parts.Add($"кв. {flat}");
            return string.Join(", ", parts);
        }

        private string BuildAddressTooltip(string street, string house, string corpus, string flat, string typeBuilding)
        {
            var lines = new List<string>();

            if (!string.IsNullOrWhiteSpace(street)) lines.Add($"\nУлица: {street}");
            if (!string.IsNullOrWhiteSpace(house)) lines.Add($"\nДом: {house}");
            if (!string.IsNullOrWhiteSpace(corpus)) lines.Add($"\nКорпус: {corpus}");
            if (!string.IsNullOrWhiteSpace(flat)) lines.Add($"\nКвартира: {flat}");
            if (!string.IsNullOrWhiteSpace(typeBuilding)) lines.Add($"\nТип: {typeBuilding}\n");

            return lines.Count > 0 ? string.Join("", lines) : "Адрес: данные не заполнены";
        }

        // ─── Единые вспомогательные методы для маркеров ──────────────────────

        /// <summary>Строит текст тултипа из MarkerTag.</summary>
        private string BuildTooltipFromTag(MarkerTag tag)
        {
            return BuildAddressTooltip(tag.Street, tag.House, tag.Corpus, tag.Flat, tag.BuildingType);
        }

        /// <summary>
        /// Применяет единый стиль тултипа к маркеру и привязывает к нему MarkerTag.
        /// Используйте этот метод везде, где создаётся или обновляется маркер.
        /// </summary>
        private void ApplyMarkerTag(GMarkerGoogle marker, MarkerTag tag)
        {
            marker.Tag = tag;
            marker.ToolTipText = BuildTooltipFromTag(tag);
            marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;
            marker.ToolTip.Fill = Brushes.White;
            marker.ToolTip.Foreground = Brushes.Black;
            marker.ToolTip.Stroke = Pens.Black;
            marker.ToolTip.TextPadding = new Size(10, 10);
        }

        /// <summary>Выставляет выбранное значение в comboBox5 (безопасно).</summary>
        private void SetComboBox5Value(string value)
        {
            int idx = comboBox5.Items.IndexOf(value ?? "");
            comboBox5.SelectedIndex = idx >= 0 ? idx : 0;
        }

        /// <summary>
        /// Извлекает MarkerTag из маркера. Поддерживает как новый MarkerTag,
        /// так и устаревшие форматы Tag (AddressPoint, анонимный объект).
        /// Если Tag не задан — пытается распарсить тултип как запасной вариант.
        /// </summary>
        private MarkerTag GetMarkerTag(GMarkerGoogle marker)
        {
            if (marker.Tag is MarkerTag mt)
                return mt;

            // Запасной вариант: парсим тултип
            return ParseTooltipToMarkerTag(marker.ToolTipText);
        }

        /// <summary>Парсит тултип в MarkerTag. Поддерживает формат «Улица: X\nДом: Y...».</summary>
        private MarkerTag ParseTooltipToMarkerTag(string tooltip)
        {
            var tag = new MarkerTag();
            if (string.IsNullOrWhiteSpace(tooltip))
                return tag;

            // Тултип строится со строками вида "\nУлица: X\nДом: Y" — разбиваем по \n
            foreach (var rawLine in tooltip.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.StartsWith("Улица:")) tag.Street = line.Substring("Улица:".Length).Trim();
                else if (line.StartsWith("Дом:")) tag.House = line.Substring("Дом:".Length).Trim();
                else if (line.StartsWith("Корпус:")) tag.Corpus = line.Substring("Корпус:".Length).Trim();
                else if (line.StartsWith("Квартира:")) tag.Flat = line.Substring("Квартира:".Length).Trim();
                else if (line.StartsWith("Тип строения:")) tag.BuildingType = line.Substring("Тип строения:".Length).Trim();
                else if (line.StartsWith("Тип:")) tag.BuildingType = line.Substring("Тип:".Length).Trim();
            }
            return tag;
        }

        private Bitmap CreateSmallCircleBitmap(Color color)
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (Brush brush = new SolidBrush(color))
                using (Pen border = new Pen(Color.Black, 1))
                {
                    g.FillEllipse(brush, 2, 2, 12, 12);
                    g.DrawEllipse(border, 2, 2, 12, 12);
                }
            }
            return bmp;
        }

        private string ExtractStreetFromAddressPoint(AddressPoint ap)
        {
            // Если Street уже заполнен — возвращаем его напрямую
            if (!string.IsNullOrWhiteSpace(ap.Street))
                return ap.Street;

            // Запасной вариант: берём первый токен из Address
            if (string.IsNullOrWhiteSpace(ap.Address)) return null;
            var tokens = ap.Address.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
            return tokens.Count > 0 ? tokens[0] : null;
        }

        /// <summary>Извлекает название улицы из строки адреса формата «Улица, д. X, кор. Y».</summary>
        private string ExtractStreetFromAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return "";
            return address.Split(',')[0].Trim();
        }

        private string ExtractHouseFromAddressPoint(AddressPoint ap)
        {
            if (string.IsNullOrWhiteSpace(ap.Address)) return null;
            var tokens = ap.Address.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim());
            foreach (var t in tokens)
            {
                if (t.StartsWith("д.") || t.StartsWith("д ") || t.StartsWith("дом"))
                    return t.Replace("д.", "").Replace("д ", "").Replace("дом", "").Trim();
                if (t.All(c => char.IsDigit(c)))
                    return t;
            }
            return null;
        }

        private string ExtractCorpusFromAddressPoint(AddressPoint ap)
        {
            if (string.IsNullOrWhiteSpace(ap.Address)) return null;
            var tokens = ap.Address.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim());
            foreach (var t in tokens)
            {
                if (t.StartsWith("корп.") || t.StartsWith("корп "))
                    return t.Replace("корп.", "").Replace("корп ", "").Trim();
                if (t.StartsWith("к.") || t.StartsWith("к "))
                    return t.Replace("к.", "").Replace("к ", "").Trim();
            }
            return null;
        }

        private string ExtractFlatFromAddressPoint(AddressPoint ap)
        {
            if (string.IsNullOrWhiteSpace(ap.Address)) return null;
            var tokens = ap.Address.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim());
            foreach (var t in tokens)
            {
                if (t.StartsWith("кв.") || t.StartsWith("кв "))
                    return t.Replace("кв.", "").Replace("кв ", "").Trim();
            }
            return null;
        }

        // Устаревший класс — оставлен для совместимости с возможными другими вызовами.
        // В новом коде используйте MarkerTag напрямую.
        private class AddressParts
        {
            public string Street { get; set; }
            public string House { get; set; }
            public string Corpus { get; set; }
            public string Flat { get; set; }
            public string BuildingType { get; set; }
        }

        private AddressParts ParseTooltipToAddressParts(string tooltip)
        {
            var mt = ParseTooltipToMarkerTag(tooltip);
            return new AddressParts
            {
                Street = mt.Street,
                House = mt.House,
                Corpus = mt.Corpus,
                Flat = mt.Flat,
                BuildingType = mt.BuildingType
            };
        }


        private void UpdateStatusLabel(string message)
        {
            statusLabel.Text = message;
        }

        // button3_Click (балансировка)
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                ButtonEnabledInBalancedRegions(false);
                EnsureAccessDatabaseStructure();

                cancellationTokenSource = new CancellationTokenSource();
                MessageBox.Show($"Начинаю балансировку на {numberOfRegions} участков. Это может занять некоторое время...");

                Task.Run(() => BalanceAndCreateRegionsWithValidation(cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                ButtonEnabledInBalancedRegions(true);
                Cursor = Cursors.Default;
                button3.Enabled = true;
            }
        }

        private void ButtonEnabledInBalancedRegions(bool enabled)
        {
            button3.Enabled = enabled;
            button4.Enabled = enabled;
            DataTransferBroupBox.Enabled = enabled;
            EditMarkersAndNodesGroupBox.Enabled = enabled;
            groupBox4.Enabled = enabled;
        }

        private Task<List<AddressPoint>> LoadPointsFromMapAsync()
        {
            return Task.Run(() =>
            {
                var points = new List<AddressPoint>();
                int id = 0;

                foreach (var overlay in gMapControl1.Overlays)
                {
                    foreach (var marker in overlay.Markers.OfType<GMarkerGoogle>())
                    {
                        // Пропускаем служебные маркеры (почта, центры участков)
                        if (marker.Tag == null && string.IsNullOrWhiteSpace(marker.ToolTipText))
                            continue;
                        if (marker.ToolTipText == "Почтовое отделение")
                            continue;
                        if (marker.ToolTipText?.StartsWith("Центр участка") == true)
                            continue;

                        // Читаем данные через GetMarkerTag — работает для любого формата Tag
                        var tag = GetMarkerTag(marker);

                        var p = new AddressPoint
                        {
                            Id = id++,
                            Latitude = marker.Position.Lat,
                            Longitude = marker.Position.Lng,
                            BuildingType = tag.BuildingType,
                            Street = tag.Street,
                            House = tag.House,
                            Corpus = tag.Corpus,
                            Flat = tag.Flat,
                            Address = BuildFullAddress(tag.Street, tag.House, tag.Corpus, tag.Flat),
                            RegionId = 0,
                            DistanceToPostOffice = 0
                        };
                        points.Add(p);
                    }
                }

                return points;
            });
        }

        private string ExtractAddressFromTooltip(string tooltip)
        {
            var tag = ParseTooltipToMarkerTag(tooltip);
            return BuildFullAddress(tag.Street, tag.House, tag.Corpus, tag.Flat);
        }

        private string ExtractBuildingTypeFromTooltip(string tooltip)
        {
            return ParseTooltipToMarkerTag(tooltip).BuildingType;
        }

        private async Task BalanceAndCreateRegionsWithValidation(CancellationToken cancellationToken)
        {
            try
            {
                this.Invoke(new Action(() =>
                {
                    UpdateStatusLabel("Загрузка данных с карты...");
                }));

                // *** БЕРЁМ ТОЧКИ С КАРТЫ, А НЕ ИЗ БД ***
                var allPoints = await LoadPointsFromMapAsync();
                if (allPoints.Count == 0)
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show("На карте нет меток для балансировки!");
                        Cursor = Cursors.Default;
                        button3.Enabled = true;
                    }));
                    return;
                }

                List<Region> regions = null;
                int iteration = 1;
                const int maxIterations = 50;
                bool isValid = false;

                while (!isValid && iteration <= maxIterations)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    this.Invoke(new Action(() =>
                    {
                        UpdateStatusLabel($"Итерация {iteration}/{maxIterations}: Выполняю кластеризацию...");
                    }));

                    regions = PerformKMeansClustering(allPoints, numberOfRegions);

                    CalculateRegionTimes(regions);

                    BalanceRegions(regions, cancellationToken);

                    BuildConvexHulls(regions);

                    var intersectionResult = CheckPointIntersections(allPoints, regions);
                    isValid = intersectionResult.IsValid;

                    if (!isValid)
                    {
                        this.Invoke(new Action(() =>
                        {
                            UpdateStatusLabel($"Итерация {iteration}: Найдено {intersectionResult.ConflictingPoints.Count} конфликтных точек. Перестраиваю...");
                            MessageBox.Show($"Итерация {iteration}: Найдено точек, принадлежащих нескольким участкам: {intersectionResult.ConflictingPoints.Count}. Перестраиваю участки...");
                        }));

                        RedistributeConflictingPoints(allPoints, regions, intersectionResult);
                        iteration++;
                    }
                    else
                    {
                        this.Invoke(new Action(() =>
                        {
                            UpdateStatusLabel($"Итерация {iteration}: Все точки принадлежат только одному участку!");
                        }));
                    }

                    await Task.Delay(100);
                }

                if (!isValid && iteration > maxIterations)
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show($"Достигнуто максимальное количество итераций ({maxIterations}). Возможно, некоторые точки все еще в нескольких участках.");
                    }));
                }

                BuildRegionPolygons(regions);

                // Обновляем поле "Участок" в таблице Метки (как было)
                SaveRegionsToDatabaseOld(regions);

                this.Invoke(new Action(() =>
                {
                    DisplayRegionsOnMap(regions);
                    DisplayStatistics(regions);

                    Cursor = Cursors.Default;
                    ButtonEnabledInBalancedRegions(true);
                    UpdateStatusLabel("Балансировка завершена!");

                    MessageBox.Show($"Балансировка завершена за {iteration} итераций! Создано {regions.Count} участков.");
                    balancedRegions = regions;
                }));
            }
            catch (OperationCanceledException)
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show("Операция балансировки отменена.");
                    Cursor = Cursors.Default;
                    button3.Enabled = true;
                    UpdateStatusLabel("Операция отменена.");
                }));
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show($"Ошибка при балансировке: {ex.Message}");
                    Cursor = Cursors.Default;
                    button3.Enabled = true;
                    UpdateStatusLabel($"Ошибка: {ex.Message}");
                }));
            }
        }

        private List<Region> PerformKMeansClustering(List<AddressPoint> points, int clusterCount)
        {
            var regions = new List<Region>();
            var random = new Random();
            var centers = new List<PointLatLng>();

            // ─── K-means++ инициализация центроидов ───────────────────────────────
            if (points.Count > 0)
            {
                // Шаг 1: первый центроид — случайная точка
                var first = points[random.Next(points.Count)];
                centers.Add(new PointLatLng(first.Latitude, first.Longitude));

                // Шаги 2..k: каждый следующий центроид выбирается с вероятностью
                // пропорциональной D²(x) — квадрату расстояния до ближайшего уже выбранного центра
                while (centers.Count < clusterCount)
                {
                    var distances = new double[points.Count];
                    double totalWeight = 0;

                    for (int j = 0; j < points.Count; j++)
                    {
                        double minDist = double.MaxValue;
                        foreach (var c in centers)
                        {
                            double d = CalculateDistance(
                                points[j].Latitude, points[j].Longitude,
                                c.Lat, c.Lng);
                            if (d < minDist) minDist = d;
                        }
                        distances[j] = minDist * minDist; // D²
                        totalWeight += distances[j];
                    }

                    // Выбираем точку с вероятностью D²/sum(D²)
                    double threshold = random.NextDouble() * totalWeight;
                    double cumulative = 0;
                    int chosen = points.Count - 1;
                    for (int j = 0; j < points.Count; j++)
                    {
                        cumulative += distances[j];
                        if (cumulative >= threshold)
                        {
                            chosen = j;
                            break;
                        }
                    }
                    centers.Add(new PointLatLng(points[chosen].Latitude, points[chosen].Longitude));
                }
            }
            else
            {
                // Нет точек — раскладываем центры случайно вокруг почты
                for (int i = 0; i < clusterCount; i++)
                    centers.Add(new PointLatLng(
                        postOfficeLocation.Lat + (random.NextDouble() - 0.5) * 0.1,
                        postOfficeLocation.Lng + (random.NextDouble() - 0.5) * 0.1));
            }

            for (int i = 0; i < clusterCount; i++)
            {
                regions.Add(new Region
                {
                    Id = i,
                    Color = regionColors[i % regionColors.Length],
                    Name = $"Участок {i + 1}"
                });
            }

            const int maxIterations = 100;
            bool changed;
            int iteration = 0;

            do
            {
                changed = false;

                foreach (var region in regions)
                    region.Points.Clear();

                foreach (var point in points)
                {
                    int nearestCenterIndex = -1;
                    double minDistance = double.MaxValue;

                    for (int i = 0; i < centers.Count; i++)
                    {
                        double distance = CalculateDistance(
                            point.Latitude, point.Longitude,
                            centers[i].Lat, centers[i].Lng);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestCenterIndex = i;
                        }
                    }

                    if (nearestCenterIndex >= 0 && nearestCenterIndex < regions.Count)
                    {
                        point.RegionId = nearestCenterIndex;
                        regions[nearestCenterIndex].Points.Add(point);
                    }
                }

                for (int i = 0; i < centers.Count; i++)
                {
                    if (regions[i].Points.Count > 0)
                    {
                        double avgLat = regions[i].Points.Average(p => p.Latitude);
                        double avgLng = regions[i].Points.Average(p => p.Longitude);

                        var newCenter = new PointLatLng(avgLat, avgLng);

                        if (CalculateDistance(centers[i].Lat, centers[i].Lng, newCenter.Lat, newCenter.Lng) > 0.0001)
                        {
                            centers[i] = newCenter;
                            changed = true;
                        }
                    }
                }

                iteration++;
            } while (changed && iteration < maxIterations);

            for (int i = 0; i < regions.Count; i++)
            {
                regions[i].CenterPoint = centers[i];
            }

            return regions;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;

        private void CalculateRegionTimes(List<Region> regions)
        {
            foreach (var region in regions)
            {
                foreach (var point in region.Points)
                {
                    point.DistanceToPostOffice = CalculateDistance(
                        postOfficeLocation.Lat, postOfficeLocation.Lng,
                        point.Latitude, point.Longitude);
                }

                double houseTime = region.Points.Count * TIME_PER_HOUSE;

                double avgDistance = region.Points.Any()
                    ? region.Points.Average(p => p.DistanceToPostOffice)
                    : 0;

                double travelTime = avgDistance * METER_PER_SECOND * 2;

                region.TotalTime = houseTime + travelTime;
            }
        }

        private void BalanceRegions(List<Region> regions, CancellationToken cancellationToken)
        {
            const int maxIterations = 100;
            double targetTime = regions.Average(r => r.TotalTime);

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool improved = false;

                var maxTimeRegion = regions.OrderByDescending(r => r.TotalTime).First();
                var minTimeRegion = regions.OrderBy(r => r.TotalTime).First();

                if ((maxTimeRegion.TotalTime - minTimeRegion.TotalTime) / targetTime < 0.1)
                    break;

                var borderPoints = FindBorderPointsBetweenRegions(maxTimeRegion, minTimeRegion);

                if (borderPoints.Any())
                {
                    var pointToMove = borderPoints.First();

                    maxTimeRegion.Points.Remove(pointToMove);
                    minTimeRegion.Points.Add(pointToMove);
                    pointToMove.RegionId = minTimeRegion.Id;

                    CalculateRegionTimes(new List<Region> { maxTimeRegion, minTimeRegion });

                    improved = true;
                }

                if (!improved) break;
            }
        }

        private List<AddressPoint> FindBorderPointsBetweenRegions(Region sourceRegion, Region targetRegion)
        {
            var borderPoints = new List<AddressPoint>();

            if (!sourceRegion.Points.Any() || !targetRegion.Points.Any())
                return borderPoints;

            var targetCenter = targetRegion.CenterPoint ?? CalculateRegionCenter(targetRegion);
            var sourceCenter = sourceRegion.CenterPoint ?? CalculateRegionCenter(sourceRegion);

            // Считаем сколько точек на каждой улице в исходном участке.
            // Если точка последняя на своей улице — её перенос уменьшит количество улиц.
            var streetCountsInSource = sourceRegion.Points
                .GroupBy(p => p.Street ?? "")
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var point in sourceRegion.Points)
            {
                double distToTarget = CalculateDistance(
                    point.Latitude, point.Longitude,
                    targetCenter.Lat, targetCenter.Lng);
                double distToSource = CalculateDistance(
                    point.Latitude, point.Longitude,
                    sourceCenter.Lat, sourceCenter.Lng);

                if (distToTarget < distToSource)
                    borderPoints.Add(point);
            }

            // Сортировка:
            //   Первичный критерий  — расстояние до центра целевого участка (оптимальные траектории).
            //   Вторичный критерий  — если точка последняя на своей улице, даём ей скидку 25%,
            //                         чтобы при прочих равных перенести именно её и сократить
            //                         количество улиц в исходном участке.
            const double lastOnStreetBonus = 0.75;

            return borderPoints.OrderBy(p =>
            {
                double dist = CalculateDistance(
                    p.Latitude, p.Longitude,
                    targetCenter.Lat, targetCenter.Lng);

                string street = p.Street ?? "";
                bool isLastOnStreet = street.Length > 0
                    && streetCountsInSource.TryGetValue(street, out int cnt)
                    && cnt == 1;

                return isLastOnStreet ? dist * lastOnStreetBonus : dist;
            }).ToList();
        }

        private PointLatLng CalculateRegionCenter(Region region)
        {
            if (!region.Points.Any())
                return postOfficeLocation;

            double avgLat = region.Points.Average(p => p.Latitude);
            double avgLng = region.Points.Average(p => p.Longitude);

            return new PointLatLng(avgLat, avgLng);
        }

        private void BuildConvexHulls(List<Region> regions)
        {
            foreach (var region in regions)
            {
                if (region.Points.Count < 3)
                {
                    region.ConvexHullPoints = region.Points
                        .Select(p => new PointLatLng(p.Latitude, p.Longitude))
                        .ToList();
                    continue;
                }

                region.ConvexHullPoints = CalculateConvexHull(
                    region.Points.Select(p => new PointLatLng(p.Latitude, p.Longitude)).ToList());
            }
        }

        private List<PointLatLng> CalculateConvexHull(List<PointLatLng> points)
        {
            if (points.Count < 3)
                return points;

            var startPoint = points.OrderBy(p => p.Lat).ThenBy(p => p.Lng).First();

            var sortedPoints = points
                .Where(p => !p.Equals(startPoint))
                .OrderBy(p => Math.Atan2(p.Lat - startPoint.Lat, p.Lng - startPoint.Lng))
                .ToList();

            var hull = new List<PointLatLng> { startPoint };

            foreach (var point in sortedPoints)
            {
                while (hull.Count >= 2 &&
                       CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(point);
            }

            return hull;
        }

        private double CrossProduct(PointLatLng O, PointLatLng A, PointLatLng B)
        {
            return (A.Lng - O.Lng) * (B.Lat - O.Lat) - (A.Lat - O.Lat) * (B.Lng - O.Lng);
        }

        private class IntersectionCheckResult
        {
            public bool IsValid { get; set; }
            public List<AddressPoint> ConflictingPoints { get; set; } = new List<AddressPoint>();
            public Dictionary<int, List<int>> PointRegions { get; set; } = new Dictionary<int, List<int>>();
        }

        private IntersectionCheckResult CheckPointIntersections(List<AddressPoint> allPoints, List<Region> regions)
        {
            var result = new IntersectionCheckResult
            {
                IsValid = true,
                ConflictingPoints = new List<AddressPoint>(),
                PointRegions = new Dictionary<int, List<int>>()
            };

            foreach (var point in allPoints)
            {
                var containingRegions = new List<int>();

                for (int i = 0; i < regions.Count; i++)
                {
                    if (regions[i].ConvexHullPoints.Count >= 3)
                    {
                        if (IsPointInPolygon(point.Latitude, point.Longitude, regions[i].ConvexHullPoints))
                        {
                            containingRegions.Add(i);
                        }
                    }
                }

                result.PointRegions[point.Id] = containingRegions;

                if (containingRegions.Count > 1)
                {
                    result.IsValid = false;
                    result.ConflictingPoints.Add(point);
                }
                else if (containingRegions.Count == 0)
                {
                    Debug.WriteLine($"Точка {point.Id} не находится ни в одном регионе");
                }
            }

            return result;
        }

        private bool IsPointInPolygon(double lat, double lng, List<PointLatLng> polygon)
        {
            if (polygon.Count < 3)
                return false;

            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].Lat > lat) != (polygon[j].Lat > lat)) &&
                    (lng < (polygon[j].Lng - polygon[i].Lng) * (lat - polygon[i].Lat) /
                    (polygon[j].Lat - polygon[i].Lat) + polygon[i].Lng))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private void RedistributeConflictingPoints(List<AddressPoint> allPoints, List<Region> regions, IntersectionCheckResult intersectionResult)
        {
            foreach (var point in intersectionResult.ConflictingPoints)
            {
                var containingRegions = intersectionResult.PointRegions[point.Id];

                if (containingRegions.Count > 0)
                {
                    int bestRegion = -1;
                    double minDistance = double.MaxValue;

                    foreach (int regionId in containingRegions)
                    {
                        if (regionId < regions.Count)
                        {
                            var center = regions[regionId].CenterPoint ??
                                         CalculateRegionCenter(regions[regionId]);

                            double distance = CalculateDistance(
                                point.Latitude, point.Longitude,
                                center.Lat, center.Lng);

                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                bestRegion = regionId;
                            }
                        }
                    }

                    foreach (var region in regions)
                    {
                        region.Points.RemoveAll(p => p.Id == point.Id);
                    }

                    if (bestRegion >= 0 && bestRegion < regions.Count)
                    {
                        point.RegionId = bestRegion;
                        regions[bestRegion].Points.Add(point);
                    }
                }
            }

            foreach (var point in allPoints)
            {
                if (intersectionResult.PointRegions.ContainsKey(point.Id) &&
                    intersectionResult.PointRegions[point.Id].Count == 0)
                {
                    int bestRegion = -1;
                    double minDistance = double.MaxValue;

                    for (int i = 0; i < regions.Count; i++)
                    {
                        var center = regions[i].CenterPoint ?? CalculateRegionCenter(regions[i]);
                        double distance = CalculateDistance(
                            point.Latitude, point.Longitude,
                            center.Lat, center.Lng);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            bestRegion = i;
                        }
                    }

                    foreach (var region in regions)
                    {
                        region.Points.RemoveAll(p => p.Id == point.Id);
                    }

                    if (bestRegion >= 0 && bestRegion < regions.Count)
                    {
                        point.RegionId = bestRegion;
                        regions[bestRegion].Points.Add(point);
                    }
                }
            }
        }

        private void BuildRegionPolygons(List<Region> regions)
        {
            foreach (var region in regions)
            {
                region.PolygonPoints = region.ConvexHullPoints;
            }
        }

        private void SaveRegionsToDatabaseOld(List<Region> regions)
        {
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                try
                {
                    string checkColumnQuery = @"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'Метки' AND COLUMN_NAME = 'Участок')
                    BEGIN
                        ALTER TABLE Метки ADD Участок INT
                    END";

                    using (OleDbCommand checkCommand = new OleDbCommand(checkColumnQuery, connection))
                    {
                        checkCommand.ExecuteNonQuery();
                    }
                }
                catch
                {
                }

                foreach (var region in regions)
                {
                    foreach (var point in region.Points)
                    {
                        try
                        {
                            string updateQuery = @"UPDATE Метки SET Участок = ? WHERE [Адрес] = ?";
                            using (OleDbCommand updateCommand = new OleDbCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@region", region.Id);
                                updateCommand.Parameters.AddWithValue("@address", point.Address);
                                updateCommand.ExecuteNonQuery();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Ошибка при обновлении точки {point.Address}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void DisplayRegionsOnMap(List<Region> regions)
        {
            this.Invoke(new Action(() =>
            {
                gMapControl1.Overlays.Clear();
            }));

            GMapOverlay polygonsOverlay = new GMapOverlay("polygons");
            GMapOverlay markersOverlay = new GMapOverlay("markers");

            foreach (var region in regions)
            {
                if (region.PolygonPoints.Count >= 3)
                {
                    GMapPolygon polygon = new GMapPolygon(region.PolygonPoints, region.Id.ToString())
                    {
                        Stroke = new Pen(region.Color, 2),
                        Fill = new SolidBrush(Color.FromArgb(PolygonFillAlpha, region.Color))
                    };
                    polygon.Tag = new PolygonTag
                    {
                        DbId = region.DbId ?? "",
                        Name = region.Name ?? $"Участок {region.Id + 1}",
                        Color = region.Color
                    };
                    polygonsOverlay.Polygons.Add(polygon);
                }

                foreach (var point in region.Points)
                {
                    PointLatLng position = new PointLatLng(point.Latitude, point.Longitude);

                    GMarkerGoogle marker = new GMarkerGoogle(position, GMarkerGoogleType.blue_small);

                    Bitmap colored = CreateSmallCircleBitmap(region.Color);
                    marker = new GMarkerGoogle(position, colored);

                    marker.ToolTipText =
                        $"\nАдрес: {point.Address}" +
                        $"\nТип: {point.BuildingType}" +
                        $"\nУчасток: {region.Id + 1}" +
                        $"\nВремя участка: {region.TotalTime / 60:F1} мин\n";

                    marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;
                    marker.ToolTip.Fill = Brushes.White;
                    marker.ToolTip.Foreground = Brushes.Black;
                    marker.ToolTip.Stroke = Pens.Black;
                    marker.ToolTip.TextPadding = new Size(10, 10);

                    markersOverlay.Markers.Add(marker);
                }

                if (region.CenterPoint.HasValue)
                {
                    GMarkerGoogle centerMarker = new GMarkerGoogle(
                        region.CenterPoint.Value,
                        GMarkerGoogleType.blue_pushpin);
                    centerMarker.ToolTipText = $"Центр участка {region.Id + 1}";
                    markersOverlay.Markers.Add(centerMarker);
                }
            }

            GMarkerGoogle postMarker = new GMarkerGoogle(
                postOfficeLocation,
                GMarkerGoogleType.red_pushpin);
            postMarker.ToolTipText = "Почтовое отделение";
            markersOverlay.Markers.Add(postMarker);

            this.Invoke(new Action(() =>
            {
                gMapControl1.Overlays.Add(polygonsOverlay);
                gMapControl1.Overlays.Add(markersOverlay);
                gMapControl1.Refresh();
            }));
        }

        private void DisplayStatistics(List<Region> regions)
        {
            string stats = "Статистика участков:";

            foreach (var region in regions.OrderBy(r => r.Id))
            {
                stats += $"\nУчасток {region.Id + 1} (Цвет: {ColorToHex(region.Color)}):";
                stats += $"\nКоличество адресов: {region.Points.Count}";
                stats += $"\nКоличество уникальных улиц: {region.UniqueStreetCount}";
                stats += $"\nПримерное время обхода: {region.TotalTime / 60:F1} минут";
                stats += $"\nСреднее расстояние до почты: {region.Points.Average(p => p.DistanceToPostOffice):F0} м\n";
            }

            double avgTime = regions.Average(r => r.TotalTime);
            double maxTime = regions.Max(r => r.TotalTime);
            double minTime = regions.Min(r => r.TotalTime);

            stats += $"\nСреднее время на участок: {avgTime / 60:F1} минут";
            stats += $"\nМаксимальное время: {maxTime / 60:F1} минут";
            stats += $"\nМинимальное время: {minTime / 60:F1} минут";
            stats += $"\nРазница: {(maxTime - minTime) / 60:F1} минут";
            stats += $"\nБалансировка: {((maxTime - minTime) / avgTime * 100):F1}%";

            int totalStreets = regions.Sum(r => r.UniqueStreetCount);
            stats += $"\n\nИтого улиц по участкам: {totalStreets}";
            stats += $"\nСреднее улиц на участок: {regions.Average(r => r.UniqueStreetCount):F1}";

            this.Invoke(new Action(() =>
            {
                MessageBox.Show(stats, "Результаты балансировки");
            }));
        }

        private void UpdateNumberOfRegions()
        {
            using (Form inputForm = new Form())
            {
                inputForm.Text = "Настройка балансировки";
                inputForm.Size = new Size(320, 240);
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.StartPosition = FormStartPosition.CenterParent;

                // ── Количество участков ──────────────────────────────────────
                Label labelRegions = new Label
                {
                    Text = "Количество участков:",
                    Location = new Point(16, 20),
                    Size = new Size(180, 20)
                };
                NumericUpDown nudRegions = new NumericUpDown
                {
                    Minimum = 1,
                    Maximum = 20,
                    Value = numberOfRegions,
                    Location = new Point(200, 18),
                    Width = 80
                };

                // ── Скорость (м/с) ───────────────────────────────────────────
                Label labelMps = new Label
                {
                    Text = "Скорость (м/с):",
                    Location = new Point(16, 60),
                    Size = new Size(180, 20)
                };
                NumericUpDown nudMps = new NumericUpDown
                {
                    Minimum = 0.01m,
                    Maximum = 10m,
                    DecimalPlaces = 2,
                    Increment = 0.05m,
                    Value = (decimal)METER_PER_SECOND,
                    Location = new Point(200, 58),
                    Width = 80
                };

                // ── Время на дом (с) ─────────────────────────────────────────
                Label labelTph = new Label
                {
                    Text = "Время на дом (с):",
                    Location = new Point(16, 100),
                    Size = new Size(180, 20)
                };
                NumericUpDown nudTph = new NumericUpDown
                {
                    Minimum = 1,
                    Maximum = 3600,
                    Value = (decimal)TIME_PER_HOUSE,
                    Location = new Point(200, 98),
                    Width = 80
                };

                // ── Кнопки ───────────────────────────────────────────────────
                Button okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(80, 150),
                    Size = new Size(75, 30)
                };
                Button cancelButton = new Button
                {
                    Text = "Отмена",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(165, 150),
                    Size = new Size(75, 30)
                };

                inputForm.Controls.AddRange(new Control[]
                {
                    labelRegions, nudRegions,
                    labelMps, nudMps,
                    labelTph, nudTph,
                    okButton, cancelButton
                });
                inputForm.AcceptButton = okButton;
                inputForm.CancelButton = cancelButton;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    numberOfRegions = (int)nudRegions.Value;
                    METER_PER_SECOND = (double)nudMps.Value;
                    TIME_PER_HOUSE = (double)nudTph.Value;
                    MessageBox.Show(
                        $"Установлено: {numberOfRegions} участков, " +
                        $"скорость {METER_PER_SECOND:F2} м/с, " +
                        $"{TIME_PER_HOUSE} с на дом.",
                        "Параметры сохранены", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                Cursor = Cursors.Default;
                ButtonEnabledInBalancedRegions(true);
                UpdateStatusLabel("Операция отменяется...");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            UpdateNumberOfRegions();
        }

        private List<Region> LoadRegionsFromNewTables(string cs = null)
        {
            if (cs == null) cs = connectionString;

            var regions = new List<Region>();

            using (var connection = new OleDbConnection(cs))
            {
                connection.Open();

                string selectRegions = @"
                    SELECT [id], [Название], [Цвет], [Долгота], [Широта]
                    FROM Участки";

                using (var rCmd = new OleDbCommand(selectRegions, connection))
                using (var rReader = rCmd.ExecuteReader())
                {
                    int idx = 0;
                    while (rReader.Read())
                    {
                        var region = new Region();
                        region.DbId = rReader["id"].ToString();
                        region.Id = idx++;
                        region.Name = rReader["Название"]?.ToString() ?? $"Участок {region.Id + 1}";

                        string colorString = rReader["Цвет"].ToString();
                        Color c;
                        try
                        {
                            // Проверяем формат HEX
                            if (colorString.StartsWith("#"))
                            {
                                c = HexToColor(colorString);
                            }
                            else
                            {
                                // Обратная совместимость с RGB форматом
                                var rgb = colorString.Split(',');
                                if (rgb.Length == 3 &&
                                    int.TryParse(rgb[0], out int r) &&
                                    int.TryParse(rgb[1], out int g) &&
                                    int.TryParse(rgb[2], out int b))
                                {
                                    c = Color.FromArgb(r, g, b);
                                }
                                else
                                {
                                    c = regionColors[region.Id % regionColors.Length];
                                }
                            }
                        }
                        catch
                        {
                            c = regionColors[region.Id % regionColors.Length];
                        }

                        region.Color = c;

                        double lng = Convert.ToDouble(rReader["Долгота"]);
                        double lat = Convert.ToDouble(rReader["Широта"]);
                        region.CenterPoint = new PointLatLng(lat, lng);

                        string selectNodes = @"
                            SELECT [Долгота], [Широта], [Номер]
                            FROM Узлы
                            WHERE [Id участка] = ?
                            ORDER BY [Номер]";

                        using (var nCmd = new OleDbCommand(selectNodes, connection))
                        {
                            nCmd.Parameters.AddWithValue("@id_участка", region.DbId);

                            using (var nReader = nCmd.ExecuteReader())
                            {
                                while (nReader.Read())
                                {
                                    double nodeLng = Convert.ToDouble(nReader["Долгота"]);
                                    double nodeLat = Convert.ToDouble(nReader["Широта"]);
                                    region.PolygonPoints.Add(new PointLatLng(nodeLat, nodeLng));
                                }
                            }
                        }

                        regions.Add(region);
                    }
                }
            }

            return regions;
        }

        private class RegionPolygon
        {
            public string Id { get; set; }
            public List<PointLatLng> Points { get; set; } = new List<PointLatLng>();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                RebuildMarkersRegionIds();
                UpdateMarkersColors();
                MessageBox.Show("Пересборка id_участка у меток завершена!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при пересборке id_участка: " + ex.Message);
            }
        }

        private void UpdateMarkersColors()
        {
            // Загружаем полигоны всех участков
            var regionPolygons = LoadRegionPolygonsWithColors();

            // Обновляем цвета маркеров на карте
            foreach (var overlay in gMapControl1.Overlays)
            {
                foreach (var marker in overlay.Markers.OfType<GMarkerGoogle>().ToList())
                {
                    double lat = marker.Position.Lat;
                    double lng = marker.Position.Lng;

                    // Ищем участок, в котором находится эта метка
                    Color markerColor = Color.Gray; // По умолчанию серый

                    foreach (var region in regionPolygons)
                    {
                        if (region.Points.Count >= 3 && IsPointInPolygon(lat, lng, region.Points))
                        {
                            markerColor = region.Color;
                            break;
                        }
                    }

                    // Создаем новый кастомный маркер с нужным цветом, сохраняя MarkerTag
                    Bitmap customMarker = CreateSmallCircleBitmap(markerColor);
                    var newMarker = new GMarkerGoogle(marker.Position, customMarker);

                    // Переносим MarkerTag (или строим из тултипа если Tag старого формата)
                    var existingTag = GetMarkerTag(marker);
                    ApplyMarkerTag(newMarker, existingTag);

                    // Заменяем старый маркер на новый
                    int index = overlay.Markers.IndexOf(marker);
                    overlay.Markers.Remove(marker);
                    overlay.Markers.Insert(index, newMarker);
                }
            }

            gMapControl1.Refresh();
        }

        private class RegionPolygonWithColor
        {
            public string Id { get; set; }
            public List<PointLatLng> Points { get; set; } = new List<PointLatLng>();
            public Color Color { get; set; }
        }

        private List<RegionPolygonWithColor> LoadRegionPolygonsWithColors()
        {
            var regions = new List<RegionPolygonWithColor>();

            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                string selectRegions = "SELECT [id], [Цвет] FROM Участки";

                using (var rCmd = new OleDbCommand(selectRegions, connection))
                using (var rReader = rCmd.ExecuteReader())
                {
                    while (rReader.Read())
                    {
                        var region = new RegionPolygonWithColor
                        {
                            Id = rReader["id"].ToString()
                        };

                        string colorString = rReader["Цвет"].ToString();
                        try
                        {
                            if (colorString.StartsWith("#"))
                            {
                                region.Color = HexToColor(colorString);
                            }
                            else
                            {
                                var rgb = colorString.Split(',');
                                if (rgb.Length == 3 &&
                                    int.TryParse(rgb[0], out int r) &&
                                    int.TryParse(rgb[1], out int g) &&
                                    int.TryParse(rgb[2], out int b))
                                {
                                    region.Color = Color.FromArgb(r, g, b);
                                }
                                else
                                {
                                    region.Color = Color.Gray;
                                }
                            }
                        }
                        catch
                        {
                            region.Color = Color.Gray;
                        }

                        string selectNodes = "SELECT [Долгота], [Широта], [Номер] FROM Узлы WHERE [Id участка] = ? ORDER BY [Номер]";

                        using (var nCmd = new OleDbCommand(selectNodes, connection))
                        {
                            nCmd.Parameters.AddWithValue("@id_участка", region.Id);

                            using (var nReader = nCmd.ExecuteReader())
                            {
                                while (nReader.Read())
                                {
                                    double lng = Convert.ToDouble(nReader["Долгота"]);
                                    double lat = Convert.ToDouble(nReader["Широта"]);
                                    region.Points.Add(new PointLatLng(lat, lng));
                                }
                            }
                        }

                        regions.Add(region);
                    }
                }
            }

            return regions;
        }

        private void RebuildMarkersRegionIds()
        {
            // Загружаем полигоны всех участков
            var regionPolygons = LoadRegionPolygons();

            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                // Загружаем все метки
                string selectMarkers = "SELECT [id], [Широта], [Долгота] FROM Метки";
                var markersToUpdate = new List<(string id, double lat, double lng, string regionId)>();

                using (var mCmd = new OleDbCommand(selectMarkers, connection))
                using (var mReader = mCmd.ExecuteReader())
                {
                    while (mReader.Read())
                    {
                        string markerId = mReader["id"].ToString();
                        double lat = Convert.ToDouble(mReader["Широта"]);
                        double lng = Convert.ToDouble(mReader["Долгота"]);

                        // Ищем участок, в котором находится эта метка
                        string foundRegionId = null; // NULL для меток, не принадлежащих участкам

                        foreach (var region in regionPolygons)
                        {
                            if (region.Points.Count >= 3 && IsPointInPolygon(lat, lng, region.Points))
                            {
                                foundRegionId = region.Id;
                                break; // Нашли первый подходящий участок
                            }
                        }

                        markersToUpdate.Add((markerId, lat, lng, foundRegionId));
                    }
                }

                // Обновляем id_участка для каждой метки пакетами
                string updateSql = "UPDATE Метки SET [Id участка] = ? WHERE [id] = ?";

                int batchSize = 100;
                int totalUpdated = 0;

                for (int batchStart = 0; batchStart < markersToUpdate.Count; batchStart += batchSize)
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int batchEnd = Math.Min(batchStart + batchSize, markersToUpdate.Count);

                            for (int i = batchStart; i < batchEnd; i++)
                            {
                                var marker = markersToUpdate[i];

                                using (var uCmd = new OleDbCommand(updateSql, connection, transaction))
                                {
                                    if (marker.regionId != null)
                                    {
                                        uCmd.Parameters.AddWithValue("@id_участка", marker.regionId);
                                    }
                                    else
                                    {
                                        uCmd.Parameters.AddWithValue("@id_участка", DBNull.Value);
                                    }
                                    uCmd.Parameters.AddWithValue("@id", marker.id);
                                    uCmd.ExecuteNonQuery();
                                    totalUpdated++;
                                }
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                Debug.WriteLine($"Обновлено меток: {totalUpdated}");
            }
        }

        private List<RegionPolygon> LoadRegionPolygons()
        {
            var regions = new List<RegionPolygon>();

            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                string selectRegions = "SELECT [id] FROM Участки";

                using (var rCmd = new OleDbCommand(selectRegions, connection))
                using (var rReader = rCmd.ExecuteReader())
                {
                    while (rReader.Read())
                    {
                        var region = new RegionPolygon
                        {
                            Id = rReader["id"].ToString()
                        };

                        string selectNodes = "SELECT [Долгота], [Широта] FROM Узлы WHERE [Id участка] = ? ORDER BY [Номер]";

                        using (var nCmd = new OleDbCommand(selectNodes, connection))
                        {
                            nCmd.Parameters.AddWithValue("@id_участка", region.Id);

                            using (var nReader = nCmd.ExecuteReader())
                            {
                                while (nReader.Read())
                                {
                                    double lng = Convert.ToDouble(nReader["Долгота"]);
                                    double lat = Convert.ToDouble(nReader["Широта"]);
                                    region.Points.Add(new PointLatLng(lat, lng));
                                }
                            }
                        }

                        regions.Add(region);
                    }
                }
            }

            return regions;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                OptimizeRegionBoundaries();
                MessageBox.Show("Оптимизация границ участков завершена! Перезагрузите участки из БД для просмотра результата.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при оптимизации границ участков: " + ex.Message);
            }
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С ЦВЕТОМ ==========

        /// <summary>
        /// Оптимизирует границы конкретного участка по его ID
        /// </summary>
        private void OptimizeSingleRegionBoundary(string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
                return;

            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                // Загружаем узлы участка
                var points = new List<PointLatLng>();
                string selectNodes = "SELECT [Долгота], [Широта], [Номер] FROM Узлы WHERE [Id участка] = ? ORDER BY [Номер]";

                using (var cmd = new OleDbCommand(selectNodes, connection))
                {
                    cmd.Parameters.AddWithValue("@id_участка", regionId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            double lng = Convert.ToDouble(reader["Долгота"]);
                            double lat = Convert.ToDouble(reader["Широта"]);
                            points.Add(new PointLatLng(lat, lng));
                        }
                    }
                }

                if (points.Count < 3)
                    return;

                // Оптимизируем порядок точек
                var optimizedPoints = OptimizePolygonBoundary(points);

                // Удаляем старые узлы этого участка
                string deleteNodes = "DELETE FROM Узлы WHERE [Id участка] = ?";
                using (var cmd = new OleDbCommand(deleteNodes, connection))
                {
                    cmd.Parameters.AddWithValue("@id_участка", regionId);
                    cmd.ExecuteNonQuery();
                }

                // Сохраняем оптимизированные узлы
                string insertNode = @"
                    INSERT INTO Узлы ([id], [Долгота], [Широта], [Id участка], [Номер])
                    VALUES (?, ?, ?, ?, ?)";

                int nodeNumber = 1;
                foreach (var point in optimizedPoints)
                {
                    using (var cmd = new OleDbCommand(insertNode, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                        cmd.Parameters.AddWithValue("@Долгота", Math.Round(point.Lng, 6));
                        cmd.Parameters.AddWithValue("@Широта", Math.Round(point.Lat, 6));
                        cmd.Parameters.AddWithValue("@id_участка", regionId);
                        cmd.Parameters.AddWithValue("@Номер", nodeNumber++);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Находит ближайший узел участка к указанным координатам экрана
        /// </summary>
        private (string regionId, PointLatLng nodePoint, int nodeIndex)? FindNearestNode(int mouseX, int mouseY, double maxDistance = 10)
        {
            if (balancedRegions == null)
                return null;

            double minDist = maxDistance;
            string foundRegionId = null;
            PointLatLng foundPoint = default;
            int foundIndex = -1;

            foreach (var region in balancedRegions)
            {
                for (int i = 0; i < region.PolygonPoints.Count; i++)
                {
                    var nodePoint = gMapControl1.FromLatLngToLocal(region.PolygonPoints[i]);
                    double distance = Math.Sqrt(Math.Pow(nodePoint.X - mouseX, 2) + Math.Pow(nodePoint.Y - mouseY, 2));

                    if (distance < minDist)
                    {
                        minDist = distance;
                        foundRegionId = region.DbId;
                        foundPoint = region.PolygonPoints[i];
                        foundIndex = i;
                    }
                }
            }

            if (foundRegionId != null)
                return (foundRegionId, foundPoint, foundIndex);

            return null;
        }

        /// <summary>
        /// Обновляет узел участка в БД
        /// </summary>
        private void UpdateNodeInDatabase(string regionId, PointLatLng oldPoint, PointLatLng newPoint)
        {
            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                // Находим узел по старым координатам
                string updateNode = @"
                    UPDATE Узлы 
                    SET [Долгота] = ?, [Широта] = ?
                    WHERE [Id участка] = ? 
                    AND ABS([Долгота] - ?) < 0.000001 
                    AND ABS([Широта] - ?) < 0.000001";

                using (var cmd = new OleDbCommand(updateNode, connection))
                {
                    cmd.Parameters.AddWithValue("@new_lng", Math.Round(newPoint.Lng, 6));
                    cmd.Parameters.AddWithValue("@new_lat", Math.Round(newPoint.Lat, 6));
                    cmd.Parameters.AddWithValue("@id_участка", regionId);
                    cmd.Parameters.AddWithValue("@old_lng", Math.Round(oldPoint.Lng, 6));
                    cmd.Parameters.AddWithValue("@old_lat", Math.Round(oldPoint.Lat, 6));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Удаляет узел участка из БД и пересчитывает номера
        /// </summary>
        private void DeleteNodeFromDatabase(string regionId, PointLatLng nodePoint)
        {
            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                // Удаляем узел
                string deleteNode = @"
                    DELETE FROM Узлы 
                    WHERE [Id участка] = ? 
                    AND ABS([Долгота] - ?) < 0.000001 
                    AND ABS([Широта] - ?) < 0.000001";

                using (var cmd = new OleDbCommand(deleteNode, connection))
                {
                    cmd.Parameters.AddWithValue("@id_участка", regionId);
                    cmd.Parameters.AddWithValue("@lng", Math.Round(nodePoint.Lng, 6));
                    cmd.Parameters.AddWithValue("@lat", Math.Round(nodePoint.Lat, 6));
                    cmd.ExecuteNonQuery();
                }

                // Пересчитываем номера оставшихся узлов
                var nodes = new List<(string id, double lng, double lat)>();
                string selectNodes = "SELECT [id], [Долгота], [Широта] FROM Узлы WHERE [Id участка] = ? ORDER BY [Номер]";

                using (var cmd = new OleDbCommand(selectNodes, connection))
                {
                    cmd.Parameters.AddWithValue("@id_участка", regionId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nodes.Add((
                                reader["id"].ToString(),
                                Convert.ToDouble(reader["Долгота"]),
                                Convert.ToDouble(reader["Широта"])
                            ));
                        }
                    }
                }

                // Обновляем номера
                string updateNumber = "UPDATE Узлы SET [Номер] = ? WHERE [id] = ?";
                int number = 1;
                foreach (var node in nodes)
                {
                    using (var cmd = new OleDbCommand(updateNumber, connection))
                    {
                        cmd.Parameters.AddWithValue("@Номер", number++);
                        cmd.Parameters.AddWithValue("@id", node.id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Конвертирует Color в HEX формат (#RRGGBB)
        /// </summary>
        private string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// Конвертирует HEX формат в Color
        /// </summary>
        private Color HexToColor(string hex)
        {
            try
            {
                return ColorTranslator.FromHtml(hex);
            }
            catch
            {
                return Color.Gray; // Цвет по умолчанию
            }
        }

        // ========== МЕТОДЫ ДЛЯ ОПТИМИЗАЦИИ ГРАНИЦ РЕГИОНОВ ==========

        /// <summary>
        /// Вычисляет направление поворота для трех точек (векторное произведение)
        /// </summary>
        private double Direction(PointLatLng p1, PointLatLng p2, PointLatLng p3)
        {
            return (p3.Lng - p1.Lng) * (p2.Lat - p1.Lat) -
                   (p2.Lng - p1.Lng) * (p3.Lat - p1.Lat);
        }

        /// <summary>
        /// Проверяет, пересекаются ли два отрезка
        /// </summary>
        private bool DoSegmentsIntersect(PointLatLng p1, PointLatLng p2, PointLatLng p3, PointLatLng p4)
        {
            double d1 = Direction(p3, p4, p1);
            double d2 = Direction(p3, p4, p2);
            double d3 = Direction(p1, p2, p3);
            double d4 = Direction(p1, p2, p4);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Подсчитывает количество пересечений ребер многоугольника
        /// </summary>
        private int CountPolygonIntersections(List<PointLatLng> points)
        {
            if (points.Count < 3)
                return 0;

            int count = 0;
            int n = points.Count;

            for (int i = 0; i < n; i++)
            {
                PointLatLng p1 = points[i];
                PointLatLng p2 = points[(i + 1) % n];

                // Проверяем пересечение с несмежными ребрами
                for (int j = i + 2; j < n; j++)
                {
                    // Пропускаем смежные ребра
                    if (j == (i + n - 1) % n)
                        continue;

                    PointLatLng p3 = points[j];
                    PointLatLng p4 = points[(j + 1) % n];

                    if (DoSegmentsIntersect(p1, p2, p3, p4))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Оптимизирует порядок узлов многоугольника для минимизации пересечений
        /// </summary>
        private List<PointLatLng> OptimizePolygonBoundary(List<PointLatLng> points)
        {
            if (points.Count < 3)
                return points;

            var bestPoints = new List<PointLatLng>(points);
            int bestIntersections = CountPolygonIntersections(bestPoints);

            // Если пересечений нет, возвращаем исходный список
            if (bestIntersections == 0)
                return bestPoints;

            const int maxIterations = 1000;
            int noImprovementCount = 0;
            var currentPoints = new List<PointLatLng>(points);

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                bool improved = false;

                // Пробуем swap соседних узлов
                for (int i = 0; i < currentPoints.Count; i++)
                {
                    int j = (i + 1) % currentPoints.Count;

                    // Меняем местами узлы i и j
                    var tempPoints = new List<PointLatLng>(currentPoints);
                    var temp = tempPoints[i];
                    tempPoints[i] = tempPoints[j];
                    tempPoints[j] = temp;

                    int intersections = CountPolygonIntersections(tempPoints);

                    if (intersections < bestIntersections)
                    {
                        bestPoints = new List<PointLatLng>(tempPoints);
                        bestIntersections = intersections;
                        improved = true;

                        // Если достигли нуля пересечений, можно остановиться
                        if (bestIntersections == 0)
                            return bestPoints;
                    }
                }

                if (!improved)
                {
                    noImprovementCount++;
                    if (noImprovementCount >= 10)
                        break;
                }
                else
                {
                    noImprovementCount = 0;
                    currentPoints = new List<PointLatLng>(bestPoints);
                }
            }

            return bestPoints;
        }

        /// <summary>
        /// Оптимизирует границы всех регионов и сохраняет в БД
        /// </summary>
        private void OptimizeRegionBoundaries()
        {
            // Загружаем текущие регионы из базы данных
            var regions = LoadRegionsFromNewTables();

            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                // Очищаем таблицу Узлы
                using (var cmd = new OleDbCommand("DELETE FROM Узлы", connection))
                    cmd.ExecuteNonQuery();

                // Для каждого региона оптимизируем точки и сохраняем заново
                foreach (var region in regions)
                {
                    if (region.PolygonPoints.Count < 3)
                        continue;

                    // Оптимизируем точки для минимизации пересечений
                    var optimizedPoints = OptimizePolygonBoundary(region.PolygonPoints);

                    // Сохраняем оптимизированные точки обратно в таблицу Узлы
                    string insertNode = @"
                        INSERT INTO Узлы ([id], [Долгота], [Широта], [Id участка], [Номер])
                        VALUES (?, ?, ?, ?, ?)";

                    int nodeNumber = 1;
                    foreach (var point in optimizedPoints)
                    {
                        using (var nCmd = new OleDbCommand(insertNode, connection))
                        {
                            string nodeGuid = Guid.NewGuid().ToString();

                            nCmd.Parameters.AddWithValue("@id", nodeGuid);
                            nCmd.Parameters.AddWithValue("@Долгота", Math.Round(point.Lng, 6));
                            nCmd.Parameters.AddWithValue("@Широта", Math.Round(point.Lat, 6));
                            nCmd.Parameters.AddWithValue("@id_участка", region.DbId);
                            nCmd.Parameters.AddWithValue("@Номер", nodeNumber++);
                            nCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        // ========== МЕТОДЫ ДЛЯ ЗАГРУЗКИ МАРКЕРОВ ИЗ БД ==========

        /// <summary>
        /// Формирует tooltip для маркера из данных БД
        /// </summary>
        private string BuildMarkerTooltip(OleDbDataReader reader)
        {
            var lines = new List<string>();

            string street = reader["Улица"]?.ToString();
            string house = reader["Дом"]?.ToString();
            string corpus = reader["Корпус"]?.ToString();
            string flat = reader["Квартира"]?.ToString();
            string buildingType = reader["Тип здания"]?.ToString();

            if (!string.IsNullOrWhiteSpace(street)) lines.Add($"\nУлица: {street}");
            if (!string.IsNullOrWhiteSpace(house)) lines.Add($"\nДом: {house}");
            if (!string.IsNullOrWhiteSpace(corpus)) lines.Add($"\nКорпус: {corpus}");
            if (!string.IsNullOrWhiteSpace(flat)) lines.Add($"\nКвартира: {flat}");
            if (!string.IsNullOrWhiteSpace(buildingType)) lines.Add($"\nТип: {buildingType}\n");

            return lines.Count > 0 ? string.Join("", lines) : "Адрес: данные не заполнены";
        }

        /// <summary>
        /// Загружает маркеры из БД и отображает их на карте
        /// </summary>
        private void LoadMarkersFromDatabase(List<Region> regions, string cs = null)
        {
            if (cs == null) cs = connectionString;
            var regionDict = regions.ToDictionary(r => r.DbId, r => r);

            // Создаем или очищаем overlay для маркеров
            var markersOverlay = gMapControl1.Overlays.FirstOrDefault(o => o.Id == "markers");
            if (markersOverlay == null)
            {
                markersOverlay = new GMapOverlay("markers");
                gMapControl1.Overlays.Add(markersOverlay);
            }
            else
            {
                markersOverlay.Markers.Clear();
            }

            using (var connection = new OleDbConnection(cs))
            {
                connection.Open();

                string selectMarkers = @"
                    SELECT [id], [Долгота], [Широта], [Тип здания], 
                           [Улица], [Дом], [Корпус], [Квартира], [Id участка]
                    FROM Метки
                    WHERE [Id участка] IS NOT NULL";

                using (var cmd = new OleDbCommand(selectMarkers, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            double lng = Convert.ToDouble(reader["Долгота"]);
                            double lat = Convert.ToDouble(reader["Широта"]);
                            string regionId = reader["id_участка"]?.ToString();

                            var point = new PointLatLng(lat, lng);

                            // Определяем цвет маркера по региону
                            Color markerColor = Color.Blue;
                            if (!string.IsNullOrEmpty(regionId) && regionDict.ContainsKey(regionId))
                            {
                                markerColor = regionDict[regionId].Color;
                            }

                            var marker = new GMarkerGoogle(point, GMarkerGoogleType.blue_small);
                            var tagDb = new MarkerTag
                            {
                                DbId = reader["id"]?.ToString() ?? "",
                                Street = reader["Улица"]?.ToString() ?? "",
                                House = reader["Дом"]?.ToString() ?? "",
                                Corpus = reader["Корпус"]?.ToString() ?? "",
                                Flat = reader["Квартира"]?.ToString() ?? "",
                                BuildingType = reader["Тип здания"]?.ToString() ?? ""
                            };
                            ApplyMarkerTag(marker, tagDb);

                            markersOverlay.Markers.Add(marker);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Ошибка загрузки маркера: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void LoadMarkersFromDatabaseToMap(string cs = null)
        {
            if (cs == null) cs = connectionString;
            // Загружаем полигоны всех участков для определения цвета
            var regionPolygons = LoadRegionPolygonsWithColors();

            // Создаем или очищаем overlay для маркеров
            var markersOverlay = gMapControl1.Overlays.FirstOrDefault(o => o.Id == "markers");
            if (markersOverlay == null)
            {
                markersOverlay = new GMapOverlay("markers");
                gMapControl1.Overlays.Add(markersOverlay);
            }
            else
            {
                markersOverlay.Markers.Clear();
            }

            using (var connection = new OleDbConnection(cs))
            {
                connection.Open();

                string selectMarkers = @"
                    SELECT [id], [Долгота], [Широта], [Тип здания], 
                           [Улица], [Дом], [Корпус], [Квартира], [Id участка]
                    FROM Метки";

                using (var cmd = new OleDbCommand(selectMarkers, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            double lng = Convert.ToDouble(reader["Долгота"]);
                            double lat = Convert.ToDouble(reader["Широта"]);

                            var point = new PointLatLng(lat, lng);

                            // Определяем цвет маркера по фактическому положению на карте
                            Color markerColor = Color.Gray; // По умолчанию серый

                            foreach (var region in regionPolygons)
                            {
                                if (region.Points.Count >= 3 && IsPointInPolygon(lat, lng, region.Points))
                                {
                                    markerColor = region.Color;
                                    break;
                                }
                            }

                            // Создаем кастомный маркер
                            Bitmap customMarker = CreateSmallCircleBitmap(markerColor);
                            var marker = new GMarkerGoogle(point, customMarker);

                            var tagAccess = new MarkerTag
                            {
                                DbId = reader["id"]?.ToString() ?? "",
                                Street = reader["Улица"]?.ToString() ?? "",
                                House = reader["Дом"]?.ToString() ?? "",
                                Corpus = reader["Корпус"]?.ToString() ?? "",
                                Flat = reader["Квартира"]?.ToString() ?? "",
                                BuildingType = reader["Тип здания"]?.ToString() ?? ""
                            };
                            ApplyMarkerTag(marker, tagAccess);

                            markersOverlay.Markers.Add(marker);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Ошибка загрузки маркера: {ex.Message}");
                        }
                    }
                }
            }

            gMapControl1.Refresh();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (currentMapMode == MapMode.CreateMarker)
            {
                // Отключаем режим
                DeactivateMapMode();
            }
            else
            {
                // Включаем режим создания меток
                DeactivateMapMode();
                currentMapMode = MapMode.CreateMarker;
                button11.Text = "Прекратить";
                gMapControl1.Cursor = Cursors.Cross;
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (currentMapMode == MapMode.MoveObject)
            {
                // Отключаем режим
                DeactivateMapMode();
            }
            else
            {
                // Включаем режим перемещения
                DeactivateMapMode();
                currentMapMode = MapMode.MoveObject;
                button12.Text = "Прекратить";
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (currentMapMode == MapMode.DeleteObject)
            {
                // Отключаем режим
                DeactivateMapMode();
            }
            else
            {
                // Включаем режим удаления
                DeactivateMapMode();
                currentMapMode = MapMode.DeleteObject;
                button13.Text = "Прекратить";
                gMapControl1.Cursor = Cursors.Cross;
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (currentMapMode == MapMode.CreateNode)
            {
                // Отключаем режим
                DeactivateMapMode();
            }
            else
            {
                // Включаем режим создания узлов
                DeactivateMapMode();
                currentMapMode = MapMode.CreateNode;
                button14.Text = "Прекратить";
                gMapControl1.Cursor = Cursors.Cross;

                // Обновляем comboBox1 с названиями участков
                UpdateRegionsComboBox();
            }
        }

        private void DeactivateMapMode()
        {
            currentMapMode = MapMode.None;
            gMapControl1.Cursor = Cursors.Default;
            selectedMarkerToMove = null;

            // Восстанавливаем текст кнопок
            button11.Text = "Создать метку";
            button12.Text = "Переместить";
            button13.Text = "Удалить";
            button14.Text = "Создать узел";
        }

        private void UpdateRegionsComboBox()
        {
            if (comboBox1 == null) return;

            comboBox1.Items.Clear();

            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                string selectRegions = "SELECT [Название] FROM Участки ORDER BY [Название]";

                using (var cmd = new OleDbCommand(selectRegions, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        comboBox1.Items.Add(reader["Название"].ToString());
                    }
                }
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Вы действительно хотите полностью очистить таблицу Метки? Это действие нельзя отменить!",
                "Подтверждение очистки",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (var connection = new OleDbConnection(connectionString))
                    {
                        connection.Open();
                        using (var cmd = new OleDbCommand("DELETE FROM Метки", connection))
                        {
                            int deleted = cmd.ExecuteNonQuery();
                            MessageBox.Show($"Таблица Метки очищена. Удалено записей: {deleted}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при очистке таблицы Метки: " + ex.Message);
                }
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Вы действительно хотите полностью очистить таблицы Участки и Узлы? Это действие нельзя отменить!",
                "Подтверждение очистки",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (var connection = new OleDbConnection(connectionString))
                    {
                        connection.Open();

                        using (var cmd = new OleDbCommand("DELETE FROM Узлы", connection))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new OleDbCommand("DELETE FROM Участки", connection))
                        {
                            int deleted = cmd.ExecuteNonQuery();
                            MessageBox.Show($"Таблицы Участки и Узлы очищены. Удалено участков: {deleted}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при очистке таблиц Участки и Узлы: " + ex.Message);
                }
            }
        }

        // button19 - Очистить все таблицы в БД
        private void button19_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Вы действительно хотите полностью очистить ВСЕ таблицы (Метки, Участки, Узлы)? Это действие нельзя отменить!",
                "Подтверждение очистки",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (var connection = new OleDbConnection(connectionString))
                    {
                        connection.Open();

                        using (var cmd = new OleDbCommand("DELETE FROM Метки", connection))
                            cmd.ExecuteNonQuery();

                        using (var cmd = new OleDbCommand("DELETE FROM Узлы", connection))
                            cmd.ExecuteNonQuery();

                        using (var cmd = new OleDbCommand("DELETE FROM Участки", connection))
                            cmd.ExecuteNonQuery();

                        MessageBox.Show("Все таблицы очищены!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при очистке таблиц: " + ex.Message);
                }
            }
        }

        // button23 - Сделать резервную копию БД (access)
        private void button23_Click(object sender, EventArgs e)
        {
            try
            {
                EnsureAccessDatabaseStructure();
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "Access Database|*.accdb";
                    saveDialog.Title = "Сохранить резервную копию БД";
                    saveDialog.FileName = $"BD_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.accdb";
                    saveDialog.InitialDirectory = Path.Combine(Application.StartupPath, "data");

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        string sourcePath = Path.Combine(Application.StartupPath, "data", "BD.accdb");
                        File.Copy(sourcePath, saveDialog.FileName, true);
                        MessageBox.Show($"Резервная копия сохранена:\n{saveDialog.FileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании резервной копии: " + ex.Message);
            }
        }

        // Вспомогательные методы для работы с сервером PostgreSQL
        private async Task TransferMarkersToServer(string sourceCs = null)
        {
            if (sourceCs == null) sourceCs = connectionString;
            string pgConn = Program.BuildPgConnectionString();

            using (var pg = new NpgsqlConnection(pgConn))
            {
                await pg.OpenAsync();

                // Снимаем FK чтобы вставить метки без привязки к участку
                using (var cmd = new NpgsqlCommand(@"
            ALTER TABLE ""Метки"" DROP CONSTRAINT IF EXISTS ""Метки_Id участка_fkey"";", pg))
                    await cmd.ExecuteNonQueryAsync();

                // Очищаем таблицу Метки на сервере
                using (var cmd = new NpgsqlCommand("DELETE FROM \"Метки\"", pg))
                    await cmd.ExecuteNonQueryAsync();

                // Загружаем метки из Access
                using (var access = new OleDbConnection(sourceCs))
                {
                    access.Open();
                    string selectSql = "SELECT [id], [Долгота], [Широта], [Тип здания], [Улица], [Дом], [Корпус], [Квартира], [Id участка] FROM Метки";
                    using (var cmd = new OleDbCommand(selectSql, access))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string rawId = reader["id"].ToString().Trim('{', '}');
                            object regionIdObj = DBNull.Value;
                            if (reader["Id участка"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["Id участка"].ToString()))
                            {
                                if (Guid.TryParse(reader["Id участка"].ToString().Trim('{', '}'), out Guid rg))
                                    regionIdObj = rg;
                            }

                            string insertSql = @"INSERT INTO ""Метки"" (""id"", ""Широта"", ""Долгота"", ""Тип здания"", ""Улица"", ""Дом"", ""Корпус"", ""Квартира"", ""Id участка"", ""Id читателей"")
                                        VALUES (@id, @lat, @lon, @type, @street, @house, @building, @apartment, @region, @readers)
                                        ON CONFLICT (""id"") DO UPDATE SET
                                            ""Широта""=EXCLUDED.""Широта"", ""Долгота""=EXCLUDED.""Долгота"",
                                            ""Тип здания""=EXCLUDED.""Тип здания"", ""Улица""=EXCLUDED.""Улица"",
                                            ""Дом""=EXCLUDED.""Дом"", ""Корпус""=EXCLUDED.""Корпус"",
                                            ""Квартира""=EXCLUDED.""Квартира"", ""Id участка""=EXCLUDED.""Id участка"",
                                            ""Id читателей""=EXCLUDED.""Id читателей""";

                            using (var ins = new NpgsqlCommand(insertSql, pg))
                            {
                                ins.Parameters.AddWithValue("@id", Guid.Parse(rawId));
                                ins.Parameters.AddWithValue("@lat", Convert.ToDouble(reader["Широта"]));
                                ins.Parameters.AddWithValue("@lon", Convert.ToDouble(reader["Долгота"]));
                                ins.Parameters.AddWithValue("@type", reader["Тип здания"]?.ToString() ?? "");
                                ins.Parameters.AddWithValue("@street", reader["Улица"]?.ToString() ?? "");
                                ins.Parameters.AddWithValue("@house", reader["Дом"]?.ToString() ?? "");
                                ins.Parameters.AddWithValue("@building", reader["Корпус"]?.ToString() ?? "");
                                ins.Parameters.AddWithValue("@apartment", reader["Квартира"]?.ToString() ?? "");
                                ins.Parameters.AddWithValue("@region", regionIdObj);
                                ins.Parameters.AddWithValue("@readers", ""); // пустая строка вместо reader["Id читателей"]
                                await ins.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // Восстанавливаем FK
                using (var cmd = new NpgsqlCommand(@"
            ALTER TABLE ""Метки"" ADD CONSTRAINT ""Метки_Id участка_fkey""
                FOREIGN KEY (""Id участка"") REFERENCES ""Участки""(id) ON DELETE CASCADE;", pg))
                    await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task TransferRegionsToServer(string sourceCs = null)
        {
            if (sourceCs == null) sourceCs = connectionString;
            string pgConn = Program.BuildPgConnectionString();

            using (var pg = new NpgsqlConnection(pgConn))
            {
                await pg.OpenAsync();

                // Снимаем FK чтобы вставить участки без привязки к сотруднику
                using (var cmd = new NpgsqlCommand(@"
            ALTER TABLE ""Участки"" DROP CONSTRAINT IF EXISTS ""Участки_Id сотрудника_fkey"";
            ALTER TABLE ""Метки"" DROP CONSTRAINT IF EXISTS ""Метки_Id участка_fkey"";
            ALTER TABLE ""Узлы"" DROP CONSTRAINT IF EXISTS ""Узлы_Id участка_fkey"";", pg))
                    await cmd.ExecuteNonQueryAsync();

                // Сначала очищаем Узлы (зависят от Участков), потом Участки
                using (var cmd = new NpgsqlCommand("DELETE FROM \"Узлы\"", pg))
                    await cmd.ExecuteNonQueryAsync();
                using (var cmd = new NpgsqlCommand("DELETE FROM \"Участки\"", pg))
                    await cmd.ExecuteNonQueryAsync();

                using (var access = new OleDbConnection(sourceCs))
                {
                    access.Open();

                    // Переносим участки
                    string selectRegions = "SELECT [id], [Название], [Цвет] FROM Участки";
                    using (var rCmd = new OleDbCommand(selectRegions, access))
                    using (var reader = rCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string regionId = reader["id"].ToString().Trim('{', '}');
                            string insertRegion = @"INSERT INTO ""Участки"" (""id"", ""Название"", ""Цвет"", ""Id сотрудника"")
                                           VALUES (@id, @name, @color, @idEmployee)
                                           ON CONFLICT (""id"") DO UPDATE SET
                                               ""Название""=EXCLUDED.""Название"", ""Цвет""=EXCLUDED.""Цвет""";
                            using (var ins = new NpgsqlCommand(insertRegion, pg))
                            {
                                ins.Parameters.AddWithValue("@id", Guid.Parse(regionId));
                                ins.Parameters.AddWithValue("@name", reader["Название"]?.ToString() ?? "");
                                ins.Parameters.AddWithValue("@color", reader["Цвет"]?.ToString() ?? "#000000");
                                ins.Parameters.AddWithValue("@idEmployee", Guid.Empty);
                                await ins.ExecuteNonQueryAsync();
                            }

                            // Переносим узлы этого участка
                            string selectNodes = "SELECT [id], [Долгота], [Широта], [Номер] FROM Узлы WHERE [Id участка] = ? ORDER BY [Номер]";
                            using (var nCmd = new OleDbCommand(selectNodes, access))
                            {
                                nCmd.Parameters.AddWithValue("@id_участка", regionId);
                                using (var nReader = nCmd.ExecuteReader())
                                {
                                    while (nReader.Read())
                                    {
                                        int number = nReader["Номер"] != DBNull.Value ? Convert.ToInt32(nReader["Номер"]) : 1;
                                        string insertNode = @"INSERT INTO ""Узлы"" (""id"", ""Широта"", ""Долгота"", ""Id участка"", ""Номер"")
                                                     VALUES (@id, @lon, @lat, @region, @number)
                                                     ON CONFLICT (""id"") DO UPDATE SET
                                                         ""Широта""=EXCLUDED.""Широта"", ""Долгота""=EXCLUDED.""Долгота"",
                                                         ""Id участка""=EXCLUDED.""Id участка"", ""Номер""=EXCLUDED.""Номер""";
                                        using (var nIns = new NpgsqlCommand(insertNode, pg))
                                        {
                                            nIns.Parameters.AddWithValue("@id", Guid.Parse(nReader["id"].ToString().Trim('{', '}')));
                                            nIns.Parameters.AddWithValue("@lon", Convert.ToDouble(nReader["Долгота"]));
                                            nIns.Parameters.AddWithValue("@lat", Convert.ToDouble(nReader["Широта"]));
                                            nIns.Parameters.AddWithValue("@region", Guid.Parse(regionId));
                                            nIns.Parameters.AddWithValue("@number", number);
                                            await nIns.ExecuteNonQueryAsync();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Восстанавливаем FK
                using (var cmd = new NpgsqlCommand(@"
    ALTER TABLE ""Метки"" ADD CONSTRAINT ""Метки_Id участка_fkey""
        FOREIGN KEY (""Id участка"") REFERENCES ""Участки""(id) ON DELETE CASCADE;
    ALTER TABLE ""Узлы"" ADD CONSTRAINT ""Узлы_Id участка_fkey""
        FOREIGN KEY (""Id участка"") REFERENCES ""Участки""(id) ON DELETE CASCADE;", pg))
                    await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task TransferFromServerToAccess(string targetConnectionString)
        {
            string pgConn = Program.BuildPgConnectionString();

            using (var pg = new NpgsqlConnection(pgConn))
            {
                await pg.OpenAsync();

                using (var access = new OleDbConnection(targetConnectionString))
                {
                    access.Open();

                    // Очищаем таблицы перед вставкой (в правильном порядке зависимостей)
                    foreach (string tbl in new[] { "Метки", "Узлы", "Участки" })
                    {
                        using (var cmd = new OleDbCommand($"DELETE FROM {tbl}", access))
                            cmd.ExecuteNonQuery();
                    }

                    // Загружаем и вставляем участки
                    using (var pgCmd = new NpgsqlCommand("SELECT \"id\", \"Название\", \"Цвет\" FROM \"Участки\"", pg))
                    using (var pgReader = await pgCmd.ExecuteReaderAsync())
                    {
                        while (await pgReader.ReadAsync())
                        {
                            string insertRegion = "INSERT INTO Участки ([id], [Название], [Цвет]) VALUES (?, ?, ?)";
                            using (var cmd = new OleDbCommand(insertRegion, access))
                            {
                                cmd.Parameters.AddWithValue("@id", pgReader["id"].ToString());
                                cmd.Parameters.AddWithValue("@Название", pgReader["Название"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@Цвет", pgReader["Цвет"]?.ToString() ?? "#000000");
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // Загружаем и вставляем узлы
                    using (var pgCmd = new NpgsqlCommand("SELECT \"id\", \"Широта\", \"Долгота\", \"Id участка\", \"Номер\" FROM \"Узлы\" ORDER BY \"Id участка\", \"Номер\"", pg))
                    using (var pgReader = await pgCmd.ExecuteReaderAsync())
                    {
                        while (await pgReader.ReadAsync())
                        {
                            string insertNode = "INSERT INTO Узлы ([id], [Долгота], [Широта], [Id участка], [Номер]) VALUES (?, ?, ?, ?, ?)";
                            using (var cmd = new OleDbCommand(insertNode, access))
                            {
                                cmd.Parameters.AddWithValue("@id", pgReader["id"].ToString());
                                cmd.Parameters.AddWithValue("@Долгота", Convert.ToDouble(pgReader["Широта"]));
                                cmd.Parameters.AddWithValue("@Широта", Convert.ToDouble(pgReader["Долгота"]));
                                cmd.Parameters.AddWithValue("@Id участка", pgReader["Id участка"].ToString());
                                cmd.Parameters.AddWithValue("@Номер", Convert.ToInt32(pgReader["Номер"]));
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // Загружаем и вставляем метки
                    using (var pgCmd = new NpgsqlCommand("SELECT \"id\", \"Широта\", \"Долгота\", \"Тип здания\", \"Улица\", \"Дом\", \"Корпус\", \"Квартира\", \"Id участка\", \"Id читателей\" FROM \"Метки\"", pg))
                    using (var pgReader = await pgCmd.ExecuteReaderAsync())
                    {
                        while (await pgReader.ReadAsync())
                        {
                            string insertMarker = "INSERT INTO Метки ([id], [Долгота], [Широта], [Тип здания], [Улица], [Дом], [Корпус], [Квартира], [Id участка], [Id читателей]) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                            using (var cmd = new OleDbCommand(insertMarker, access))
                            {
                                cmd.Parameters.AddWithValue("@id", pgReader["id"].ToString());
                                cmd.Parameters.AddWithValue("@Долгота", Convert.ToDouble(pgReader["Долгота"]));
                                cmd.Parameters.AddWithValue("@Широта", Convert.ToDouble(pgReader["Широта"]));
                                cmd.Parameters.AddWithValue("@Тип здания", pgReader["Тип здания"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@Улица", pgReader["Улица"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@Дом", pgReader["Дом"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@Корпус", pgReader["Корпус"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@Квартира", pgReader["Квартира"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@Id участка", pgReader["Id участка"] == DBNull.Value ? (object)DBNull.Value : pgReader["Id участка"].ToString());
                                cmd.Parameters.AddWithValue("@Id читателей", pgReader["Id читателей"]?.ToString() ?? "");
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        // ─── Встроенный Python-скрипт SearchAddresses.py ──────────────────────────
        // Скрипт закодирован в Base64, чтобы исключить проблемы с экранированием.
        // Ожидаемый SHA-256 (от исходных байт файла, включая BOM):
        //   10250c2467809c54ffa77da242aaadec18d8903d9eeb23d9da453ad225364dc6
        private static readonly string EmbeddedScriptBase64 =
            "77u/aW1wb3J0IHJlcXVlc3RzDQppbXBvcnQgY3N2DQppbXBvcnQgdGltZQ0KaW1wb3J0IHJlDQpp" +
            "bXBvcnQgb3MNCg0KZGVmIGdldF9vc21fZGF0YShwbGFjZV9uYW1lKToNCiAgICAiIiINCiAgICDQ" +
            "pNGD0L3QutGG0LjRjyDQtNC10LvQsNC10YIg0LfQsNC/0YDQvtGBINC6IEFQSSBPdmVycGFzcyAo" +
            "T3BlblN0cmVldE1hcCkNCiAgICAiIiINCiAgICBwcmludChmIlNlYXJjaGluZyBmb3I6IHtwbGFj" +
            "ZV9uYW1lfS4uLiIpDQogICAgDQogICAgIyDQoNCw0YHRiNC40YDQtdC90L3Ri9C5INC30LDQv9GA" +
            "0L7RgSDQtNC70Y8g0L/QvtC70YPRh9C10L3QuNGPINCx0L7Qu9GM0YjQtSDQuNC90YTQvtGA0LzQ" +
            "sNGG0LjQuA0KICAgIG92ZXJwYXNzX3F1ZXJ5ID0gZiIiIg0KICAgIFtvdXQ6anNvbl1bdGltZW91" +
            "dDo2MF07DQogICAgYXJlYVsibmFtZSI9IntwbGFjZV9uYW1lfSJdLT4uc2VhcmNoQXJlYTsNCiAg" +
            "ICAoDQogICAgICBub2RlWyJhZGRyOmhvdXNlbnVtYmVyIl0oYXJlYS5zZWFyY2hBcmVhKTsNCiAg" +
            "ICAgIHdheVsiYWRkcjpob3VzZW51bWJlciJdKGFyZWEuc2VhcmNoQXJlYSk7DQogICAgICByZWxh" +
            "dGlvblsiYWRkcjpob3VzZW51bWJlciJdKGFyZWEuc2VhcmNoQXJlYSk7DQogICAgICBub2RlWyJi" +
            "dWlsZGluZyJdKGFyZWEuc2VhcmNoQXJlYSk7DQogICAgICB3YXlbImJ1aWxkaW5nIl0oYXJlYS5z" +
            "ZWFyY2hBcmVhKTsNCiAgICAgIHJlbGF0aW9uWyJidWlsZGluZyJdKGFyZWEuc2VhcmNoQXJlYSk7" +
            "DQogICAgKTsNCiAgICBvdXQgY2VudGVyOw0KICAgID47DQogICAgb3V0IHRhZ3M7DQogICAgIiIi" +
            "DQoNCiAgICB0cnk6DQogICAgICAgIHJlc3BvbnNlID0gcmVxdWVzdHMuZ2V0KA0KICAgICAgICAg" +
            "ICAgImh0dHA6Ly9vdmVycGFzcy1hcGkuZGUvYXBpL2ludGVycHJldGVyIiwgDQogICAgICAgICAg" +
            "ICBwYXJhbXM9eydkYXRhJzogb3ZlcnBhc3NfcXVlcnl9LA0KICAgICAgICAgICAgaGVhZGVycz17" +
            "J1VzZXItQWdlbnQnOiAnUHl0aG9uU2NyaXB0LzEuMCd9DQogICAgICAgICkNCiAgICAgICAgDQog" +
            "ICAgICAgIGlmIHJlc3BvbnNlLnN0YXR1c19jb2RlICE9IDIwMDoNCiAgICAgICAgICAgIHByaW50" +
            "KGYiRXJyb3I6IFNlcnZlciByZXR1cm5lZCBzdGF0dXMge3Jlc3BvbnNlLnN0YXR1c19jb2RlfSIp" +
            "DQogICAgICAgICAgICByZXR1cm4gW10NCiAgICAgICAgICAgIA0KICAgICAgICBkYXRhID0gcmVz" +
            "cG9uc2UuanNvbigpDQogICAgICAgIHJldHVybiBkYXRhLmdldCgnZWxlbWVudHMnLCBbXSkNCiAg" +
            "ICAgICAgDQogICAgZXhjZXB0IEV4Y2VwdGlvbiBhcyBlOg0KICAgICAgICBwcmludChmIkVycm9y" +
            "IGR1cmluZyByZXF1ZXN0OiB7ZX0iKQ0KICAgICAgICByZXR1cm4gW10NCg0KZGVmIHBhcnNlX2hv" +
            "dXNlX251bWJlcihob3VzZV9udW1iZXJfc3RyKToNCiAgICAiIiINCiAgICDQoNCw0LfQsdC40YDQ" +
            "sNC10YIg0L3QvtC80LXRgCDQtNC+0LzQsCDQvdCwINGB0L7RgdGC0LDQstC70Y/RjtGJ0LjQtTog" +
            "0L3QvtC80LXRgCwg0LrQvtGA0L/Rg9GBLCDRgdGC0YDQvtC10L3QuNC1DQogICAgIiIiDQogICAg" +
            "aG91c2UgPSBob3VzZV9udW1iZXJfc3RyDQogICAga29ycHVzID0gIiINCiAgICBrdmFydGlyYSA9" +
            "ICIiDQogICAgDQogICAgaWYgaG91c2VfbnVtYmVyX3N0cjoNCiAgICAgICAgIyDQo9C00LDQu9GP" +
            "0LXQvCDQu9C40YjQvdC40LUg0L/RgNC+0LHQtdC70YsNCiAgICAgICAgaG91c2VfbnVtYmVyX3N0" +
            "ciA9IHN0cihob3VzZV9udW1iZXJfc3RyKS5zdHJpcCgpDQogICAgICAgIA0KICAgICAgICAjINCf" +
            "0LDRgtGC0LXRgNC90Ysg0LTQu9GPINC/0L7QuNGB0LrQsCDQutC+0YDQv9GD0YHQsCAo0LrQvtGA" +
            "0L8uLCDQuiwg0LouLCDQutC+0YDQv9GD0YEpDQogICAgICAgIGtvcnB1c19wYXR0ZXJucyA9IFsN" +
            "CiAgICAgICAgICAgIHIn0Lpb0L7Qvl3RgNC/KD860YPRgSk/XC4/XHMqKFxkK1thLXpBLVrQsC3R" +
            "j9CQLdCvXSopJywNCiAgICAgICAgICAgIHIn0LpcLj9ccyooXGQrW2EtekEtWtCwLdGP0JAt0K9d" +
            "KiknLA0KICAgICAgICAgICAgcifQutC+0YDQvyg/OtGD0YEpP1wuP1xzKihcZCtbYS16QS1a0LAt" +
            "0Y/QkC3Qr10qKScsDQogICAgICAgICAgICByJy9ccyooXGQrW2EtekEtWtCwLdGP0JAt0K9dKikn" +
            "ICAjINGE0L7RgNC80LDRgiAxMjMvMQ0KICAgICAgICBdDQogICAgICAgIA0KICAgICAgICAjINCf" +
            "0LDRgtGC0LXRgNC90Ysg0LTQu9GPINC/0L7QuNGB0LrQsCDQutCy0LDRgNGC0LjRgNGLDQogICAg" +
            "ICAgIGt2YXJ0aXJhX3BhdHRlcm5zID0gWw0KICAgICAgICAgICAgcifQutCyKD860LDRgNGC0LjR" +
            "gNCwKT9cLj9ccyooXGQrW2EtekEtWtCwLdGP0JAt0K9dKiknLA0KICAgICAgICAgICAgcifQutCy" +
            "XC4/XHMqKFxkK1thLXpBLVrQsC3Rj9CQLdCvXSopJywNCiAgICAgICAgICAgIHIn4oSWP1xzKihc" +
            "ZCspXHMq0LrQsicNCiAgICAgICAgXQ0KICAgICAgICANCiAgICAgICAgIyDQodC90LDRh9Cw0LvQ" +
            "sCDQv9GL0YLQsNC10LzRgdGPINCy0YvQtNC10LvQuNGC0Ywg0LrQstCw0YDRgtC40YDRgw0KICAg" +
            "ICAgICBmb3IgcGF0dGVybiBpbiBrdmFydGlyYV9wYXR0ZXJuczoNCiAgICAgICAgICAgIG1hdGNo" +
            "ID0gcmUuc2VhcmNoKHBhdHRlcm4sIGhvdXNlX251bWJlcl9zdHIsIHJlLklHTk9SRUNBU0UpDQog" +
            "ICAgICAgICAgICBpZiBtYXRjaDoNCiAgICAgICAgICAgICAgICBrdmFydGlyYSA9IG1hdGNoLmdy" +
            "b3VwKDEpDQogICAgICAgICAgICAgICAgIyDQo9C00LDQu9GP0LXQvCDQuNC90YTQvtGA0LzQsNGG" +
            "0LjRjiDQviDQutCy0LDRgNGC0LjRgNC1INC40Lcg0YHRgtGA0L7QutC4DQogICAgICAgICAgICAg" +
            "ICAgaG91c2VfbnVtYmVyX3N0ciA9IHJlLnN1YihwYXR0ZXJuLCAnJywgaG91c2VfbnVtYmVyX3N0" +
            "ciwgZmxhZ3M9cmUuSUdOT1JFQ0FTRSkuc3RyaXAoKQ0KICAgICAgICAgICAgICAgIGJyZWFrDQog" +
            "ICAgICAgIA0KICAgICAgICAjINCX0LDRgtC10Lwg0L/Ri9GC0LDQtdC80YHRjyDQstGL0LTQtdC7" +
            "0LjRgtGMINC60L7RgNC/0YPRgQ0KICAgICAgICBmb3IgcGF0dGVybiBpbiBrb3JwdXNfcGF0dGVy" +
            "bnM6DQogICAgICAgICAgICBtYXRjaCA9IHJlLnNlYXJjaChwYXR0ZXJuLCBob3VzZV9udW1iZXJf" +
            "c3RyLCByZS5JR05PUkVDQVNFKQ0KICAgICAgICAgICAgaWYgbWF0Y2g6DQogICAgICAgICAgICAg" +
            "ICAga29ycHVzID0gbWF0Y2guZ3JvdXAoMSkNCiAgICAgICAgICAgICAgICAjINCj0LTQsNC70Y/Q" +
            "tdC8INC40L3RhNC+0YDQvNCw0YbQuNGOINC+INC60L7RgNC/0YPRgdC1INC40Lcg0YHRgtGA0L7Q" +
            "utC4DQogICAgICAgICAgICAgICAgaG91c2VfbnVtYmVyX3N0ciA9IHJlLnN1YihwYXR0ZXJuLCAn" +
            "JywgaG91c2VfbnVtYmVyX3N0ciwgZmxhZ3M9cmUuSUdOT1JFQ0FTRSkuc3RyaXAoKQ0KICAgICAg" +
            "ICAgICAgICAgIGJyZWFrDQogICAgICAgIA0KICAgICAgICAjINCe0YHRgtCw0LXRgtGB0Y8g0L7R" +
            "gdC90L7QstC90L7QuSDQvdC+0LzQtdGAINC00L7QvNCwDQogICAgICAgICMg0KPQtNCw0LvRj9C1" +
            "0Lwg0LLRgdC1INC90LXRhtC40YTRgNC+0LLRi9C1INGB0LjQvNCy0L7Qu9GLINCyINC90LDRh9Cw" +
            "0LvQtS/QutC+0L3RhtC1DQogICAgICAgIGhvdXNlX21hdGNoID0gcmUuc2VhcmNoKHInXihcZCtb" +
            "YS16QS1a0LAt0Y/QkC3Qr10qKScsIGhvdXNlX251bWJlcl9zdHIuc3RyaXAoKSkNCiAgICAgICAg" +
            "aWYgaG91c2VfbWF0Y2g6DQogICAgICAgICAgICBob3VzZSA9IGhvdXNlX21hdGNoLmdyb3VwKDEp" +
            "DQogICAgICAgIGVsc2U6DQogICAgICAgICAgICBob3VzZSA9IGhvdXNlX251bWJlcl9zdHIuc3Ry" +
            "aXAoKQ0KICAgIA0KICAgIHJldHVybiBob3VzZSwga29ycHVzLCBrdmFydGlyYQ0KDQpkZWYgc2F2" +
            "ZV90b19maWxlKGVsZW1lbnRzLCBmaWxlbmFtZT0iYWRkcmVzc2VzLnR4dCIpOg0KICAgIGFkZHJl" +
            "c3Nlc19kYXRhID0gW10NCiAgICANCiAgICBmb3IgZWxlbWVudCBpbiBlbGVtZW50czoNCiAgICAg" +
            "ICAgdGFncyA9IGVsZW1lbnQuZ2V0KCd0YWdzJywge30pDQogICAgICAgIA0KICAgICAgICAjINCf" +
            "0YDQvtC/0YPRgdC60LDQtdC8INGN0LvQtdC80LXQvdGC0Ysg0LHQtdC3INCw0LTRgNC10YHQsCDQ" +
            "uNC70Lgg0LfQtNCw0L3QuNGPDQogICAgICAgIGlmIG5vdCB0YWdzLmdldCgnYWRkcjpob3VzZW51" +
            "bWJlcicpIGFuZCBub3QgdGFncy5nZXQoJ2J1aWxkaW5nJyk6DQogICAgICAgICAgICBjb250aW51" +
            "ZQ0KICAgICAgICANCiAgICAgICAgIyDQn9C+0LvRg9GH0LDQtdC8INC60L7QvtGA0LTQuNC90LDR" +
            "gtGLDQogICAgICAgIGxhdCA9IGVsZW1lbnQuZ2V0KCdsYXQnKQ0KICAgICAgICBsb24gPSBlbGVt" +
            "ZW50LmdldCgnbG9uJykNCiAgICAgICAgDQogICAgICAgICMg0JXRgdC70Lgg0Y3RgtC+INC60L7Q" +
            "vdGC0YPRgCDQt9C00LDQvdC40Y8gKHdheSksINCx0LXRgNC10Lwg0LXQs9C+INGG0LXQvdGC0YAN" +
            "CiAgICAgICAgaWYgbGF0IGlzIE5vbmUgYW5kICdjZW50ZXInIGluIGVsZW1lbnQ6DQogICAgICAg" +
            "ICAgICBsYXQgPSBlbGVtZW50WydjZW50ZXInXVsnbGF0J10NCiAgICAgICAgICAgIGxvbiA9IGVs" +
            "ZW1lbnRbJ2NlbnRlciddWydsb24nXQ0KICAgICAgICANCiAgICAgICAgaWYgbGF0IGlzIE5vbmUg" +
            "b3IgbG9uIGlzIE5vbmU6DQogICAgICAgICAgICBjb250aW51ZQ0KICAgICAgICANCiAgICAgICAg" +
            "IyDQn9C+0LvRg9GH0LDQtdC8INGC0LjQvyDQt9C00LDQvdC40Y8NCiAgICAgICAgYnVpbGRpbmdf" +
            "dHlwZSA9IHRhZ3MuZ2V0KCdidWlsZGluZycsICfQvdC1INGD0LrQsNC30LDQvScpDQogICAgICAg" +
            "IA0KICAgICAgICAjINCf0L7Qu9GD0YfQsNC10Lwg0YPQu9C40YbRgw0KICAgICAgICBzdHJlZXQg" +
            "PSB0YWdzLmdldCgnYWRkcjpzdHJlZXQnLCAnJykNCiAgICAgICAgaWYgbm90IHN0cmVldDoNCiAg" +
            "ICAgICAgICAgIHN0cmVldCA9IHRhZ3MuZ2V0KCdhZGRyOnBsYWNlJywgJycpDQogICAgICAgIGlm" +
            "IG5vdCBzdHJlZXQ6DQogICAgICAgICAgICBzdHJlZXQgPSB0YWdzLmdldCgnYWRkcjpzdHJlZXRu" +
            "YW1lJywgJycpDQogICAgICAgIA0KICAgICAgICAjINCf0L7Qu9GD0YfQsNC10Lwg0L3QvtC80LXR" +
            "gCDQtNC+0LzQsCDQuCDQv9Cw0YDRgdC40Lwg0LXQs9C+DQogICAgICAgIGhvdXNlX251bWJlcl9z" +
            "dHIgPSB0YWdzLmdldCgnYWRkcjpob3VzZW51bWJlcicsICcnKQ0KICAgICAgICBob3VzZSwga29y" +
            "cHVzLCBrdmFydGlyYSA9IHBhcnNlX2hvdXNlX251bWJlcihob3VzZV9udW1iZXJfc3RyKQ0KICAg" +
            "ICAgICANCiAgICAgICAgIyDQldGB0LvQuCDQutCy0LDRgNGC0LjRgNCwINC90LUg0L3QsNC50LTQ" +
            "tdC90LAg0LIg0L3QvtC80LXRgNC1INC00L7QvNCwLCDQuNGJ0LXQvCDQsiDQvtGC0LTQtdC70YzQ" +
            "vdC+0Lwg0YLQtdCz0LUNCiAgICAgICAgaWYgbm90IGt2YXJ0aXJhIGFuZCAnYWRkcjpmbGF0JyBp" +
            "biB0YWdzOg0KICAgICAgICAgICAga3ZhcnRpcmEgPSB0YWdzLmdldCgnYWRkcjpmbGF0JywgJycp" +
            "DQogICAgICAgIA0KICAgICAgICAjINCV0YHQu9C4INC60L7RgNC/0YPRgSDQvdC1INC90LDQudC0" +
            "0LXQvSDQsiDQvdC+0LzQtdGA0LUg0LTQvtC80LAsINC40YnQtdC8INCyINC+0YLQtNC10LvRjNC9" +
            "0L7QvCDRgtC10LPQtQ0KICAgICAgICBpZiBub3Qga29ycHVzOg0KICAgICAgICAgICAgIyDQn9GA" +
            "0L7QstC10YDRj9C10Lwg0YDQsNC30LvQuNGH0L3Ri9C1INCy0LDRgNC40LDQvdGC0Ysg0YLQtdCz" +
            "0L7QsiDQtNC70Y8g0LrQvtGA0L/Rg9GB0LANCiAgICAgICAgICAgIGZvciB0YWdfbmFtZSBpbiBb" +
            "J2FkZHI6dW5pdCcsICdhZGRyOmZsYXRzJywgJ2FkZHI6YmxvY2snLCAnYWRkcjpjb3JwdXMnXToN" +
            "CiAgICAgICAgICAgICAgICBpZiB0YWdfbmFtZSBpbiB0YWdzOg0KICAgICAgICAgICAgICAgICAg" +
            "ICBrb3JwdXMgPSB0YWdzW3RhZ19uYW1lXQ0KICAgICAgICAgICAgICAgICAgICBicmVhaw0KICAg" +
            "ICAgICANCiAgICAgICAgIyDQpNC+0YDQvNC40YDRg9C10Lwg0LfQsNC/0LjRgdGMDQogICAgICAg" +
            "IHJlY29yZCA9IHsNCiAgICAgICAgICAgICdsb24nOiBsb24sDQogICAgICAgICAgICAnbGF0Jzog" +
            "bGF0LA0KICAgICAgICAgICAgJ2J1aWxkaW5nX3R5cGUnOiBidWlsZGluZ190eXBlLA0KICAgICAg" +
            "ICAgICAgJ3N0cmVldCc6IHN0cmVldC5zdHJpcCgpIGlmIHN0cmVldCBlbHNlICfQvdC1INGD0LrQ" +
            "sNC30LDQvdCwJywNCiAgICAgICAgICAgICdob3VzZSc6IGhvdXNlIGlmIGhvdXNlIGVsc2UgJ9C9" +
            "0LUg0YPQutCw0LfQsNC9JywNCiAgICAgICAgICAgICdrb3JwdXMnOiBrb3JwdXMgaWYga29ycHVz" +
            "IGVsc2UgJycsDQogICAgICAgICAgICAna3ZhcnRpcmEnOiBrdmFydGlyYSBpZiBrdmFydGlyYSBl" +
            "bHNlICcnDQogICAgICAgIH0NCiAgICAgICAgDQogICAgICAgIGFkZHJlc3Nlc19kYXRhLmFwcGVu" +
            "ZChyZWNvcmQpDQogICAgDQogICAgIyDQl9Cw0L/QuNGB0YvQstCw0LXQvCDQsiDRhNCw0LnQuw0K" +
            "ICAgIHdpdGggb3BlbihmaWxlbmFtZSwgJ3cnLCBlbmNvZGluZz0ndXRmLTgnLCBuZXdsaW5lPScn" +
            "KSBhcyBmOg0KICAgICAgICAjINCV0YHQu9C4INGF0L7RgtC40LwgQ1NWINGBINGA0LDQt9C00LXQ" +
            "u9C40YLQtdC70LXQvCDQt9Cw0L/Rj9GC0LDRjw0KICAgICAgICB3cml0ZXIgPSBjc3Yud3JpdGVy" +
            "KGYsIGRlbGltaXRlcj0nLCcsIHF1b3RlY2hhcj0nIicsIHF1b3Rpbmc9Y3N2LlFVT1RFX01JTklN" +
            "QUwpDQogICAgICAgIA0KICAgICAgICAjINCX0LDQs9C+0LvQvtCy0LrQuA0KICAgICAgICB3cml0" +
            "ZXIud3JpdGVyb3coWyfQlNC+0LvQs9C+0YLQsCcsICfQqNC40YDQvtGC0LAnLCAn0KLQuNC/INC3" +
            "0LTQsNC90LjRjycsICfQo9C70LjRhtCwJywgJ9CU0L7QvCcsICfQmtC+0YDQv9GD0YEnLCAn0JrQ" +
            "stCw0YDRgtC40YDQsCddKQ0KICAgICAgICANCiAgICAgICAgIyDQlNCw0L3QvdGL0LUNCiAgICAg" +
            "ICAgZm9yIHJlY29yZCBpbiBhZGRyZXNzZXNfZGF0YToNCiAgICAgICAgICAgIHdyaXRlci53cml0" +
            "ZXJvdyhbDQogICAgICAgICAgICAgICAgcmVjb3JkWydsb24nXSwNCiAgICAgICAgICAgICAgICBy" +
            "ZWNvcmRbJ2xhdCddLA0KICAgICAgICAgICAgICAgIHJlY29yZFsnYnVpbGRpbmdfdHlwZSddLA0K" +
            "ICAgICAgICAgICAgICAgIHJlY29yZFsnc3RyZWV0J10sDQogICAgICAgICAgICAgICAgcmVjb3Jk" +
            "Wydob3VzZSddLA0KICAgICAgICAgICAgICAgIHJlY29yZFsna29ycHVzJ10sDQogICAgICAgICAg" +
            "ICAgICAgcmVjb3JkWydrdmFydGlyYSddDQogICAgICAgICAgICBdKQ0KICAgIA0KICAgIHJldHVy" +
            "biBsZW4oYWRkcmVzc2VzX2RhdGEpDQoNCmRlZiBzYXZlX3RvX3R4dChlbGVtZW50cywgZmlsZW5h" +
            "bWU9ImFkZHJlc3Nlcy50eHQiKToNCiAgICAiIiINCiAgICDQkNC70YzRgtC10YDQvdCw0YLQuNCy" +
            "0L3QsNGPINGE0YPQvdC60YbQuNGPINC00LvRjyDRgdC+0YXRgNCw0L3QtdC90LjRjyDQsiDQv9GA" +
            "0L7RgdGC0L7QuSDRgtC10LrRgdGC0L7QstGL0Lkg0YTQsNC50LsNCiAgICAiIiINCiAgICBjb3Vu" +
            "dCA9IDANCiAgICANCiAgICB3aXRoIG9wZW4oZmlsZW5hbWUsICd3JywgZW5jb2Rpbmc9J3V0Zi04" +
            "JykgYXMgZjoNCiAgICAgICAgIyDQl9Cw0LPQvtC70L7QstC+0LoNCiAgICAgICAgZi53cml0ZSgi" +
            "0JTQvtC70LPQvtGC0LAs0KjQuNGA0L7RgtCwLNCi0LjQvyDQt9C00LDQvdC40Y8s0KPQu9C40YbQ" +
            "sCzQlNC+0Lws0JrQvtGA0L/Rg9GBLNCa0LLQsNGA0YLQuNGA0LBcbiIpDQogICAgICAgIA0KICAg" +
            "ICAgICBmb3IgZWxlbWVudCBpbiBlbGVtZW50czoNCiAgICAgICAgICAgIHRhZ3MgPSBlbGVtZW50" +
            "LmdldCgndGFncycsIHt9KQ0KICAgICAgICAgICAgDQogICAgICAgICAgICAjINCf0YDQvtC/0YPR" +
            "gdC60LDQtdC8INGN0LvQtdC80LXQvdGC0Ysg0LHQtdC3INCw0LTRgNC10YHQsCDQuNC70Lgg0LfQ" +
            "tNCw0L3QuNGPDQogICAgICAgICAgICBpZiBub3QgdGFncy5nZXQoJ2FkZHI6aG91c2VudW1iZXIn" +
            "KSBhbmQgbm90IHRhZ3MuZ2V0KCdidWlsZGluZycpOg0KICAgICAgICAgICAgICAgIGNvbnRpbnVl" +
            "DQogICAgICAgICAgICANCiAgICAgICAgICAgICMg0J/QvtC70YPRh9Cw0LXQvCDQutC+0L7RgNC0" +
            "0LjQvdCw0YLRiw0KICAgICAgICAgICAgbGF0ID0gZWxlbWVudC5nZXQoJ2xhdCcpDQogICAgICAg" +
            "ICAgICBsb24gPSBlbGVtZW50LmdldCgnbG9uJykNCiAgICAgICAgICAgIA0KICAgICAgICAgICAg" +
            "aWYgbGF0IGlzIE5vbmUgYW5kICdjZW50ZXInIGluIGVsZW1lbnQ6DQogICAgICAgICAgICAgICAg" +
            "bGF0ID0gZWxlbWVudFsnY2VudGVyJ11bJ2xhdCddDQogICAgICAgICAgICAgICAgbG9uID0gZWxl" +
            "bWVudFsnY2VudGVyJ11bJ2xvbiddDQogICAgICAgICAgICANCiAgICAgICAgICAgIGlmIGxhdCBp" +
            "cyBOb25lIG9yIGxvbiBpcyBOb25lOg0KICAgICAgICAgICAgICAgIGNvbnRpbnVlDQogICAgICAg" +
            "ICAgICANCiAgICAgICAgICAgICMg0J/QvtC70YPRh9Cw0LXQvCDQtNCw0L3QvdGL0LUNCiAgICAg" +
            "ICAgICAgIGJ1aWxkaW5nX3R5cGUgPSB0YWdzLmdldCgnYnVpbGRpbmcnLCAn0L3QtSDRg9C60LDQ" +
            "t9Cw0L0nKQ0KICAgICAgICAgICAgDQogICAgICAgICAgICBzdHJlZXQgPSB0YWdzLmdldCgnYWRk" +
            "cjpzdHJlZXQnLCAnJykNCiAgICAgICAgICAgIGlmIG5vdCBzdHJlZXQ6DQogICAgICAgICAgICAg" +
            "ICAgc3RyZWV0ID0gdGFncy5nZXQoJ2FkZHI6cGxhY2UnLCAnJykNCiAgICAgICAgICAgIGlmIG5v" +
            "dCBzdHJlZXQ6DQogICAgICAgICAgICAgICAgc3RyZWV0ID0gdGFncy5nZXQoJ2FkZHI6c3RyZWV0" +
            "bmFtZScsICcnKQ0KICAgICAgICAgICAgDQogICAgICAgICAgICBob3VzZV9udW1iZXJfc3RyID0g" +
            "dGFncy5nZXQoJ2FkZHI6aG91c2VudW1iZXInLCAnJykNCiAgICAgICAgICAgIGhvdXNlLCBrb3Jw" +
            "dXMsIGt2YXJ0aXJhID0gcGFyc2VfaG91c2VfbnVtYmVyKGhvdXNlX251bWJlcl9zdHIpDQogICAg" +
            "ICAgICAgICANCiAgICAgICAgICAgICMg0JTQvtC/0L7Qu9C90LjRgtC10LvRjNC90YvQtSDRgtC1" +
            "0LPQuA0KICAgICAgICAgICAgaWYgbm90IGt2YXJ0aXJhIGFuZCAnYWRkcjpmbGF0JyBpbiB0YWdz" +
            "Og0KICAgICAgICAgICAgICAgIGt2YXJ0aXJhID0gdGFncy5nZXQoJ2FkZHI6ZmxhdCcsICcnKQ0K" +
            "ICAgICAgICAgICAgDQogICAgICAgICAgICBpZiBub3Qga29ycHVzOg0KICAgICAgICAgICAgICAg" +
            "IGZvciB0YWdfbmFtZSBpbiBbJ2FkZHI6dW5pdCcsICdhZGRyOmZsYXRzJywgJ2FkZHI6YmxvY2sn" +
            "LCAnYWRkcjpjb3JwdXMnXToNCiAgICAgICAgICAgICAgICAgICAgaWYgdGFnX25hbWUgaW4gdGFn" +
            "czoNCiAgICAgICAgICAgICAgICAgICAgICAgIGtvcnB1cyA9IHRhZ3NbdGFnX25hbWVdDQogICAg" +
            "ICAgICAgICAgICAgICAgICAgICBicmVhaw0KICAgICAgICAgICAgDQogICAgICAgICAgICAjINCX" +
            "0LDQv9C40YHRi9Cy0LDQtdC8INGB0YLRgNC+0LrRgyAo0YTQvtGA0LzQsNGCIENTViDQtNC70Y8g" +
            "0YPQtNC+0LHRgdGC0LLQsCkNCiAgICAgICAgICAgIGxpbmUgPSBmJ3tsb259LHtsYXR9LCJ7YnVp" +
            "bGRpbmdfdHlwZX0iLCJ7c3RyZWV0fSIsIntob3VzZX0iLCJ7a29ycHVzfSIsIntrdmFydGlyYX0i" +
            "XG4nDQogICAgICAgICAgICBmLndyaXRlKGxpbmUpDQogICAgICAgICAgICBjb3VudCArPSAxDQog" +
            "ICAgDQogICAgcmV0dXJuIGNvdW50DQoNCiMgLS0tINCT0JvQkNCS0J3QkNCvINCn0JDQodCi0Kwg" +
            "LS0tDQppZiBfX25hbWVfXyA9PSAiX19tYWluX18iOg0KICAgIHByaW50KCI9IiAqIDUwKQ0KICAg" +
            "IHByaW50KCLQn9Cw0YDRgdC10YAg0LDQtNGA0LXRgdC+0LIg0LjQtyBPcGVuU3RyZWV0TWFwIikN" +
            "CiAgICBwcmludCgiPSIgKiA1MCkNCiAgICANCiAgICAjIDEuINCh0L/RgNCw0YjQuNCy0LDQtdC8" +
            "INGDINC/0L7Qu9GM0LfQvtCy0LDRgtC10LvRjyDQvdCw0LfQstCw0L3QuNC1DQogICAgY2l0eSA9" +
            "IGlucHV0KCLQktCy0LXQtNC40YLQtSDQvdCw0LfQstCw0L3QuNC1INGB0LXQu9CwINC40LvQuCDQ" +
            "s9C+0YDQvtC00LAgKNC60LDQuiDQvdCwINC60LDRgNGC0LDRhSk6ICIpLnN0cmlwKCkNCiAgICAN" +
            "CiAgICBpZiBub3QgY2l0eToNCiAgICAgICAgcHJpbnQoItCe0YjQuNCx0LrQsDog0L3QsNC30LLQ" +
            "sNC90LjQtSDQvdC1INC80L7QttC10YIg0LHRi9GC0Ywg0L/Rg9GB0YLRi9C8ISIpDQogICAgICAg" +
            "IGV4aXQoKQ0KICAgIA0KICAgICMgMi4g0J7Qv9GA0LXQtNC10LvRj9C10Lwg0L/Rg9GC0Ywg0Log" +
            "0L/QsNC/0LrQtSBkYXRhDQogICAgc2NyaXB0X2RpciA9IG9zLnBhdGguZGlybmFtZShvcy5wYXRo" +
            "LmFic3BhdGgoX19maWxlX18pKQ0KICAgIGRhdGFfZGlyID0gb3MucGF0aC5qb2luKHNjcmlwdF9k" +
            "aXIsICJkYXRhIikNCiAgICANCiAgICAjINCh0L7Qt9C00LDQtdC8INC/0LDQv9C60YMgZGF0YSDQ" +
            "tdGB0LvQuCDQtdGRINC90LXRgg0KICAgIGlmIG5vdCBvcy5wYXRoLmV4aXN0cyhkYXRhX2Rpcik6" +
            "DQogICAgICAgIG9zLm1ha2VkaXJzKGRhdGFfZGlyKQ0KICAgICAgICBwcmludChmItCh0L7Qt9C0" +
            "0LDQvdCwINC/0LDQv9C60LA6IHtkYXRhX2Rpcn0iKQ0KICAgIA0KICAgICMg0J/Rg9GC0Ywg0Log" +
            "0YTQsNC50LvRgyBhZGRyZXNzZXMudHh0INCyINC/0LDQv9C60LUgZGF0YQ0KICAgIGFkZHJlc3Nl" +
            "c19maWxlID0gb3MucGF0aC5qb2luKGRhdGFfZGlyLCAiYWRkcmVzc2VzLnR4dCIpDQogICAgDQog" +
            "ICAgIyDQo9C00LDQu9GP0LXQvCDRgdGC0LDRgNGL0Lkg0YTQsNC50Lsg0LXRgdC70Lgg0L7QvSDR" +
            "gdGD0YnQtdGB0YLQstGD0LXRgg0KICAgIGlmIG9zLnBhdGguZXhpc3RzKGFkZHJlc3Nlc19maWxl" +
            "KToNCiAgICAgICAgb3MucmVtb3ZlKGFkZHJlc3Nlc19maWxlKQ0KICAgICAgICBwcmludChmItCj" +
            "0LTQsNC70LXQvSDRgdGC0LDRgNGL0Lkg0YTQsNC50Ls6IHthZGRyZXNzZXNfZmlsZX0iKQ0KICAg" +
            "IA0KICAgICMgMy4g0KHQutCw0YfQuNCy0LDQtdC8INC00LDQvdC90YvQtQ0KICAgIHByaW50KGYi" +
            "XG7Ql9Cw0L/RgNCw0YjQuNCy0LDRjiDQtNCw0L3QvdGL0LUg0LTQu9GPOiB7Y2l0eX0iKQ0KICAg" +
            "IHByaW50KCLQrdGC0L4g0LzQvtC20LXRgiDQt9Cw0L3Rj9GC0Ywg0L3QtdC60L7RgtC+0YDQvtC1" +
            "INCy0YDQtdC80Y8uLi4iKQ0KICAgIA0KICAgIGVsZW1lbnRzID0gZ2V0X29zbV9kYXRhKGNpdHkp" +
            "DQogICAgDQogICAgaWYgZWxlbWVudHM6DQogICAgICAgICMgNC4g0KHQvtGF0YDQsNC90Y/QtdC8" +
            "INCyIFRYVCDQsiDQv9Cw0L/QutGDIGRhdGENCiAgICAgICAgdG90YWwgPSBzYXZlX3RvX3R4dChl" +
            "bGVtZW50cywgYWRkcmVzc2VzX2ZpbGUpDQogICAgICAgIA0KICAgICAgICBwcmludCgiLSIgKiA1" +
            "MCkNCiAgICAgICAgcHJpbnQoZiLQk9C+0YLQvtCy0L4hINCd0LDQudC00LXQvdC+INCw0LTRgNC1" +
            "0YHQvtCyOiB7dG90YWx9IikNCiAgICAgICAgcHJpbnQoZiLQlNCw0L3QvdGL0LUg0YHQvtGF0YDQ" +
            "sNC90LXQvdGLINCyINGE0LDQudC7OiIpDQogICAgICAgIHByaW50KGYiICAtIHthZGRyZXNzZXNf" +
            "ZmlsZX0iKQ0KICAgICAgICBwcmludCgiXG7QpNCw0LnQuyDQs9C+0YLQvtCyINC00LvRjyDQuNGB" +
            "0L/QvtC70YzQt9C+0LLQsNC90LjRjyDQsiDQv9GA0LjQu9C+0LbQtdC90LjQuCEiKQ0KICAgICAg" +
            "ICANCiAgICBlbHNlOg0KICAgICAgICBwcmludCgi0J3QuNGH0LXQs9C+INC90LUg0L3QsNC50LTQ" +
            "tdC90L4uINCf0L7Qv9GA0L7QsdGD0LnRgtC1INGD0YLQvtGH0L3QuNGC0Ywg0L3QsNC30LLQsNC9" +
            "0LjQtS4iKQ0KICAgICAgICBwcmludCgi0J/RgNC40LzQtdGA0Ys6IikNCiAgICAgICAgcHJpbnQo" +
            "IiAgLSAn0YHQtdC70L4g0JjQstCw0L3QvtCy0LrQsCciKQ0KICAgICAgICBwcmludCgiICAtICfQ" +
            "s9C+0YDQvtC0INCc0L7RgdC60LLQsCciKQ0KICAgICAgICBwcmludCgiICAtICfQv9C+0YHRkdC7" +
            "0L7QuiDQm9C10YHQvdC+0LknIikNCiAgICANCiAgICBpbnB1dCgiXG7QndCw0LbQvNC40YLQtSBF" +
            "bnRlciDQtNC70Y8g0LLRi9GF0L7QtNCwLi4uIik=";

        private static readonly string EmbeddedScriptHash =
            "10250c2467809c54ffa77da242aaadec18d8903d9eeb23d9da453ad225364dc6";

        /// <summary>
        /// Возвращает SHA-256 хэш файла в нижнем регистре, или null при ошибке.
        /// </summary>
        private static string ComputeFileSha256(string path)
        {
            try
            {
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(path))
                {
                    byte[] hash = sha.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Проверяет наличие Python в PATH.
        /// Возвращает имя команды ("python" или "python3"), либо null если не найден.
        /// </summary>
        private static string DetectPython()
        {
            foreach (string cmd in new[] { "python", "python3" })
            {
                try
                {
                    using (var p = new Process())
                    {
                        p.StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {cmd} --version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        p.Start();
                        p.WaitForExit();
                        if (p.ExitCode == 0) return cmd;
                    }
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// Проверяет, установлена ли библиотека pip-пакета.
        /// </summary>
        private static bool IsPipPackageInstalled(string pythonCmd, string package)
        {
            try
            {
                using (var p = new Process())
                {
                    p.StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {pythonCmd} -c \"import {package}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    p.Start();
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            try
            {
                // ── 1. Проверяем/восстанавливаем скрипт ─────────────────────────────
                bool needsRestore = false;

                if (!File.Exists(scriptPath))
                {
                    needsRestore = true;
                }
                else
                {
                    string actualHash = ComputeFileSha256(scriptPath);
                    if (actualHash == null || !actualHash.Equals(EmbeddedScriptHash, StringComparison.OrdinalIgnoreCase))
                        needsRestore = true;
                }

                if (needsRestore)
                {
                    // Записываем встроенный скрипт на диск
                    byte[] scriptBytes = Convert.FromBase64String(EmbeddedScriptBase64);
                    File.WriteAllBytes(scriptPath, scriptBytes);
                    UpdateStatusLabel("Файл SearchAddresses.py был восстановлен из встроенной копии.");
                }

                // ── 2. Проверяем наличие Python ──────────────────────────────────────
                string pythonCmd = DetectPython();

                if (pythonCmd == null)
                {
                    DialogResult dr = MessageBox.Show(
                        "Python не найден в системе!\n\n" +
                        "Для работы скрипта необходим Python 3.7 или новее.\n" +
                        "Нажмите «Да», чтобы открыть официальный сайт для скачивания.\n\n" +
                        "При установке обязательно отметьте пункт\n«Add Python to PATH».",
                        "Python не установлен",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (dr == DialogResult.Yes)
                        Process.Start("https://www.python.org/downloads/");

                    return;
                }

                // ── 3. Проверяем/устанавливаем зависимости (только requests) ────────
                if (!IsPipPackageInstalled(pythonCmd, "requests"))
                {
                    UpdateStatusLabel("Устанавливается библиотека requests...");

                    using (var installProc = new Process())
                    {
                        installProc.StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {pythonCmd} -m pip install requests",
                            UseShellExecute = true,
                            CreateNoWindow = false
                        };
                        installProc.Start();
                        installProc.WaitForExit();

                        if (installProc.ExitCode != 0)
                        {
                            MessageBox.Show(
                                "Не удалось автоматически установить библиотеку requests.\n\n" +
                                "Установите её вручную:\n" +
                                $"    {pythonCmd} -m pip install requests",
                                "Ошибка установки зависимостей",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }

                    UpdateStatusLabel("Библиотека requests успешно установлена.");
                }

                // ── 4. Запускаем скрипт в отдельном окне консоли ────────────────────
                using (var runProc = new Process())
                {
                    runProc.StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/k {pythonCmd} \"{scriptPath}\"",
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        WorkingDirectory = Application.StartupPath
                    };
                    runProc.Start();
                }

                MessageBox.Show(
                    "Скрипт поиска адресов запущен!\n\n" +
                    "Следуйте инструкциям в открывшемся окне консоли.\n" +
                    "После завершения работы скрипта файл addresses.txt\n" +
                    "будет сохранён в папку data рядом с программой.",
                    "Информация",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске скрипта:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Загружает участки, узлы и метки из PostgreSQL и отображает на карте
        /// с разноцветным стилем (как при балансировке).
        /// </summary>
        private async Task LoadFromServerAndDisplayOnMap()
        {
            string pgConn = Program.BuildPgConnectionString();

            // ── 1. Читаем участки и их узлы ─────────────────────────────────────
            var regions = new List<Region>();
            var regionIndex = new Dictionary<string, Region>(StringComparer.OrdinalIgnoreCase);

            using (var pg = new NpgsqlConnection(pgConn))
            {
                await pg.OpenAsync();

                // Читаем участки
                using (var cmd = new NpgsqlCommand(
                    "SELECT \"id\", \"Название\", \"Цвет\" FROM \"Участки\" ORDER BY \"Название\"", pg))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    int idx = 0;
                    while (await reader.ReadAsync())
                    {
                        string dbId = reader["id"].ToString();
                        string colorStr = reader["Цвет"]?.ToString() ?? "";
                        string name = reader["Название"]?.ToString() ?? $"Участок {idx + 1}";

                        Color color;
                        try
                        {
                            color = colorStr.StartsWith("#")
                                ? HexToColor(colorStr)
                                : regionColors[idx % regionColors.Length];
                        }
                        catch { color = regionColors[idx % regionColors.Length]; }

                        var region = new Region
                        {
                            Id = idx++,
                            DbId = dbId,
                            Color = color,
                            Name = name
                        };

                        regions.Add(region);
                        regionIndex[dbId] = region;
                    }
                }

                // Читаем узлы (есть намеренный своп Широта↔Долгота относительно Access)
                using (var cmd = new NpgsqlCommand(
                    "SELECT \"Id участка\", \"Долгота\" AS lat, \"Широта\" AS lon, \"Номер\" " +
                    "FROM \"Узлы\" ORDER BY \"Id участка\", \"Номер\"", pg))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string rid = reader["Id участка"].ToString();
                        if (!regionIndex.TryGetValue(rid, out Region region)) continue;

                        double lat = Convert.ToDouble(reader["lat"]);
                        double lon = Convert.ToDouble(reader["lon"]);
                        region.PolygonPoints.Add(new PointLatLng(lat, lon));
                    }
                }

                // ── 2. Читаем метки, назначаем в участки ────────────────────────
                // Для меток своп НЕ применяется — PG «Широта»=lat, «Долгота»=lon
                using (var cmd = new NpgsqlCommand(
                    "SELECT \"id\", \"Широта\" AS lat, \"Долгота\" AS lon, " +
                    "\"Тип здания\", \"Улица\", \"Дом\", \"Корпус\", \"Квартира\", \"Id участка\" " +
                    "FROM \"Метки\"", pg))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    int ptId = 0;
                    while (await reader.ReadAsync())
                    {
                        double lat = Convert.ToDouble(reader["lat"]);
                        double lon = Convert.ToDouble(reader["lon"]);
                        string type = reader["Тип здания"]?.ToString() ?? "";
                        string street = reader["Улица"]?.ToString() ?? "";
                        string house = reader["Дом"]?.ToString() ?? "";
                        string korpus = reader["Корпус"]?.ToString() ?? "";
                        string flat = reader["Квартира"]?.ToString() ?? "";

                        var pt = new AddressPoint
                        {
                            Id = ptId++,
                            Latitude = lat,
                            Longitude = lon,
                            BuildingType = type,
                            Street = street,
                            House = house,
                            Corpus = korpus,
                            Flat = flat,
                            Address = BuildFullAddress(street, house, korpus, flat),
                            DistanceToPostOffice = 0
                        };

                        string rid = reader["Id участка"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(rid) && regionIndex.TryGetValue(rid, out Region region))
                            region.Points.Add(pt);
                        else
                        {
                            // Метки без участка кладём во временный «серый» регион
                            if (!regionIndex.TryGetValue("__orphan__", out Region orphan))
                            {
                                orphan = new Region
                                {
                                    Id = regions.Count,
                                    DbId = "__orphan__",
                                    Color = Color.Gray
                                };
                                regions.Add(orphan);
                                regionIndex["__orphan__"] = orphan;
                            }
                            orphan.Points.Add(pt);
                        }
                    }
                }
            }

            // ── 3. Отображаем на карте (разноцветный стиль как при балансировке) ─
            this.Invoke(new Action(() =>
            {
                gMapControl1.Overlays.Clear();
            }));

            GMapOverlay polygonsOverlay = new GMapOverlay("polygons");
            GMapOverlay markersOverlay = new GMapOverlay("markers");

            foreach (var region in regions)
            {
                // Полигон участка
                if (region.PolygonPoints.Count >= 3)
                {
                    GMapPolygon polygon = new GMapPolygon(region.PolygonPoints, region.DbId)
                    {
                        Stroke = new Pen(region.Color, 2),
                        Fill = new SolidBrush(Color.FromArgb(PolygonFillAlpha, region.Color))
                    };
                    polygon.Tag = new PolygonTag
                    {
                        DbId = region.DbId ?? "",
                        Name = region.Name ?? "",
                        Color = region.Color
                    };
                    polygonsOverlay.Polygons.Add(polygon);
                }

                // Метки участка
                foreach (var point in region.Points)
                {
                    PointLatLng pos = new PointLatLng(point.Latitude, point.Longitude);
                    Bitmap icon = CreateSmallCircleBitmap(region.Color);
                    GMarkerGoogle marker = new GMarkerGoogle(pos, icon);

                    // Переносим ВСЕ поля адреса в MarkerTag — именно его читает panel3
                    var serverTag = new MarkerTag
                    {
                        Street = point.Street ?? "",
                        House = point.House ?? "",
                        Corpus = point.Corpus ?? "",
                        Flat = point.Flat ?? "",
                        BuildingType = point.BuildingType ?? ""
                    };
                    ApplyMarkerTag(marker, serverTag);

                    markersOverlay.Markers.Add(marker);
                }

                // Центральный маркер участка (если есть узлы)
                if (region.PolygonPoints.Count > 0)
                {
                    double cLat = region.PolygonPoints.Average(p => p.Lat);
                    double cLng = region.PolygonPoints.Average(p => p.Lng);
                    GMarkerGoogle centerMarker = new GMarkerGoogle(
                        new PointLatLng(cLat, cLng),
                        GMarkerGoogleType.blue_pushpin);
                    centerMarker.ToolTipText = $"Центр участка {region.Id + 1}";
                    markersOverlay.Markers.Add(centerMarker);
                }
            }

            // Маркер почты
            GMarkerGoogle postMarker = new GMarkerGoogle(
                postOfficeLocation,
                GMarkerGoogleType.red_pushpin);
            postMarker.ToolTipText = "Почтовое отделение";
            markersOverlay.Markers.Add(postMarker);

            this.Invoke(new Action(() =>
            {
                gMapControl1.Overlays.Add(polygonsOverlay);
                gMapControl1.Overlays.Add(markersOverlay);
                gMapControl1.Refresh();

                int regCount = regions.Count(r => r.DbId != "__orphan__");
                int markerCount = regions.SelectMany(r => r.Points).Count();
                MessageBox.Show(
                    $"Загружено с сервера:\n" +
                    $"  Участков: {regCount}\n" +
                    $"  Меток: {markerCount}",
                    "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));

            balancedRegions = regions.Where(r => r.DbId != "__orphan__").ToList();
        }

        // button27 — Открыть базу данных Access в Microsoft Access
        private void button27_Click(object sender, EventArgs e)
        {
            try
            {
                EnsureAccessDatabaseStructure();
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "BD.accdb");

                if (!File.Exists(dbPath))
                {
                    MessageBox.Show(
                        $"Файл базы данных не найден:\n{dbPath}\n\n" +
                        "Убедитесь, что файл BD.accdb находится в папке data рядом с программой.",
                        "Файл не найден",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Открываем файл в ассоциированном приложении (Microsoft Access)
                Process.Start(new ProcessStartInfo
                {
                    FileName = dbPath,
                    UseShellExecute = true   // Windows сама найдёт Microsoft Access
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось открыть базу данных в Microsoft Access.\n\n" +
                    "Убедитесь, что Microsoft Access установлен на этом компьютере.\n\n" +
                    "Ошибка: " + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void button29_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "Access Database|*.accdb";
                    saveDialog.Title = "Создать новую БД Access";
                    saveDialog.FileName = $"BD_FromServer_{DateTime.Now:yyyyMMdd_HHmmss}.accdb";
                    saveDialog.InitialDirectory = Path.Combine(Application.StartupPath, "..", "..", "data");

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        Cursor = Cursors.WaitCursor;

                        // Копируем структуру БД
                        string sourcePath = Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", "data", "BD.accdb"));
                        File.Copy(sourcePath, saveDialog.FileName, true);

                        // Очищаем новую БД
                        string newConnectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={saveDialog.FileName}";
                        using (var connection = new OleDbConnection(newConnectionString))
                        {
                            connection.Open();
                            using (var cmd = new OleDbCommand("DELETE FROM Метки", connection))
                                cmd.ExecuteNonQuery();
                            using (var cmd = new OleDbCommand("DELETE FROM Узлы", connection))
                                cmd.ExecuteNonQuery();
                            using (var cmd = new OleDbCommand("DELETE FROM Участки", connection))
                                cmd.ExecuteNonQuery();
                        }

                        // Загружаем данные с сервера PostgreSQL и сохраняем в новую БД
                        await TransferFromServerToAccess(newConnectionString);

                        MessageBox.Show($"Новая БД создана и заполнена данными с сервера PostgreSQL:\n{saveDialog.FileName}");
                        Cursor = Cursors.Default;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании БД из данных сервера: " + ex.Message);
                Cursor = Cursors.Default;
            }
        }

        private async Task LoadFromServerToAcccess()
        {
            string pgConn = Program.BuildPgConnectionString();

            using (var access = new OleDbConnection(connectionString))
            {
                access.Open();

                // Очищаем таблицы в правильном порядке (сначала зависимые)
                foreach (string tbl in new[] { "Метки", "Узлы", "Участки" })
                {
                    using (var cmd = new OleDbCommand($"DELETE FROM {tbl}", access))
                        cmd.ExecuteNonQuery();
                }

                using (var pg = new NpgsqlConnection(pgConn))
                {
                    await pg.OpenAsync();

                    // ── Участки ──────────────────────────────────────────────
                    using (var pgCmd = new NpgsqlCommand(
                        "SELECT \"id\", \"Название\", \"Цвет\" FROM \"Участки\"", pg))
                    using (var pgReader = await pgCmd.ExecuteReaderAsync())
                    {
                        while (await pgReader.ReadAsync())
                        {
                            using (var cmd = new OleDbCommand(
                                "INSERT INTO Участки ([id],[Название],[Цвет]) VALUES (?,?,?)", access))
                            {
                                cmd.Parameters.AddWithValue("@id", pgReader["id"].ToString());
                                cmd.Parameters.AddWithValue("@Название", pgReader["Название"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@Цвет", pgReader["Цвет"]?.ToString() ?? "#000000");
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // ── Узлы (намеренный своп Широта↔Долгота как в TransferFromServerToAccess) ──
                    using (var pgCmd = new NpgsqlCommand(
                        "SELECT \"id\", \"Широта\", \"Долгота\", \"Id участка\", \"Номер\" " +
                        "FROM \"Узлы\" ORDER BY \"Id участка\", \"Номер\"", pg))
                    using (var pgReader = await pgCmd.ExecuteReaderAsync())
                    {
                        while (await pgReader.ReadAsync())
                        {
                            using (var cmd = new OleDbCommand(
                                "INSERT INTO Узлы ([id],[Долгота],[Широта],[Id участка],[Номер]) VALUES (?,?,?,?,?)", access))
                            {
                                cmd.Parameters.AddWithValue("@id", pgReader["id"].ToString());
                                // В Access Долгота хранит то что PG называет Широта (намеренный своп)
                                cmd.Parameters.AddWithValue("@Долгота", Convert.ToDouble(pgReader["Широта"]));
                                cmd.Parameters.AddWithValue("@Широта", Convert.ToDouble(pgReader["Долгота"]));
                                cmd.Parameters.AddWithValue("@id_участка", pgReader["Id участка"].ToString());
                                cmd.Parameters.AddWithValue("@Номер", Convert.ToInt32(pgReader["Номер"]));
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // ── Метки ─────────────────────────────────────────────────
                    using (var pgCmd = new NpgsqlCommand(
                        "SELECT \"id\", \"Широта\", \"Долгота\", \"Тип здания\", " +
                        "\"Улица\", \"Дом\", \"Корпус\", \"Квартира\", \"Id участка\" " +
                        "FROM \"Метки\"", pg))
                    using (var pgReader = await pgCmd.ExecuteReaderAsync())
                    {
                        while (await pgReader.ReadAsync())
                        {
                            object regionIdVal = pgReader["Id участка"] == DBNull.Value
                                ? (object)DBNull.Value
                                : pgReader["Id участка"].ToString();

                            using (var cmd = new OleDbCommand(
                                "INSERT INTO Метки ([id],[Долгота],[Широта],[Тип здания],[Улица],[Дом],[Корпус],[Квартира],[Id участка]) " +
                                "VALUES (?,?,?,?,?,?,?,?,?)", access))
                            {
                                cmd.Parameters.AddWithValue("@id", pgReader["id"].ToString());
                                cmd.Parameters.AddWithValue("@Долгота", Convert.ToDouble(pgReader["Долгота"]));
                                cmd.Parameters.AddWithValue("@Широта", Convert.ToDouble(pgReader["Широта"]));
                                cmd.Parameters.AddWithValue("@Тип_здания", pgReader["Тип здания"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@Улица", pgReader["Улица"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@Дом", pgReader["Дом"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@Корпус", pgReader["Корпус"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@Квартира", pgReader["Квартира"]?.ToString() ?? "");
                                cmd.Parameters.AddWithValue("@id_участка", regionIdVal);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        // ─── Чекер базы данных Access ──────────────────────────────────────────
        //
        // Использование (одна строка):
        //     EnsureAccessDatabaseStructure();
        //
        // Метод проверяет наличие файла BD.accdb, его таблицы и столбцы.
        // Если файл отсутствует — создаёт новый с нуля.
        // Если структура нарушена — пытается починить ALTER TABLE,
        // при неудаче удаляет файл и создаёт новый.
        // ──────────────────────────────────────────────────────────────────────

        private void EnsureAccessDatabaseStructure()
        {
            string cs = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath}";

            // Эталонная структура таблиц: имя → список (столбец, тип DDL для создания)
            var schema = new Dictionary<string, List<(string Column, string Ddl)>>
            {
                ["Участки"] = new List<(string, string)>
                {
                    ("id",       "TEXT(50) NOT NULL PRIMARY KEY"),
                    ("Название", "TEXT(255)"),
                    ("Цвет",     "TEXT(50)"),
                    ("Долгота",  "DOUBLE"),
                    ("Широта",   "DOUBLE")
                },
                ["Узлы"] = new List<(string, string)>
                {
                    ("id",           "TEXT(50) NOT NULL PRIMARY KEY"),
                    ("Долгота",      "DOUBLE"),
                    ("Широта",       "DOUBLE"),
                    ("Id участка",   "TEXT(50)"),
                    ("Номер",        "INTEGER")
                },
                ["Метки"] = new List<(string, string)>
                {
                    ("id",           "TEXT(50) NOT NULL PRIMARY KEY"),
                    ("Долгота",      "DOUBLE"),
                    ("Широта",       "DOUBLE"),
                    ("Тип здания",   "TEXT(255)"),
                    ("Улица",        "TEXT(255)"),
                    ("Дом",          "TEXT(50)"),
                    ("Корпус",       "TEXT(50)"),
                    ("Квартира",     "TEXT(50)"),
                    ("Id участка",   "TEXT(50)"),
                    ("Участок",      "INTEGER")
                }
            };

            // ── Шаг 1: создать файл если отсутствует ─────────────────────────
            if (!File.Exists(dbPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
                CreateFreshAccessDatabase(dbPath, schema);
                return;
            }

            // ── Шаг 2: файл есть — проверяем структуру ────────────────────────
            try
            {
                using (var conn = new OleDbConnection(cs))
                {
                    conn.Open();
                    bool repaired = TryRepairAccessStructure(conn, schema);
                    if (!repaired)
                    {
                        // Repair невозможен — пересоздаём файл
                        conn.Close();
                        File.Delete(dbPath);
                        CreateFreshAccessDatabase(dbPath, schema);
                    }
                }
            }
            catch
            {
                // Файл повреждён — удаляем и создаём заново
                try { File.Delete(dbPath); } catch { /* ничего */ }
                CreateFreshAccessDatabase(dbPath, schema);
                ButtonEnabledInBalancedRegions(true);
            }
        }

        /// <summary>
        /// Пробует добавить недостающие таблицы/столбцы через DDL.
        /// Возвращает true если структура приведена к норме,
        /// false если это невозможно (например, тип столбца конфликтует).
        /// </summary>
        private bool TryRepairAccessStructure(
            OleDbConnection conn,
            Dictionary<string, List<(string Column, string Ddl)>> schema)
        {
            try
            {
                // Получаем список существующих таблиц
                DataTable tables = conn.GetSchema("Tables");
                var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow row in tables.Rows)
                {
                    string tableType = row["TABLE_TYPE"]?.ToString() ?? "";
                    if (tableType == "TABLE")
                        existingTables.Add(row["TABLE_NAME"].ToString());
                }

                foreach (var kvp in schema)
                {
                    string tableName = kvp.Key;
                    var columns = kvp.Value;

                    if (!existingTables.Contains(tableName))
                    {
                        // Таблица отсутствует — создаём
                        string colDefs = string.Join(", ",
                            columns.Select(c => $"[{c.Column}] {c.Ddl}"));
                        string createSql = $"CREATE TABLE [{tableName}] ({colDefs})";
                        using (var cmd = new OleDbCommand(createSql, conn))
                            cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // Таблица есть — проверяем каждый столбец
                        DataTable cols = conn.GetSchema("Columns",
                            new[] { null, null, tableName, null });

                        var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (DataRow row in cols.Rows)
                            existingCols.Add(row["COLUMN_NAME"].ToString());

                        foreach (var (column, ddl) in columns)
                        {
                            if (!existingCols.Contains(column))
                            {
                                // Столбец отсутствует — добавляем
                                // Убираем PRIMARY KEY из ALTER (Access не поддерживает)
                                string alterDdl = ddl
                                    .Replace("NOT NULL PRIMARY KEY", "")
                                    .Replace("PRIMARY KEY", "")
                                    .Replace("NOT NULL", "")
                                    .Trim();

                                string alterSql = $"ALTER TABLE [{tableName}] ADD COLUMN [{column}] {alterDdl}";
                                using (var cmd = new OleDbCommand(alterSql, conn))
                                    cmd.ExecuteNonQuery();
                            }
                            // Примечание: изменение типа существующего столбца в Access
                            // через DDL невозможно без пересоздания таблицы.
                            // Если тип неверный — вернём false чтобы пересоздать всю БД.
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Создаёт новый пустой файл .accdb со всеми нужными таблицами и столбцами.
        /// Использует ADOX через COM (Microsoft ADO Ext. for DDL and Security).
        /// </summary>
        private void CreateFreshAccessDatabase(
            string dbPath,
            Dictionary<string, List<(string Column, string Ddl)>> schema)
        {
            // Создаём пустой .accdb через ADOX Catalog
            // (требует Microsoft ADO Ext. — есть везде где стоит ACE/Access)
            Type catalogType = Type.GetTypeFromProgID("ADOX.Catalog");
            if (catalogType == null)
                throw new InvalidOperationException(
                    "ADOX.Catalog недоступен. Убедитесь, что установлен Microsoft Access Database Engine.");

            dynamic catalog = Activator.CreateInstance(catalogType);
            catalog.Create($"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath}");
            // Закрываем соединение ADOX
            try { catalog.ActiveConnection.Close(); } catch { /* ничего */ }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(catalog);

            // Теперь создаём таблицы через OleDb DDL
            string cs = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath}";
            using (var conn = new OleDbConnection(cs))
            {
                conn.Open();

                foreach (var kvp in schema)
                {
                    string tableName = kvp.Key;
                    var columns = kvp.Value;

                    // ACCESS не поддерживает PRIMARY KEY inline для TEXT — делаем без него,
                    // уникальность обеспечивается логикой приложения
                    string colDefs = string.Join(", ",
                        columns.Select(c =>
                        {
                            string ddl = c.Ddl
                                .Replace("NOT NULL PRIMARY KEY", "")
                                .Replace("PRIMARY KEY", "")
                                .Replace("NOT NULL", "")
                                .Trim();
                            return $"[{c.Column}] {ddl}";
                        }));

                    string createSql = $"CREATE TABLE [{tableName}] ({colDefs})";
                    using (var cmd = new OleDbCommand(createSql, conn))
                        cmd.ExecuteNonQuery();
                }
            }
        }

        private async void DataTransferButton_Click(object sender, EventArgs e)
        {
            bool isExternalDb = _externalDbPaths.ContainsKey(comboBox2.Text);
            bool isAccessSource = comboBox2.Text == "Access" || isExternalDb;

            if (comboBox2.Text == "Файл" && comboBox3.Text == "Карта" && comboBox4.Text == "Метки")
            {
                try
                {
                    UpdateStatusLabel("Загрузка данных с файла addresses на карту...");
                    LoadMarkersFromAddresses();
                    UpdateStatusLabel("Данные с файла addresses загружены на карту.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных меток из файла addresses на карту:\n" + ex.Message,
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            else if (isAccessSource && comboBox3.Text == "Карта" && comboBox4.Text == "Метки")
            {
                try
                {
                    string activeCs = GetActiveAccessConnectionString();
                    UpdateStatusLabel("Загрузка данных меток с БД (Access) на карту...");
                    if (!isExternalDb) EnsureAccessDatabaseStructure();
                    LoadMarkersFromDatabaseToMap(activeCs);
                    UpdateStatusLabel("Данные меток с БД (Access) загружены на карту.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных меток из БД (Access) на карту:\n" + ex.Message,
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (isAccessSource && comboBox3.Text == "Карта" && comboBox4.Text == "Участки")
            {
                try
                {
                    string activeCs = GetActiveAccessConnectionString();
                    UpdateStatusLabel("Загрузка данных участков с БД (Access) на карту...");
                    if (!isExternalDb) EnsureAccessDatabaseStructure();
                    var regions = LoadRegionsFromNewTables(activeCs);
                    balancedRegions = regions;
                    DisplayRegionsOnMap(regions);
                    LoadMarkersFromDatabase(regions, activeCs);
                    UpdateStatusLabel("Данные участков с БД (Access) загружены на карту.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных участков из БД (Access) на карту:\n" + ex.Message,
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (isAccessSource && comboBox3.Text == "Карта" && comboBox4.Text == "Все данные")
            {
                try
                {
                    string activeCs = GetActiveAccessConnectionString();
                    UpdateStatusLabel("Загрузка данных с БД (Access) на карту...");
                    if (!isExternalDb) EnsureAccessDatabaseStructure();
                    var regions = LoadRegionsFromNewTables(activeCs);
                    balancedRegions = regions;
                    DisplayRegionsOnMap(regions);
                    LoadMarkersFromDatabaseToMap(activeCs);
                    UpdateStatusLabel("Данные с БД (Access) загружены на карту.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных из БД (Access) на карту:\n" + ex.Message,
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            else if (isAccessSource && comboBox3.Text == "Сервер" && comboBox4.Text == "Все данные")
            {
                try
                {
                    UpdateStatusLabel("Загрузка данных с БД (Access) на сервер PostgreSQL...");
                    if (!isExternalDb) EnsureAccessDatabaseStructure();
                    await TransferRegionsToServer(GetActiveAccessConnectionString());
                    await TransferMarkersToServer(GetActiveAccessConnectionString());
                    UpdateStatusLabel("Данные с БД (Access) загружены на сервер PostgreSQL.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных из БД (Access) на сервер PostgreSQL:\n" + ex.Message,
                       "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            else if (comboBox2.Text == "Сервер" && comboBox3.Text == "Карта" && comboBox4.Text == "Все данные")
            {
                try
                {
                    UpdateStatusLabel("Загрузка данных с сервера PostgreSQL на карту...");
                    await LoadFromServerAndDisplayOnMap();
                    UpdateStatusLabel("Данные с сервера PostgreSQL загружены на карту.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных с сервера PostgreSQL на карту:\n" + ex.Message,
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (comboBox2.Text == "Сервер" && comboBox3.Text == "Access" && comboBox4.Text == "Все данные")
            {
                try
                {
                    UpdateStatusLabel("Перенос данных с сервера PostgreSQL в БД (Access)...");
                    EnsureAccessDatabaseStructure();
                    await LoadFromServerToAcccess();
                    UpdateStatusLabel("Данные с сервера PostgreSQL успешно перенесены в БД (Access).");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при переносе данных с сервера PostgreSQL в БД (Access):\n" + ex.Message,
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите корректные параметры для переноса данных.",
                    "Неверные параметры", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void comboBox2_TextChanged(object sender, EventArgs e)
        {
            // Перехватываем выбор импорта внешней БД
            if (comboBox2.Text == "Импорт данных Access...")
            {
                HandleImportExternalDb();
                return; // ComboBoxTextUpdate вызовется после смены item
            }

            ComboBoxTextUpdate();
        }

        /// <summary>
        /// Открывает проводник для выбора внешнего .accdb файла,
        /// проверяет его структуру и добавляет в comboBox2.
        /// </summary>
        private void HandleImportExternalDb()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Выберите файл базы данных Access";
                dlg.Filter = "Access Database|*.accdb;*.mdb";
                dlg.Multiselect = false;

                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    // Пользователь отменил — возвращаем предыдущий выбор
                    comboBox2.SelectedIndex = 0;
                    return;
                }

                string path = dlg.FileName;
                string dbName = Path.GetFileNameWithoutExtension(path);

                // Если эта БД уже добавлена — просто выбираем
                if (_externalDbPaths.ContainsKey(dbName))
                {
                    comboBox2.SelectedItem = dbName;
                    return;
                }

                // Проверяем и при необходимости чиним структуру
                string error = ValidateOrRepairExternalDb(path);
                if (error != null)
                {
                    MessageBox.Show($"Невозможно использовать выбранную БД:\n{error}",
                        "Ошибка структуры БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    comboBox2.SelectedIndex = 0;
                    return;
                }

                // Всё хорошо — добавляем в список
                _externalDbPaths[dbName] = path;
                if (!comboBox2.Items.Contains(dbName))
                    comboBox2.Items.Insert(comboBox2.Items.IndexOf("Импорт данных Access..."), dbName);

                // Убираем временное срабатывание _TextChanged чтобы не зациклиться
                comboBox2.TextChanged -= comboBox2_TextChanged;
                comboBox2.SelectedItem = dbName;
                comboBox2.TextChanged += comboBox2_TextChanged;

                ComboBoxTextUpdate();
            }
        }

        /// <summary>
        /// Пытается проверить/починить структуру внешней Access БД.
        /// Возвращает null если всё OK, или строку с описанием ошибки если БД несовместима.
        /// </summary>
        private string ValidateOrRepairExternalDb(string path)
        {
            string cs = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path}";

            var schema = new Dictionary<string, List<(string Column, string Ddl)>>
            {
                ["Участки"] = new List<(string, string)>
                {
                    ("id",       "TEXT(50) NOT NULL PRIMARY KEY"),
                    ("Название", "TEXT(255)"),
                    ("Цвет",     "TEXT(50)")
                },
                ["Узлы"] = new List<(string, string)>
                {
                    ("id",         "TEXT(50) NOT NULL PRIMARY KEY"),
                    ("Долгота",    "DOUBLE"),
                    ("Широта",     "DOUBLE"),
                    ("Id участка", "TEXT(50)"),
                    ("Номер",      "INTEGER")
                },
                ["Метки"] = new List<(string, string)>
                {
                    ("id",         "TEXT(50) NOT NULL PRIMARY KEY"),
                    ("Долгота",    "DOUBLE"),
                    ("Широта",     "DOUBLE"),
                    ("Тип здания", "TEXT(255)"),
                    ("Улица",      "TEXT(255)"),
                    ("Дом",        "TEXT(50)"),
                    ("Корпус",     "TEXT(50)"),
                    ("Квартира",   "TEXT(50)")
                }
            };

            try
            {
                using (var conn = new OleDbConnection(cs))
                {
                    conn.Open();

                    // Получаем список существующих таблиц
                    DataTable tables = conn.GetSchema("Tables");
                    var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (DataRow row in tables.Rows)
                        if (row["TABLE_TYPE"]?.ToString() == "TABLE")
                            existingTables.Add(row["TABLE_NAME"].ToString());

                    // Если ни одной из нужных таблиц нет — БД несовместима
                    bool hasAny = schema.Keys.Any(t => existingTables.Contains(t));
                    if (!hasAny && existingTables.Count > 0)
                        return "БД не содержит ни одной из необходимых таблиц (Участки, Узлы, Метки). Структура файла несовместима.";

                    // Пробуем починить (добавить недостающие таблицы/столбцы)
                    bool repaired = TryRepairAccessStructure(conn, schema);
                    if (!repaired)
                        return "Структура БД несовместима и не может быть исправлена автоматически.";

                    return null; // OK
                }
            }
            catch (Exception ex)
            {
                return $"Не удалось открыть файл: {ex.Message}";
            }
        }

        private void comboBox3_TextChanged(object sender, EventArgs e)
        {
            ComboBoxTextUpdate();
        }

        private void ComboBoxTextUpdate()
        {
            bool isExternalDb = _externalDbPaths.ContainsKey(comboBox2.Text);

            if (comboBox2.Text == "Файл" && comboBox3.Text == "Карта")
            {
                comboBox4.Items.Clear();
                comboBox4.Items.Add("Метки");
                comboBox4.SelectedItem = comboBox4.Items[0];
                DataTransferButton.Enabled = true;
            }
            else if ((comboBox2.Text == "Access" || isExternalDb) && comboBox3.Text == "Карта")
            {
                comboBox4.Items.Clear();
                comboBox4.Items.Add("Все данные");
                comboBox4.Items.Add("Метки");
                comboBox4.Items.Add("Участки");
                comboBox4.SelectedItem = comboBox4.Items[0];
                DataTransferButton.Enabled = true;
            }
            else if ((comboBox2.Text == "Access" || isExternalDb) && comboBox3.Text == "Сервер")
            {
                comboBox4.Items.Clear();
                comboBox4.Items.Add("Все данные");
                comboBox4.SelectedItem = comboBox4.Items[0];
                DataTransferButton.Enabled = true;
            }
            else if (comboBox2.Text == "Сервер" && comboBox3.Text == "Карта")
            {
                comboBox4.Items.Clear();
                comboBox4.Items.Add("Все данные");
                comboBox4.SelectedItem = comboBox4.Items[0];
                DataTransferButton.Enabled = true;
            }
            else if (comboBox2.Text == "Сервер" && comboBox3.Text == "Access")
            {
                comboBox4.Items.Clear();
                comboBox4.Items.Add("Все данные");
                comboBox4.SelectedItem = comboBox4.Items[0];
                DataTransferButton.Enabled = true;
            }
            else
            {
                comboBox4.Items.Clear();
                comboBox4.Items.Add("Недоступно");
                comboBox4.SelectedItem = comboBox4.Items[0];
                DataTransferButton.Enabled = false;
            }
        }

        /// <summary>
        /// Возвращает строку подключения для текущего выбора comboBox2.
        /// Если выбрана внешняя БД — возвращает её connection string.
        /// Если выбрана "Access" — возвращает стандартную connectionString.
        /// </summary>
        private string GetActiveAccessConnectionString()
        {
            string sel = comboBox2.Text;
            if (_externalDbPaths.TryGetValue(sel, out string extPath))
                return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={extPath}";
            return connectionString;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // ─── Сохранение данных маркера из panel3 ──────────────────────────
            string buildingType = comboBox5.SelectedItem?.ToString() ?? "";
            string street = textBox1.Text.Trim();
            string house = textBox2.Text.Trim();
            string corpus = textBox3.Text.Trim();
            string flat = textBox4.Text.Trim();

            // Валидация
            if (string.IsNullOrWhiteSpace(buildingType) || buildingType == "Не указан")
            {
                MessageBox.Show("Укажите тип здания!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(street))
            {
                MessageBox.Show("Укажите улицу!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(house) &&
                string.IsNullOrWhiteSpace(corpus) &&
                string.IsNullOrWhiteSpace(flat))
            {
                MessageBox.Show("Укажите хотя бы одно из полей: Дом, Корпус или Квартира!",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newTag = new MarkerTag
            {
                Street = street,
                House = house,
                Corpus = corpus,
                Flat = flat,
                BuildingType = buildingType
            };

            if (_isCreatingMarker)
            {
                // Создаём новую метку на сохранённых координатах
                var markersOverlay = gMapControl1.Overlays.FirstOrDefault(o => o.Id == "markers");
                if (markersOverlay == null)
                {
                    markersOverlay = new GMapOverlay("markers");
                    gMapControl1.Overlays.Add(markersOverlay);
                }

                Bitmap icon = CreateSmallCircleBitmap(Color.Gray);
                var newMarker = new GMarkerGoogle(_newMarkerPoint, icon);
                ApplyMarkerTag(newMarker, newTag);
                markersOverlay.Markers.Add(newMarker);
                gMapControl1.Refresh();

                _isCreatingMarker = false;
            }
            else if (_selectedMarker != null)
            {
                // Обновляем существующую метку
                // Сохраняем DbId если он был
                if (_selectedMarker.Tag is MarkerTag old)
                    newTag.DbId = old.DbId;

                ApplyMarkerTag(_selectedMarker, newTag);
                gMapControl1.Refresh();
                _selectedMarker = null;
            }

            panel3.Visible = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Закрыть panel3 без сохранения
            panel3.Visible = false;
            _selectedMarker = null;
            _isCreatingMarker = false;
        }

        private void gMapControl1_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            if (!(item is GMarkerGoogle marker)) return;

            // ─── Режим удаления ───────────────────────────────────────────────────
            if (currentMapMode == MapMode.DeleteObject)
            {
                if (marker.ToolTipText == "Почтовое отделение") return;

                if (marker.ToolTipText?.StartsWith("Центр участка") == true)
                {
                    // Удаляем весь участок + его узлы
                    var region = balancedRegions?.FirstOrDefault(r =>
                        r.CenterPoint.HasValue &&
                        Math.Abs(r.CenterPoint.Value.Lat - marker.Position.Lat) < 0.0001 &&
                        Math.Abs(r.CenterPoint.Value.Lng - marker.Position.Lng) < 0.0001);

                    if (region != null && !string.IsNullOrEmpty(region.DbId))
                    {
                        try
                        {
                            using (var conn = new OleDbConnection(connectionString))
                            {
                                conn.Open();
                                using (var cmd = new OleDbCommand(
                                    "DELETE FROM Узлы WHERE [Id участка]=?", conn))
                                {
                                    cmd.Parameters.AddWithValue("@id", region.DbId);
                                    cmd.ExecuteNonQuery();
                                }
                                using (var cmd = new OleDbCommand(
                                    "DELETE FROM Участки WHERE [id]=?", conn))
                                {
                                    cmd.Parameters.AddWithValue("@id", region.DbId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            var regions = LoadRegionsFromNewTables();
                            balancedRegions = regions;
                            DisplayRegionsOnMap(regions);
                            UpdateRegionsComboBox();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Ошибка при удалении участка:\n" + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    return;
                }

                // Обычная метка дома — удаляем с карты (и из БД если есть DbId)
                if (marker.Tag == null && string.IsNullOrWhiteSpace(marker.ToolTipText)) return;
                if (marker.Tag is MarkerTag mt && !string.IsNullOrEmpty(mt.DbId))
                {
                    try
                    {
                        using (var conn = new OleDbConnection(connectionString))
                        {
                            conn.Open();
                            using (var cmd = new OleDbCommand("DELETE FROM Метки WHERE [id]=?", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", mt.DbId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка удаления метки из БД: {ex.Message}");
                    }
                }
                var ov = gMapControl1.Overlays.FirstOrDefault(o => o.Markers.Contains(marker));
                ov?.Markers.Remove(marker);
                gMapControl1.Refresh();
                return;
            }

            // ─── Остальные режимы — только None показывает panel3 ────────────────
            if (currentMapMode != MapMode.None) return;

            // Пропускаем служебные маркеры
            if (marker.Tag == null && string.IsNullOrWhiteSpace(marker.ToolTipText)) return;
            if (marker.ToolTipText == "Почтовое отделение") return;
            if (marker.ToolTipText?.StartsWith("Центр участка") == true) return;

            _selectedMarker = marker;
            _isCreatingMarker = false;

            MarkerTag tag = GetMarkerTag(marker);
            SetComboBox5Value(tag.BuildingType);
            textBox1.Text = tag.Street ?? "";
            textBox2.Text = tag.House ?? "";
            textBox3.Text = tag.Corpus ?? "";
            textBox4.Text = tag.Flat ?? "";

            panel4.Visible = false;
            _selectedPolygon = null;
            panel3.Visible = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            IntegrityCheckForm integrityCheckForm = new IntegrityCheckForm();
            integrityCheckForm.Show();
        }

        /// <summary>
        /// button20 — создать новый участок вручную.
        /// Показывает panel4 с автоматически подобранным именем.
        /// Сохранение происходит через button10.
        /// </summary>
        private void button20_Click(object sender, EventArgs e)
        {
            // Собираем уже занятые имена из comboBox1
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var it in comboBox1.Items)
                existingNames.Add(it.ToString());

            // Находим первый свободный номер
            int n = 1;
            while (existingNames.Contains($"Участок {n}")) n++;

            _isCreatingRegion = true;
            _selectedPolygon = null;

            textBox8.Text = $"Участок {n}";
            squarePicker1.SelectedColor = System.Windows.Media.Colors.RoyalBlue;

            panel3.Visible = false;
            panel4.Visible = true;
        }

        // ─── panel4: редактирование участка (полигона) ────────────────────────────

        /// <summary>
        /// Клик по полигону на карте. В режиме None — показывает panel4 с данными участка.
        /// </summary>
        private void gMapControl1_OnPolygonClick(GMapPolygon item, MouseEventArgs e)
        {
            if (currentMapMode != MapMode.None)
                return;

            _selectedPolygon = item;
            panel3.Visible = false;

            if (item.Tag is PolygonTag pt)
            {
                textBox8.Text = pt.Name;
                squarePicker1.SelectedColor = System.Windows.Media.Color.FromArgb(
                    pt.Color.A, pt.Color.R, pt.Color.G, pt.Color.B);
            }
            else
            {
                textBox8.Text = item.Name ?? "";
                squarePicker1.SelectedColor = System.Windows.Media.Colors.Gray;
            }

            panel4.Visible = true;
        }

        /// <summary>
        /// button6 — закрыть panel4 без сохранения.
        /// </summary>
        private void button6_Click(object sender, EventArgs e)
        {
            panel4.Visible = false;
            _selectedPolygon = null;
        }

        /// <summary>
        /// button10 — сохранить изменения названия и цвета выбранного участка.
        /// </summary>
        private void button10_Click(object sender, EventArgs e)
        {
            // ─── Режим создания нового участка ───────────────────────────────────
            if (_isCreatingRegion)
            {
                string newName = textBox8.Text.Trim();
                if (string.IsNullOrEmpty(newName))
                {
                    MessageBox.Show("Введите название участка!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var mc = squarePicker1.SelectedColor;
                Color newColor = Color.FromArgb(mc.A, mc.R, mc.G, mc.B);
                string hexColor = $"#{newColor.R:X2}{newColor.G:X2}{newColor.B:X2}";
                string newId = Guid.NewGuid().ToString();

                try
                {
                    using (var conn = new OleDbConnection(connectionString))
                    {
                        conn.Open();
                        using (var cmd = new OleDbCommand(
                            "INSERT INTO Участки ([id],[Название],[Цвет]) VALUES (?,?,?)", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", newId);
                            cmd.Parameters.AddWithValue("@Название", newName);
                            cmd.Parameters.AddWithValue("@Цвет", hexColor);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Добавляем в comboBox1 и выбираем
                    if (!comboBox1.Items.Contains(newName))
                        comboBox1.Items.Add(newName);
                    comboBox1.SelectedItem = newName;

                    MessageBox.Show($"Участок «{newName}» создан.\nТеперь добавьте узлы через режим «Создать узел».",
                        "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при создании участка:\n" + ex.Message,
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _isCreatingRegion = false;
                panel4.Visible = false;
                return;
            }

            // ─── Режим редактирования существующего участка ──────────────────────
            {
                if (_selectedPolygon == null)
                {
                    MessageBox.Show("Участок не выбран!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!(_selectedPolygon.Tag is PolygonTag polygonTag))
                {
                    MessageBox.Show("Нет данных тега у участка!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string editName = textBox8.Text.Trim();
                var mc2 = squarePicker1.SelectedColor;
                Color editColor = Color.FromArgb(mc2.A, mc2.R, mc2.G, mc2.B);
                string editHex = $"#{editColor.R:X2}{editColor.G:X2}{editColor.B:X2}";

                // Сохраняем в Access БД
                if (!string.IsNullOrEmpty(polygonTag.DbId))
                {
                    try
                    {
                        using (var conn = new OleDbConnection(connectionString))
                        {
                            conn.Open();
                            string updateSql = "UPDATE Участки SET [Название]=?, [Цвет]=? WHERE [id]=?";
                            using (var cmd = new OleDbCommand(updateSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@Название", editName);
                                cmd.Parameters.AddWithValue("@Цвет", editHex);
                                cmd.Parameters.AddWithValue("@id", polygonTag.DbId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при сохранении участка в БД:\n" + ex.Message,
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // Обновляем тег полигона
                polygonTag.Name = editName;
                polygonTag.Color = editColor;

                // Обновляем внешний вид полигона на карте
                _selectedPolygon.Stroke = new Pen(editColor, 2);
                _selectedPolygon.Fill = new SolidBrush(Color.FromArgb(PolygonFillAlpha, editColor));
                gMapControl1.Refresh();

                // Обновляем balancedRegions если они есть
                if (balancedRegions != null)
                {
                    var region = balancedRegions.FirstOrDefault(r => r.DbId == polygonTag.DbId);
                    if (region != null)
                    {
                        region.Name = editName;
                        region.Color = editColor;
                    }
                }

                panel4.Visible = false;
                _selectedPolygon = null;
            } // конец блока редактирования
        }

        private void button18_Click(object sender, EventArgs e)
        {
            button18.Visible = false;
            button17.Visible = true;
            panel1.Visible = false;
        }

        private void button17_Click(object sender, EventArgs e)
        {
            button18.Visible = true;
            button17.Visible = false;
            panel1.Visible = true;
        }

        // метки
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            var markersOverlay = gMapControl1.Overlays.FirstOrDefault(o => o.Id == "markers");
            if (markersOverlay == null)
            {
                markersOverlay = new GMapOverlay("markers");
                gMapControl1.Overlays.Add(markersOverlay);
            }

            if(checkBox2.Checked)
                markersOverlay.IsVisibile = true;
            else if(!checkBox2.Checked)
                markersOverlay.IsVisibile = false;

            gMapControl1.Refresh();
        }

        // полигоны
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            var polygonsOverlay = gMapControl1.Overlays.FirstOrDefault(o => o.Id == "polygons");
            if (polygonsOverlay == null)
            {
                polygonsOverlay = new GMapOverlay("polygons");
                gMapControl1.Overlays.Add(polygonsOverlay);
            }

            if (checkBox4.Checked)
                polygonsOverlay.IsVisibile = true;
            else if (!checkBox4.Checked)
                polygonsOverlay.IsVisibile = false;

            gMapControl1.Refresh();
        }
    }
}
