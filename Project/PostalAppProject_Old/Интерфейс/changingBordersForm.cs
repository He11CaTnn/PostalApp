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
    public partial class changingBordersForm : Form
    {
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();

        private bool gmapStorage1;
        private bool gmapStorage2;
        private bool gmapStorage3;
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private Guid idEmployee;
        private Guid currentRegionId;
        private List<DataBase.Employees> employees;

        private bool isCreateMode = false;
        private bool isDeleteMode = false;
        private bool isMoveMode = false;
        private bool isMoveMarkerSelected = false;
        private GMapMarker selectedMarkerForMove = null;

        private string CreateButtonText;
        private string MoveButtonText;
        private string DeleteButtonText;
        private string StopButtonText = "Прекратить";

        // Локальное кэширование изменений
        private List<DataBase.Regions> pendingRegionCreations = new List<DataBase.Regions>();
        private Dictionary<Guid, DataBase.Regions> pendingRegionChanges = new Dictionary<Guid, DataBase.Regions>();
        private HashSet<Guid> pendingRegionDeletions = new HashSet<Guid>();

        private List<DataBase.Nodes> pendingNodeCreations = new List<DataBase.Nodes>();
        private Dictionary<Guid, DataBase.Nodes> pendingNodeChanges = new Dictionary<Guid, DataBase.Nodes>();
        private HashSet<Guid> pendingNodeDeletions = new HashSet<Guid>();

        // Резервные копии для отката
        private DataBase.Regions selectedRegionBackup = null;

        public changingBordersForm()
        {
            InitializeComponent();
            applyRadius();
            creatingBorderTimer.Start();
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
            Logger.Info("Выход с формы редактирования участков на форму директора");
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

        private void creatingBorderTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (gmapStorage1)
            {
                diff = creatingBorderPanel.Height - creatingBorderPanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);

                creatingBorderPanel.Height -= step;
                editLabelPanel.Top -= step;
                editBorderPanel.Top -= step;
                if (creatingBorderPanel.Height <= creatingBorderPanel.MinimumSize.Height)
                {
                    creatingBorderPanel.Height = creatingBorderPanel.MinimumSize.Height;
                    gmapStorage1 = false;
                    creatingBorderTimer.Stop();
                }
            }
            else
            {
                diff = creatingBorderPanel.MaximumSize.Height - creatingBorderPanel.Height;
                step = Math.Max(2, diff / 5);
                creatingBorderPanel.Height += step;
                editLabelPanel.Top += step;
                editBorderPanel.Top += step;
                if (creatingBorderPanel.Height >= creatingBorderPanel.MaximumSize.Height)
                {
                    creatingBorderPanel.Height = creatingBorderPanel.MaximumSize.Height;
                    gmapStorage1 = true;
                    creatingBorderTimer.Stop();
                }
            }
        }

        private void editLabelTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (gmapStorage2)
            {
                diff = editLabelPanel.Height - editLabelPanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);

                editLabelPanel.Height -= step;
                editBorderPanel.Top -= step;

                if (editLabelPanel.Height <= editLabelPanel.MinimumSize.Height)
                {
                    editLabelPanel.Height = editLabelPanel.MinimumSize.Height;
                    gmapStorage2 = false;
                    editLabelTimer.Stop();
                }
            }
            else
            {
                diff = editLabelPanel.MaximumSize.Height - editLabelPanel.Height;
                step = Math.Max(2, diff / 5);
                editLabelPanel.Height += step;
                editBorderPanel.Top += step;
                if (editLabelPanel.Height >= editLabelPanel.MaximumSize.Height)
                {
                    editLabelPanel.Height = editLabelPanel.MaximumSize.Height;
                    gmapStorage2 = true;
                    editLabelTimer.Stop();
                }
            }
        }

        private void editBorderTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (gmapStorage3)
            {
                diff = editBorderPanel.Height - editBorderPanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                editBorderPanel.Height -= step;
                if (editBorderPanel.Height <= editBorderPanel.MinimumSize.Height)
                {
                    editBorderPanel.Height = editBorderPanel.MinimumSize.Height;
                    gmapStorage3 = false;
                    editBorderTimer.Stop();
                }
            }
            else
            {
                diff = editBorderPanel.MaximumSize.Height - editBorderPanel.Height;
                step = Math.Max(2, diff / 5);
                editBorderPanel.Height += step;
                if (editBorderPanel.Height >= editBorderPanel.MaximumSize.Height)
                {
                    editBorderPanel.Height = editBorderPanel.MaximumSize.Height;
                    gmapStorage3 = true;
                    editBorderTimer.Stop();
                }
            }
        }

        private void creatingBorderPanel1_Click(object sender, EventArgs e)
        {
            creatingBorderTimer.Start();
        }

        private void editLabelPanel1_Click(object sender, EventArgs e)
        {
            editLabelTimer.Start();
        }

        private void editBorderPanel1_Click(object sender, EventArgs e)
        {
            editBorderTimer.Start();
        }

        private void gMapButton1_1_Click(object sender, EventArgs e)
        {
            gMapControl.Zoom++;
        }

        private void gMapButton1_2_Click(object sender, EventArgs e)
        {
            gMapControl.Zoom--;
        }

        private void gMapButton1_3_Click(object sender, EventArgs e)
        {
            gMapControl.Position = Map.startPosition;
        }

        private void creatingBorderButton1_Click(object sender, EventArgs e)
        {
            try
            {
                // Проверяем обязательные поля
                if (string.IsNullOrWhiteSpace(creatingBorderTextBox1.Content))
                {
                    Logger.ShowWarning("Введите название участка");
                    return;
                }

                if (idEmployee == Guid.Empty)
                {
                    Logger.ShowWarning("Выберите почтальона");
                    return;
                }

                // Создаем новую запись локально
                var region = new DataBase.Regions
                {
                    Id = Guid.NewGuid(),
                    Name = creatingBorderTextBox1.Content,
                    Color = ColorTranslator.ToHtml(creatingBorderColorWheel.Color).ToUpper(),
                    IdEmployee = idEmployee
                };

                // Добавляем в список ожидающих создания
                pendingRegionCreations.Add(region);

                // Добавляем в ComboBox для выбора
                editEmployeeComboBox1.AddItem(creatingBorderTextBox1.Content);

                // Уведомление об успехе
                Logger.ShowInfo("Участок создан локально.\nНажмите «Сохранить изменения» для подтверждения");
                Logger.Info($"Участок {region.Name} создан локально");

                // Очищаем форму
                creatingBorderTextBox1.Content = "";
                creatingBorderColorWheel.Color = Color.White;
                creatingBorderComboBox2.SelectedIndex = -1;
                idEmployee = Guid.Empty;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при создании участка", ex);
                Logger.ShowError("Ошибка при создании участка");
            }
        }

        private async void changingBordersForm_Load(object sender, EventArgs e)
        {
            CreateButtonText = editLabelButton2.Content;
            MoveButtonText = editLabelButton1.Content;
            DeleteButtonText = editLabelButton3.Content;

            await Map.InitializeMap(gMapControl);
            await Map.LoadBorders(gMapControl);
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                // Загружаем всех сотрудников
                var allEmployees = await DataBase._client.From<DataBase.Employees>().Get();

                // Фильтруем только Почтальонов
                employees = allEmployees.Models.Where(x => x.Role != null && x.Role.Trim().Equals("Почтальон", StringComparison.OrdinalIgnoreCase)).ToList();

                // Заполняем ComboBox ФИО почтальонов
                creatingBorderComboBox2.Items = new string[0];
                editBorderComboBox1.Items = new string[0];
                foreach (var emp in employees)
                {
                    creatingBorderComboBox2.AddItem(emp.FIO);
                    editBorderComboBox1.AddItem(emp.FIO);
                }
                Logger.Debug("Почтальоны загружены");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки почтальонов", ex);
                Logger.ShowError("Ошибка загрузки почтальонов");
            }

            try
            {
                var regions = await DataBase._client.From<DataBase.Regions>().Get();

                // Заполняем ComboBox участков
                editEmployeeComboBox1.Items = new string[0];
                foreach (var region in regions.Models)
                    editEmployeeComboBox1.AddItem(region.Name);
                Logger.Debug("Участки загружены");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки участков", ex);
                Logger.ShowError("Ошибка загрузки участков");
            }
        }

        private void creatingBorderComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (creatingBorderComboBox2.SelectedItem == null)
                return;

            var selectedFIO = creatingBorderComboBox2.SelectedItem.ToString();

            // Находим сотрудника по ФИО и устанавливаем idEmployee
            var selectedEmployee = employees.FirstOrDefault(emp => emp.FIO == selectedFIO);

            if (selectedEmployee != null)
            {
                idEmployee = selectedEmployee.Id;
            }
        }

        private async void gMapControl_OnPolygonClick(GMapPolygon item, MouseEventArgs e)
        {
            try
            {
                Guid regionId;

                if (item.Tag is Guid tagId)
                    regionId = tagId;
                else if (!Guid.TryParse(item.Name, out regionId))
                    return;

                var region = await DataBase._client.From<DataBase.Regions>().Where(x => x.Id == regionId).Single();

                // Создаем резервную копию для отката
                selectedRegionBackup = new DataBase.Regions
                {
                    Id = region.Id,
                    Name = region.Name,
                    Color = region.Color,
                    IdEmployee = region.IdEmployee
                };

                currentRegionId = region.Id;
                editBorderTextBox1.Content = region.Name;
                editEmployeeComboBox1.SelectedItem = region.Name;

                var employee = await DataBase._client.From<DataBase.Employees>().Where(x => x.Id == region.IdEmployee).Single();

                editBorderComboBox1.SelectedItem = employee.FIO;
                idEmployee = employee.Id;

                editBorderColorWheel.Color = ColorTranslator.FromHtml(region.Color);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки участка", ex);
                Logger.ShowError("Ошибка загрузки участка");
            }
        }

        private void editBorderButton1_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentRegionId == Guid.Empty)
                {
                    Logger.ShowWarning("Выберите участок на карте");
                    return;
                }

                if (string.IsNullOrWhiteSpace(editBorderTextBox1.Content))
                {
                    Logger.ShowWarning("Введите название участка");
                    return;
                }

                if (idEmployee == Guid.Empty)
                {
                    Logger.ShowWarning("Выберите почтальона");
                    return;
                }

                // Создаем обновленный объект региона
                var updatedRegion = new DataBase.Regions
                {
                    Id = currentRegionId,
                    Name = editBorderTextBox1.Content,
                    Color = ColorTranslator.ToHtml(editBorderColorWheel.Color).ToUpper(),
                    IdEmployee = idEmployee
                };

                // Добавляем в словарь ожидающих изменений
                pendingRegionChanges[currentRegionId] = updatedRegion;

                Logger.ShowInfo("Участок обновлен локально.\nНажмите «Сохранить изменения» для подтверждения");
                Logger.Info($"Участок {updatedRegion.Name} обновлён локально");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка редактирования участка", ex);
                Logger.ShowError("Ошибка редактирования участка");
            }
        }

        private void editBorderButton2_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentRegionId == Guid.Empty)
                {
                    Logger.ShowWarning("Выберите участок для удаления");
                    return;
                }

                var result = Logger.ShowYesNo("Удалить участок и все его границы?");
                if (result != DialogResult.Yes)
                    return;

                // Отмечаем регион для удаления
                pendingRegionDeletions.Add(currentRegionId);

                // Удаляем полигон с карты локально
                string nameRegion = string.Empty;
                var overlays = gMapControl.Overlays.ToList();
                foreach (var overlay in overlays)
                {
                    var polygonsToRemove = overlay.Polygons.Where(p =>
                    {
                        if (p.Tag is Guid tagId)
                            return tagId == currentRegionId;
                        if (Guid.TryParse(p.Name, out Guid nameId))
                        {
                            if (nameId == currentRegionId)
                            {
                                nameRegion = p.Name;
                                return nameId == currentRegionId;
                            }
                        }
                        return false;
                    }).ToList();

                    foreach (var polygon in polygonsToRemove)
                        overlay.Polygons.Remove(polygon);

                    var markersToRemove = overlay.Markers.Where(m => m.Tag is Guid nodeId && pendingNodeDeletions.Contains(nodeId)).ToList();
                    foreach (var marker in markersToRemove)
                        overlay.Markers.Remove(marker);
                }

                Logger.ShowInfo("Участок отмечен для удаления.\nНажмите «Сохранить изменения» для подтверждения");
                Logger.Info($"Участок {nameRegion} удалён локально");

                // Очистка UI
                currentRegionId = Guid.Empty;
                editBorderTextBox1.Content = "";
                editBorderComboBox1.SelectedIndex = -1;
                editEmployeeComboBox1.SelectedIndex = -1;
                editBorderColorWheel.Color = Color.White;
                idEmployee = Guid.Empty;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка удаления участка", ex);
                Logger.ShowError("Ошибка удаления участка");
            }
        }

        private async void editEmployeeComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedName = editEmployeeComboBox1.SelectedItem;

            if (!string.IsNullOrEmpty(selectedName))
            {
                try
                {
                    var regions = await DataBase._client.From<DataBase.Regions>().Get();

                    // Получаем регион с выбранным названием
                    var region = regions.Models.FirstOrDefault(x => x.Name != null && x.Name.Trim().Equals(selectedName, StringComparison.OrdinalIgnoreCase));

                    if (region != null && (currentRegionId == Guid.Empty || currentRegionId != region.Id))
                    {
                        currentRegionId = region.Id;
                        editBorderTextBox1.Content = region.Name;
                        editBorderColorWheel.Color = ColorTranslator.FromHtml(region.Color);

                        // Загружаем сотрудника
                        var employee = await DataBase._client.From<DataBase.Employees>().Where(x => x.Id == region.IdEmployee).Single();
                        editBorderComboBox1.SelectedItem = employee.FIO;
                        idEmployee = employee.Id;
                    }
                }
                catch
                {
                    currentRegionId = Guid.Empty;
                }
            }
        }

        private void editLabelButton2_Click(object sender, EventArgs e)
        {
            if (currentRegionId == Guid.Empty)
            {
                Logger.ShowWarning("Выберите участок для редактирования границ");
                return;
            }

            isCreateMode = !isCreateMode;

            if (isCreateMode)
            {
                DisableOtherModes(2);
                editLabelButton2.Content = StopButtonText;
            }
            else
                editLabelButton2.Content = CreateButtonText;
        }

        private void editLabelButton1_Click(object sender, EventArgs e)
        {
            if (currentRegionId == Guid.Empty)
            {
                Logger.ShowWarning("Выберите участок для редактирования границ");
                return;
            }

            if (!isMoveMode)
            {
                isMoveMode = true;
                isMoveMarkerSelected = false;
                selectedMarkerForMove = null;
                DisableOtherModes(1);
                editLabelButton1.Content = StopButtonText;
            }
            else
            {
                isMoveMode = false;
                isMoveMarkerSelected = false;
                selectedMarkerForMove = null;
                editLabelButton1.Content = MoveButtonText;
                gMapControl.Cursor = Cursors.Default;
            }
        }

        private void editLabelButton3_Click(object sender, EventArgs e)
        {
            if (currentRegionId == Guid.Empty)
            {
                Logger.ShowWarning("Выберите участок для редактирования границ");
                return;
            }

            isDeleteMode = !isDeleteMode;

            if (isDeleteMode)
            {
                DisableOtherModes(3);
                editLabelButton3.Content = StopButtonText;
            }
            else
                editLabelButton3.Content = DeleteButtonText;
        }

        private void DisableOtherModes(int activeButton)
        {
            if (activeButton != 1)
            {
                isMoveMode = false;
                isMoveMarkerSelected = false;
                selectedMarkerForMove = null;
                editLabelButton1.Content = MoveButtonText;
            }

            if (activeButton != 2)
            {
                isCreateMode = false;
                editLabelButton2.Content = CreateButtonText;
            }

            if (activeButton != 3)
            {
                isDeleteMode = false;
                editLabelButton3.Content = DeleteButtonText;
            }
        }

        private async void gMapControl_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            if (isDeleteMode)
                await DeleteMarkerLocally(item);
            else if (isMoveMode && !isMoveMarkerSelected)
            {
                selectedMarkerForMove = item;
                isMoveMarkerSelected = true;
                gMapControl.Cursor = Cursors.Cross;
            }
        }

        private async void gMapControl_OnMapClick(PointLatLng pointClick, MouseEventArgs e)
        {
            if (isCreateMode)
                CreateMarkerLocally(pointClick);
            else if (isMoveMode && isMoveMarkerSelected && selectedMarkerForMove != null)
            {
                await MoveMarkerLocally(selectedMarkerForMove, pointClick);
                isMoveMarkerSelected = false;
                selectedMarkerForMove = null;
                gMapControl.Cursor = Cursors.Default;
            }
        }

        private void gMapControl_MouseMove(object sender, MouseEventArgs e)
        {
            if ((isCreateMode || isDeleteMode) && gMapControl.Cursor != Cursors.Cross)
                gMapControl.Cursor = Cursors.Cross;
            else if (isMoveMode && isMoveMarkerSelected && gMapControl.Cursor != Cursors.Cross)
                gMapControl.Cursor = Cursors.Cross;
            else if (!isCreateMode && !isDeleteMode && !isMoveMarkerSelected && gMapControl.Cursor != Cursors.Default)
                gMapControl.Cursor = Cursors.Default;
        }

        private async void CreateMarkerLocally(PointLatLng point)
        {
            try
            {
                // Получаем текущие узлы региона
                var region = await DataBase._client.From<DataBase.Regions>().Where(n => n.Id == currentRegionId).Single();
                var nodesResponse = await DataBase._client.From<DataBase.Nodes>().Where(n => n.IdRegion == currentRegionId).Get();
                var existingNodes = nodesResponse.Models.ToList();

                // Добавляем новые узлы из pendingNodeCreations
                existingNodes.AddRange(pendingNodeCreations.Where(n => n.IdRegion == currentRegionId));

                // Применяем изменения из pendingNodeChanges
                for (int i = 0; i < existingNodes.Count; i++)
                {
                    if (pendingNodeChanges.ContainsKey(existingNodes[i].Id))
                        existingNodes[i] = pendingNodeChanges[existingNodes[i].Id];
                }

                // Удаляем узлы из pendingNodeDeletions
                existingNodes = existingNodes.Where(n => !pendingNodeDeletions.Contains(n.Id)).ToList();

                // Определяем номер для новой метки
                int newNumber = existingNodes.Count > 0 ? existingNodes.Max(n => n.Number) + 1 : 1;

                var newNode = new DataBase.Nodes
                {
                    Id = Guid.NewGuid(),
                    Latitude = point.Lat,
                    Longitude = point.Lng,
                    IdRegion = currentRegionId,
                    Number = newNumber
                };

                // Добавляем в список ожидающих создания
                pendingNodeCreations.Add(newNode);

                // Если включена автоматизация границ, пересортируем метки
                if (checkBox1.Checked)
                {
                    existingNodes.Add(newNode);
                    ReorderNodesByConvexHull(existingNodes);
                }

                // Добавляем маркер на карту локально
                Marker.CreateMarkerBorderRegion(gMapControl, point, newNode);

                Logger.Info($"Узел участка {region.Name} создан локально");

                // Перерисовываем границы участка
                await RefreshRegionBordersLocally(currentRegionId);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка создания узла", ex);
                Logger.ShowError("Ошибка создания узла");
            }
        }

        private async Task DeleteMarkerLocally(GMapMarker marker)
        {
            try
            {
                if (marker.Tag is Guid nodeId)
                {
                    // Получаем текущие узлы региона
                    var region = await DataBase._client.From<DataBase.Regions>().Where(n => n.Id == currentRegionId).Single();
                    var nodesResponse = await DataBase._client.From<DataBase.Nodes>().Where(n => n.IdRegion == currentRegionId).Get();
                    var existingNodes = nodesResponse.Models.ToList();

                    // Добавляем новые узлы из pendingNodeCreations
                    existingNodes.AddRange(pendingNodeCreations.Where(n => n.IdRegion == currentRegionId));

                    // Применяем изменения из pendingNodeChanges
                    for (int i = 0; i < existingNodes.Count; i++)
                    {
                        if (pendingNodeChanges.ContainsKey(existingNodes[i].Id))
                            existingNodes[i] = pendingNodeChanges[existingNodes[i].Id];
                    }

                    // Находим удаляемый узел
                    var deletedNode = existingNodes.FirstOrDefault(n => n.Id == nodeId);
                    if (deletedNode == null)
                        return;

                    int deletedNumber = deletedNode.Number;

                    // Отмечаем узел для удаления
                    pendingNodeDeletions.Add(nodeId);

                    // Удаляем маркер с карты
                    marker.Overlay.Markers.Remove(marker);

                    // Сдвигаем номера всех меток после удаленной
                    foreach (var node in existingNodes.Where(n => n.Number > deletedNumber && n.Id != nodeId))
                    {
                        var updatedNode = new DataBase.Nodes
                        {
                            Id = node.Id,
                            Latitude = node.Latitude,
                            Longitude = node.Longitude,
                            IdRegion = node.IdRegion,
                            Number = node.Number - 1
                        };
                        pendingNodeChanges[node.Id] = updatedNode;
                    }

                    Logger.Info($"Узел участка {region.Name} удален локально");

                    // Перерисовываем границы участка
                    await RefreshRegionBordersLocally(currentRegionId);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка удаления узла", ex);
                Logger.ShowError("Ошибка удаления узла");
            }
        }

        private async Task MoveMarkerLocally(GMapMarker marker, PointLatLng newPoint)
        {
            try
            {
                if (marker.Tag is Guid nodeId)
                {
                    // Получаем текущие узлы региона
                    var region = await DataBase._client.From<DataBase.Regions>().Where(n => n.Id == currentRegionId).Single();
                    var nodesResponse = await DataBase._client.From<DataBase.Nodes>().Where(n => n.IdRegion == currentRegionId).Get();
                    var existingNodes = nodesResponse.Models.ToList();

                    // Добавляем новые узлы из pendingNodeCreations
                    existingNodes.AddRange(pendingNodeCreations.Where(n => n.IdRegion == currentRegionId));

                    // Применяем изменения из pendingNodeChanges
                    for (int i = 0; i < existingNodes.Count; i++)
                    {
                        if (pendingNodeChanges.ContainsKey(existingNodes[i].Id))
                            existingNodes[i] = pendingNodeChanges[existingNodes[i].Id];
                    }

                    // Удаляем узлы из pendingNodeDeletions
                    existingNodes = existingNodes.Where(n => !pendingNodeDeletions.Contains(n.Id)).ToList();

                    // Находим перемещаемый узел
                    var movedNode = existingNodes.FirstOrDefault(n => n.Id == nodeId);
                    if (movedNode == null)
                        return;

                    // Создаем обновленный узел
                    var updatedNode = new DataBase.Nodes
                    {
                        Id = nodeId,
                        Latitude = newPoint.Lat,
                        Longitude = newPoint.Lng,
                        IdRegion = currentRegionId,
                        Number = movedNode.Number
                    };

                    // Добавляем в словарь ожидающих изменений
                    pendingNodeChanges[nodeId] = updatedNode;

                    // Обновляем позицию маркера на карте
                    marker.Position = newPoint;

                    // Если включена автоматизация границ, пересортируем метки
                    if (checkBox1.Checked)
                    {
                        // Обновляем узел в списке
                        for (int i = 0; i < existingNodes.Count; i++)
                        {
                            if (existingNodes[i].Id == nodeId)
                            {
                                existingNodes[i] = updatedNode;
                                break;
                            }
                        }
                        ReorderNodesByConvexHull(existingNodes);
                    }

                    Logger.Info($"Узел участка {region.Name} перемещен локально");

                    // Перерисовываем границы участка
                    await RefreshRegionBordersLocally(currentRegionId);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка перемещения узла", ex);
                Logger.ShowError("Ошибка перемещения узла");
            }
        }

        private async Task RefreshRegionBordersLocally(Guid regionId)
        {
            try
            {
                // Получаем все узлы для данного региона из БД
                var nodesResponse = await DataBase._client.From<DataBase.Nodes>().Where(n => n.IdRegion == regionId).Get();
                var nodes = nodesResponse.Models.ToList();

                // Добавляем новые узлы из pendingNodeCreations
                nodes.AddRange(pendingNodeCreations.Where(n => n.IdRegion == regionId));

                // Применяем изменения из pendingNodeChanges
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (pendingNodeChanges.ContainsKey(nodes[i].Id))
                        nodes[i] = pendingNodeChanges[nodes[i].Id];
                }

                // Удаляем узлы из pendingNodeDeletions
                nodes = nodes.Where(n => !pendingNodeDeletions.Contains(n.Id)).ToList();

                if (nodes.Count < 3)
                    return;

                // Сортируем узлы по Number
                nodes = nodes.OrderBy(n => n.Number).ToList();

                // Удаляем старый полигон
                var overlays = gMapControl.Overlays.ToList();
                foreach (var overlay in overlays)
                {
                    var polygonsToRemove = overlay.Polygons.Where(p =>
                    {
                        if (p.Tag is Guid tagId)
                            return tagId == regionId;
                        if (Guid.TryParse(p.Name, out Guid nameId))
                            return nameId == regionId;
                        return false;
                    }).ToList();

                    foreach (var poly in polygonsToRemove)
                        overlay.Polygons.Remove(poly);
                }

                // Получаем информацию о регионе
                var region = await DataBase._client.From<DataBase.Regions>().Where(r => r.Id == regionId).Single();

                // Применяем изменения из pendingRegionChanges
                if (pendingRegionChanges.ContainsKey(regionId))
                    region = pendingRegionChanges[regionId];

                // Создаем новый полигон
                var points = nodes.Select(n => new PointLatLng(n.Latitude, n.Longitude)).ToList();
                var newPolygon = new GMapPolygon(points, region.Name)
                {
                    Fill = new SolidBrush(Color.FromArgb(50, ColorTranslator.FromHtml(region.Color))),
                    Stroke = new Pen(ColorTranslator.FromHtml(region.Color), 2),
                    Tag = regionId
                };

                Map._boundsOverlay.Polygons.Add(newPolygon);
                gMapControl.Refresh();
                Logger.Info("Границы участков обновлены");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка обновления границ", ex);
                Logger.ShowError("Ошибка обновления границ");
            }
        }
        
        private void ReorderNodesByConvexHull(List<DataBase.Nodes> nodes)
        {
            if (nodes.Count < 3)
                return;

            try
            {
                // Находим центр масс всех точек
                double centerLat = nodes.Average(n => n.Latitude);
                double centerLng = nodes.Average(n => n.Longitude);

                // Сортируем точки по углу относительно центра (против часовой стрелки)
                var sortedNodes = nodes.OrderBy(n =>
                {
                    double angle = Math.Atan2(n.Latitude - centerLat, n.Longitude - centerLng);
                    return angle;
                }).ToList();

                // Присваиваем новые номера
                for (int i = 0; i < sortedNodes.Count; i++)
                {
                    var node = sortedNodes[i];
                    var updatedNode = new DataBase.Nodes
                    {
                        Id = node.Id,
                        Latitude = node.Latitude,
                        Longitude = node.Longitude,
                        IdRegion = node.IdRegion,
                        Number = i + 1
                    };

                    // Обновляем в pendingNodeChanges или pendingNodeCreations
                    if (pendingNodeCreations.Any(n => n.Id == node.Id))
                    {
                        var index = pendingNodeCreations.FindIndex(n => n.Id == node.Id);
                        if (index >= 0)
                            pendingNodeCreations[index] = updatedNode;
                    }
                    else
                    {
                        pendingNodeChanges[node.Id] = updatedNode;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка автоматической сортировки меток", ex);
                Logger.ShowError("Ошибка автоматической сортировки меток");
            }
        }

        private async void cuiButton1_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Сохраняем новые регионы
                foreach (var region in pendingRegionCreations)
                    await DataBase._client.From<DataBase.Regions>().Insert(region);

                // 2. Обновляем измененные регионы
                foreach (var kvp in pendingRegionChanges)
                {
                    var region = kvp.Value;
                    await DataBase._client.From<DataBase.Regions>().Where(x => x.Id == region.Id).Set(x => x.Name, region.Name).Set(x => x.Color, region.Color).Set(x => x.IdEmployee, region.IdEmployee).Update();
                }

                // 3. Удаляем отмеченные регионы и их узлы
                foreach (var regionId in pendingRegionDeletions)
                {
                    // Удаляем все узлы региона
                    await DataBase._client.From<DataBase.Nodes>().Where(n => n.IdRegion == regionId).Delete();
                    // Удаляем сам регион
                    await DataBase._client.From<DataBase.Regions>().Where(r => r.Id == regionId).Delete();
                }

                // 4. Сохраняем новые узлы
                foreach (var node in pendingNodeCreations)
                {
                    await DataBase._client.From<DataBase.Nodes>().Insert(node);
                }

                // 5. Обновляем измененные узлы
                foreach (var kvp in pendingNodeChanges)
                {
                    var node = kvp.Value;
                    await DataBase._client.From<DataBase.Nodes>()
                        .Where(n => n.Id == node.Id)
                        .Set(n => n.Latitude, node.Latitude)
                        .Set(n => n.Longitude, node.Longitude)
                        .Set(n => n.Number, node.Number)
                        .Update();
                }

                // 6. Удаляем отмеченные узлы
                foreach (var nodeId in pendingNodeDeletions)
                    await DataBase._client.From<DataBase.Nodes>().Where(n => n.Id == nodeId).Delete();

                // Очищаем списки ожидающих изменений
                pendingRegionCreations.Clear();
                pendingRegionChanges.Clear();
                pendingRegionDeletions.Clear();
                pendingNodeCreations.Clear();
                pendingNodeChanges.Clear();
                pendingNodeDeletions.Clear();

                // Перезагружаем карту
                gMapControl.Overlays.Clear();
                await Map.InitializeMap(gMapControl);
                await Map.LoadBorders(gMapControl);

                // Очищаем UI
                currentRegionId = Guid.Empty;
                selectedRegionBackup = null;
                editBorderTextBox1.Content = "";
                editBorderComboBox1.SelectedIndex = -1;
                editEmployeeComboBox1.SelectedIndex = -1;
                editBorderColorWheel.Color = Color.White;
                idEmployee = Guid.Empty;

                Logger.Info("Все изменения успешно сохранены в базу данных");
                Logger.ShowInfo("Все изменения успешно сохранены в базу данных");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка сохранения изменений", ex);
                Logger.ShowError("Ошибка сохранения изменений");
            }
        }

        private async void cuiButton2_Click(object sender, EventArgs e)
        {
            try
            {
                // Перезагружаем карту с исходными данными
                gMapControl.Overlays.Clear();
                await Map.LoadBorders(gMapControl);

                // Очищаем списки ожидающих изменений
                pendingRegionCreations.Clear();
                pendingRegionChanges.Clear();
                pendingRegionDeletions.Clear();
                pendingNodeCreations.Clear();
                pendingNodeChanges.Clear();
                pendingNodeDeletions.Clear();

                // Восстанавливаем выбранный регион из резервной копии
                if (selectedRegionBackup != null)
                {
                    currentRegionId = selectedRegionBackup.Id;
                    editBorderTextBox1.Content = selectedRegionBackup.Name;
                    editBorderColorWheel.Color = ColorTranslator.FromHtml(selectedRegionBackup.Color);
                    idEmployee = selectedRegionBackup.IdEmployee;

                    // Находим сотрудника
                    var employee = employees.FirstOrDefault(emp => emp.Id == idEmployee);
                    if (employee != null)
                    {
                        editBorderComboBox1.SelectedItem = employee.FIO;
                    }
                }
                else
                {
                    currentRegionId = Guid.Empty;
                    editBorderTextBox1.Content = "";
                    editBorderComboBox1.SelectedIndex = -1;
                    editEmployeeComboBox1.SelectedIndex = -1;
                    editBorderColorWheel.Color = Color.White;
                    idEmployee = Guid.Empty;
                }

                Logger.Info("Все изменения успешно отменены");
                Logger.ShowInfo("Все изменения успешно отменены");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка отката изменений", ex);
                Logger.ShowError("Ошибка отката изменений");
            }
        }
    }
}
