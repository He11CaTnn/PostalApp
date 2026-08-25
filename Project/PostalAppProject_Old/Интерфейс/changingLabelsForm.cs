using CuoreUI.Controls;
using GMap.NET;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Интерфейс
{
    public partial class changingLabelsForm : Form
    {
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();

        private bool gmapStorage1;
        private bool gmapStorage2;
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private bool isCreatingMode = false;
        private bool isEditingMode = false;
        private bool isMovingMode = false;
        private bool isDeletingMode = false;
        private DataBase.Markers selectedMarker = null;
        private DataBase.Markers selectedMarkerBackup = null; // Резервная копия для отката
        private GMapMarker selectedGMapMarker = null;

        private string CreateButtonText;
        private string MoveButtonText;
        private string StopButtonText = "Прекратить";

        // Отслеживание изменений
        private Dictionary<Guid, DataBase.Markers> pendingChanges = new Dictionary<Guid, DataBase.Markers>(); // Измененные метки
        private HashSet<Guid> pendingDeletions = new HashSet<Guid>(); // Удаленные метки
        private List<DataBase.Markers> pendingCreations = new List<DataBase.Markers>(); // Новые метки

        public changingLabelsForm()
        {
            InitializeComponent();
            creatingLabelsTimer.Start();
            applyRadius();
        }

        public static class RoundHelper
        {
            public static void Apply(Control ctl, int radius = 15)
            {
                if (ctl.Width == 0 || ctl.Height == 0)
                    return;
                int r = Math.Min(radius, Math.Min(ctl.Width / 2, ctl.Height / 2));
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.StartFigure();
                    path.AddArc(0, 0, r, r, 180, 90);
                    path.AddArc(ctl.Width - r, 0, r, r, 270, 90);
                    path.AddArc(ctl.Width - r, ctl.Height - r, r, r, 0, 90);
                    path.AddArc(0, ctl.Height - r, r, r, 90, 90);
                    path.CloseFigure();
                    ctl.Region?.Dispose();
                    ctl.Region = new Region(path);
                }
            }
            public static void Attach(Control ctl, int radius = 15)
            {
                Apply(ctl, radius);
                ctl.Resize += (s, e) =>
                {
                    Apply(ctl, radius);
                };
            }
        }

        private void applyRadius()
        {
            RoundHelper.Attach(gMapPanel, 45);
            RoundHelper.Attach(gMapPanel1, 45);
        }

        private void upperButton3_Click(object sender, EventArgs e)
        {
            directorForm bf = new directorForm();
            bf.Show();
            Logger.Info("Выход с формы редактирования меток на форму директора");
            this.Close();
        }

        private void upperButton2_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
                this.WindowState = FormWindowState.Maximized;
            else
                this.WindowState = FormWindowState.Normal;
        }

        private void upperButton1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void upperPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void upperLabel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }
        private void upperPictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void creatingLabelsTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (gmapStorage1)
            {
                diff = creatingLabelsPanel.Height - creatingLabelsPanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                creatingLabelsPanel.Height -= step;
                editLabelsPanel.Top -= step;
                if (creatingLabelsPanel.Height <= creatingLabelsPanel.MinimumSize.Height)
                {
                    creatingLabelsPanel.Height = creatingLabelsPanel.MinimumSize.Height;
                    gmapStorage1 = false;
                    creatingLabelsTimer.Stop();
                }
            }
            else
            {
                diff = creatingLabelsPanel.MaximumSize.Height - creatingLabelsPanel.Height;
                step = Math.Max(2, diff / 5);
                creatingLabelsPanel.Height += step;
                editLabelsPanel.Top += step;
                if (creatingLabelsPanel.Height >= creatingLabelsPanel.MaximumSize.Height)
                {
                    creatingLabelsPanel.Height = creatingLabelsPanel.MaximumSize.Height;
                    gmapStorage1 = true;
                    creatingLabelsTimer.Stop();
                }
            }
        }

        private void editLabelsTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (gmapStorage2)
            {
                diff = editLabelsPanel.Height - editLabelsPanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                editLabelsPanel.Height -= step;
                if (editLabelsPanel.Height <= editLabelsPanel.MinimumSize.Height)
                {
                    editLabelsPanel.Height = editLabelsPanel.MinimumSize.Height;
                    gmapStorage2 = false;
                    editLabelsTimer.Stop();
                }
            }
            else
            {
                diff = editLabelsPanel.MaximumSize.Height - editLabelsPanel.Height;
                step = Math.Max(2, diff / 5);
                editLabelsPanel.Height += step;
                if (editLabelsPanel.Height >= editLabelsPanel.MaximumSize.Height)
                {
                    editLabelsPanel.Height = editLabelsPanel.MaximumSize.Height;
                    gmapStorage2 = true;
                    editLabelsTimer.Stop();
                }
            }
        }

        private void creatingLabelsPanel1_Click(object sender, EventArgs e)
        {
            creatingLabelsTimer.Start();
        }

        private void editLabelsPanel1_Click(object sender, EventArgs e)
        {
            editLabelsTimer.Start();
        }

        private async void changingLabelsForm_Load(object sender, EventArgs e)
        {
            CreateButtonText = creatingLabelsButton1.Content;
            MoveButtonText = editLabelsButton2.Content;

            ComboBoxAddItems(editLabelsComboBox1);
            ComboBoxAddItems(creatingLabelsComboBox1);

            await Map.InitializeMap(gMapControl);
            await Map.LoadMarkers(gMapControl);
            await Map.LoadRegions(gMapControl);
        }

        private void ComboBoxAddItems(cuiComboBox cuiComboBox)
        {
            cuiComboBox.Items = new string[0];
            for (int i = 0; i < Marker._typeBuildings.Count; i++)
                cuiComboBox.AddItem(Marker._typeBuildings[i]);
        }

        private void gMapButton1_1_Click(object sender, EventArgs e)
        {
            gMapControl.Zoom++;
        }

        private void gMapButton1_2_Click(object sender, EventArgs e)
        {
            gMapControl.Zoom--;
        }

        private void gmapButton1_3_Click(object sender, EventArgs e)
        {
            gMapControl.Position = Map.startPosition;
        }

        private async void creatingLabelsButton1_Click(object sender, EventArgs e)
        {
            if (isCreatingMode)
            {
                DeactivateCreatingMode();
                creatingLabelsButton1.Content = CreateButtonText;
            }
            else
            {
                if (ValidateCreatingFields())
                {
                    isCreatingMode = true;
                    creatingLabelsButton1.Content = StopButtonText;
                }
            }
        }

        private async void editLabelsButton1_Click(object sender, EventArgs e)
        {
            if (selectedMarker == null)
            {
                Logger.ShowWarning("Выберите метку на карте");
                return;
            }

            // Сохраняем изменения локально
            SaveMarkerChangesLocally();
        }

        private void editLabelsButton2_Click(object sender, EventArgs e)
        {
            if (selectedMarker == null)
            {
                Logger.ShowWarning("Выберите метку на карте");
                return;
            }

            if (isMovingMode)
            {
                // Деактивируем режим перемещения
                DeactivateMovingMode();
                editLabelsButton2.Content = MoveButtonText;
            }
            else
            {
                // Активируем режим перемещения
                isMovingMode = true;
                editLabelsButton2.Content = StopButtonText;
            }
        }

        private async void editLabelsButton3_Click(object sender, EventArgs e)
        {
            if (selectedMarker == null)
            {
                Logger.ShowWarning("Выберите метку на карте");
                return;
            }

            // Подтверждаем удаление
            var result = Logger.ShowYesNo("Удалить выбранную метку?");
            if (result == DialogResult.Yes)
            {
                try
                {
                    // Отмечаем метку как удаленную локально
                    pendingDeletions.Add(selectedMarker.Id);

                    // Удаляем с карты
                    if (selectedGMapMarker != null)
                        selectedGMapMarker.Overlay.Markers.Remove(selectedGMapMarker);

                    // Сбрасываем состояние
                    selectedMarker = null;
                    selectedGMapMarker = null;

                    // Очищаем поля редактирования
                    ClearEditFields();

                    Logger.ShowInfo("Метка отмечена для удаления.\nНажмите «Сохранить изменения» для подтверждения");
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка удаления метки", ex);
                    Logger.ShowError("Ошибка удаления метки");
                }
            }
        }

        private async void gMapControl_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            selectedGMapMarker = item;
            Guid markerId = item.Tag as Guid? ?? Guid.Empty;

            selectedMarker = await GetMarkerById(markerId);

            if (selectedMarker != null)
            {
                // Создаем резервную копию для отката
                selectedMarkerBackup = new DataBase.Markers
                {
                    Id = selectedMarker.Id,
                    Street = selectedMarker.Street,
                    House = selectedMarker.House,
                    Building = selectedMarker.Building,
                    Apartment = selectedMarker.Apartment,
                    TypeBuilding = selectedMarker.TypeBuilding,
                    Latitude = selectedMarker.Latitude,
                    Longitude = selectedMarker.Longitude,
                    IdRegion = selectedMarker.IdRegion,
                    IdReaders = selectedMarker.IdReaders
                };

                LoadMarkerToEditFields(selectedMarker);
            }
        }

        private async void gMapControl_OnMapClick(PointLatLng pointClick, MouseEventArgs e)
        {
            if (isCreatingMode)
                await CreateMarkerAtPoint(pointClick);
            else if (isMovingMode && selectedMarker != null)
                await MoveMarkerToPoint(pointClick);
        }

        private void gMapControl_MouseMove(object sender, MouseEventArgs e)
        {
            bool anyModeActive = isCreatingMode || isEditingMode || isMovingMode || isDeletingMode;

            if (anyModeActive && gMapControl.Cursor == Cursors.Default)
                gMapControl.Cursor = Cursors.Cross;
            else if (!anyModeActive && gMapControl.Cursor == Cursors.Cross)
                gMapControl.Cursor = Cursors.Default;
        }

        private bool ValidateCreatingFields()
        {
            string street = creatingLabelsTextBox1.Content.Trim();
            if (string.IsNullOrEmpty(street))
            {
                Logger.ShowWarning("Введите название улицы");
                return false;
            }
            return true;
        }

        private async Task CreateMarkerAtPoint(PointLatLng point)
        {
            try
            {
                // Определяем регион для метки
                Guid regionId = await GetRegionByPoint(point.Lat, point.Lng);

                // Создать объект метки
                var marker = new DataBase.Markers
                {
                    Id = Guid.NewGuid(),
                    Street = creatingLabelsTextBox1.Content,
                    House = creatingLabelsTextBox2.Content,
                    Building = creatingLabelsTextBox3.Content,
                    Apartment = creatingLabelsTextBox4.Content,
                    TypeBuilding = creatingLabelsComboBox1.SelectedItem?.ToString(),
                    Latitude = point.Lat,
                    Longitude = point.Lng,
                    IdRegion = regionId != Guid.Empty ? regionId : Guid.Empty,
                    IdReaders = string.Empty
                };

                // Добавляем в список ожидающих создания
                pendingCreations.Add(marker);

                // Добавить на карту локально
                await Marker.AddMarkerToMap(marker);

                // Деактивируем режим создания
                DeactivateCreatingMode();
                creatingLabelsButton1.Content = CreateButtonText;

                Logger.ShowInfo("Метка создана локально.\nНажмите «Сохранить изменения» для подтверждения");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка создания метки", ex);
                Logger.ShowError("Ошибка создания метки");
            }
        }

        private async Task<Guid> GetRegionByPoint(double lat, double lng)
        {
            // Найти ближайший узел и определить регион
            var nearestNode = await GetNearestNode(lat, lng);
            if (nearestNode == null)
                return Guid.Empty;

            // Проверить, находится ли точка внутри полигона региона
            var regionId = await CheckPointInRegion(nearestNode.IdRegion, lat, lng);
            return regionId;
        }

        private void LoadMarkerToEditFields(DataBase.Markers marker)
        {
            editLabelsTextBox1.Content = marker.Street ?? "";
            editLabelsTextBox2.Content = marker.House ?? "";
            editLabelsTextBox3.Content = marker.Building ?? "";
            editLabelsTextBox4.Content = marker.Apartment ?? "";

            // Устанавливаем тип строения
            if (!string.IsNullOrEmpty(marker.TypeBuilding))
                editLabelsComboBox1.SelectedItem = marker.TypeBuilding;
            else
                editLabelsComboBox1.SelectedIndex = -1;
        }

        private void SaveMarkerChangesLocally()
        {
            if (selectedMarker == null)
                return;

            try
            {
                // Обновляем данные метки локально
                selectedMarker.Street = editLabelsTextBox1.Content.Trim();
                selectedMarker.House = editLabelsTextBox2.Content.Trim();
                selectedMarker.Building = editLabelsTextBox3.Content.Trim();
                selectedMarker.Apartment = editLabelsTextBox4.Content.Trim();
                selectedMarker.TypeBuilding = editLabelsComboBox1.SelectedItem?.ToString();

                // Добавляем в словарь ожидающих изменений
                if (!pendingChanges.ContainsKey(selectedMarker.Id))
                {
                    pendingChanges[selectedMarker.Id] = selectedMarker;
                }
                else
                {
                    pendingChanges[selectedMarker.Id] = selectedMarker;
                }

                Logger.ShowInfo("Изменения сохранены локально.\nНажмите «Сохранить изменения» для подтверждения");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка сохранения", ex);
                Logger.ShowError("Ошибка сохранения");
            }
        }

        private async Task MoveMarkerToPoint(PointLatLng newPoint)
        {
            if (selectedMarker == null)
                return;

            try
            {
                // Обновляем координаты метки локально
                selectedMarker.Latitude = newPoint.Lat;
                selectedMarker.Longitude = newPoint.Lng;

                // Определяем новый регион
                Guid newRegionId = await GetRegionByPoint(newPoint.Lat, newPoint.Lng);
                selectedMarker.IdRegion = newRegionId != Guid.Empty ? newRegionId : Guid.Empty;

                // Добавляем в словарь ожидающих изменений
                if (!pendingChanges.ContainsKey(selectedMarker.Id))
                    pendingChanges[selectedMarker.Id] = selectedMarker;
                else
                    pendingChanges[selectedMarker.Id] = selectedMarker;

                // Деактивируем режим перемещения
                DeactivateMovingMode();
                editLabelsButton2.Content = MoveButtonText;

                Logger.ShowInfo("Метка перемещена локально.\nНажмите «Сохранить изменения» для подтверждения");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка перемещения метки", ex);
                Logger.ShowError("Ошибка перемещения метки");
            }
        }

        private void DeactivateCreatingMode()
        {
            isCreatingMode = false;
            ClearCreatingFields();
        }

        private void DeactivateMovingMode()
        {
            isMovingMode = false;
        }

        private void ClearCreatingFields()
        {
            creatingLabelsTextBox1.Content = string.Empty;
            creatingLabelsTextBox2.Content = string.Empty;
            creatingLabelsTextBox3.Content = string.Empty;
            creatingLabelsTextBox4.Content = string.Empty;
            creatingLabelsComboBox1.SelectedIndex = -1;
        }

        private void ClearEditFields()
        {
            editLabelsTextBox1.Content = string.Empty;
            editLabelsTextBox2.Content = string.Empty;
            editLabelsTextBox3.Content = string.Empty;
            editLabelsTextBox4.Content = string.Empty;
            editLabelsComboBox1.SelectedIndex = -1;
        }

        private async Task<DataBase.Markers> GetMarkerById(Guid id)
        {
            try
            {
                var response = await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == id).Get();
                return response.Models.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private async Task<DataBase.Nodes> GetNearestNode(double lat, double lng)
        {
            try
            {
                var response = await DataBase._client.From<DataBase.Nodes>().Get();

                var nodes = response.Models.ToList();

                if (nodes.Count == 0)
                    return null;

                DataBase.Nodes nearestNode = null;
                double minDistance = double.MaxValue;

                foreach (var node in nodes)
                {
                    double distance = CalculateDistance(lat, lng, node.Latitude, node.Longitude);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestNode = node;
                    }
                }

                return nearestNode;
            }
            catch
            {
                return null;
            }
        }

        private async Task<Guid> CheckPointInRegion(Guid regionId, double lat, double lng)
        {
            try
            {
                // Получаем все узлы этого региона
                var response = await DataBase._client.From<DataBase.Nodes>().Where(x => x.IdRegion == regionId).Get();
                var regionNodes = response.Models.ToList();

                if (regionNodes.Count < 3)
                    return Guid.Empty;

                // Проверяем, находится ли точка внутри полигона
                var polygon = regionNodes.Select(n => new PointLatLng(n.Latitude, n.Longitude)).ToList();
                var point = new PointLatLng(lat, lng);

                return IsPointInPolygon(polygon, point) ? regionId : Guid.Empty;
            }
            catch
            {
                return Guid.Empty;
            }
        }

        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371e3; // радиус Земли в метрах
            double φ1 = lat1 * Math.PI / 180;
            double φ2 = lat2 * Math.PI / 180;
            double Δφ = (lat2 - lat1) * Math.PI / 180;
            double Δλ = (lng2 - lng1) * Math.PI / 180;

            double a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                       Math.Cos(φ1) * Math.Cos(φ2) *
                       Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private bool IsPointInPolygon(List<PointLatLng> polygon, PointLatLng point)
        {
            int nvert = polygon.Count;
            bool inside = false;

            double testx = point.Lng;
            double testy = point.Lat;

            for (int i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                if (((polygon[i].Lat > testy) != (polygon[j].Lat > testy)) && (testx < polygon[i].Lng + (polygon[j].Lng - polygon[i].Lng) * (testy - polygon[i].Lat) / (polygon[j].Lat - polygon[i].Lat + double.Epsilon)))
                    inside = !inside;
            }
            return inside;
        }

        private async void cuiButton1_Click(object sender, EventArgs e)
        {
            await SaveAllChangesToDatabase();
        }

        private async void cuiButton2_Click(object sender, EventArgs e)
        {
            await RevertAllChanges();
        }

        private async Task SaveAllChangesToDatabase()
        {
            try
            {
                // Сохраняем новые метки
                foreach (var marker in pendingCreations)
                    await SaveMarkerToDatabase(marker);

                // Обновляем измененные метки
                foreach (var kvp in pendingChanges)
                    await UpdateMarkerInDatabase(kvp.Value);

                // Удаляем отмеченные метки
                foreach (var markerId in pendingDeletions)
                    await DeleteMarkerFromDatabase(markerId);

                // Очищаем списки ожидающих изменений
                pendingCreations.Clear();
                pendingChanges.Clear();
                pendingDeletions.Clear();

                // Перезагружаем карту
                gMapControl.Overlays.Clear();
                await Map.InitializeMap(gMapControl);
                await Map.LoadMarkers(gMapControl);
                await Map.LoadRegions(gMapControl);

                // Очищаем поля редактирования
                ClearEditFields();
                selectedMarker = null;
                selectedMarkerBackup = null;
                selectedGMapMarker = null;

                Logger.ShowInfo("Все изменения успешно сохранены в базу данных");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка сохранения изменений", ex);
                Logger.ShowError("Ошибка сохранения изменений");
            }
        }

        private async Task RevertAllChanges()
        {
            try
            {
                // Перезагружаем карту
                gMapControl.Overlays.Clear();
                await Map.LoadMarkers(gMapControl);
                await Map.LoadRegions(gMapControl);

                // Очищаем списки ожидающих изменений
                pendingCreations.Clear();
                pendingChanges.Clear();
                pendingDeletions.Clear();

                // Восстанавливаем выбранную метку из резервной копии
                if (selectedMarkerBackup != null)
                {
                    selectedMarker = new DataBase.Markers
                    {
                        Id = selectedMarkerBackup.Id,
                        Street = selectedMarkerBackup.Street,
                        House = selectedMarkerBackup.House,
                        Building = selectedMarkerBackup.Building,
                        Apartment = selectedMarkerBackup.Apartment,
                        TypeBuilding = selectedMarkerBackup.TypeBuilding,
                        Latitude = selectedMarkerBackup.Latitude,
                        Longitude = selectedMarkerBackup.Longitude,
                        IdRegion = selectedMarkerBackup.IdRegion,
                        IdReaders = selectedMarkerBackup.IdReaders
                    };

                    LoadMarkerToEditFields(selectedMarker);
                }
                else
                {
                    ClearEditFields();
                    selectedMarker = null;
                }

                selectedGMapMarker = null;

                Logger.ShowInfo("Все изменения отменены");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка отката изменений", ex);
                Logger.ShowError("Ошибка отката изменений");
            }
        }

        private async Task SaveMarkerToDatabase(DataBase.Markers marker)
        {
            try
            {
                var response = await DataBase._client.From<DataBase.Markers>().Insert(marker);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения метки: {ex.Message}");
            }
        }

        private async Task UpdateMarkerInDatabase(DataBase.Markers marker)
        {
            try
            {
                var response = await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == marker.Id).Update(marker);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка обновления метки: {ex.Message}");
            }
        }

        private async Task DeleteMarkerFromDatabase(Guid markerId)
        {
            try
            {
                await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == markerId).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка удаления метки: {ex.Message}");
            }
        }
    }
}
