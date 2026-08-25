using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp
{
    public static class Map
    {
        public static PointLatLng startPosition = new PointLatLng(55.7522, 37.6156);
        public static GMapProvider satelliteProvider = GMapProviders.GoogleSatelliteMap;
        public static GMapProvider streetProvider = GMapProviders.OpenCycleMap;
        public static List<DataBase.Markers> _userMarkers = new List<DataBase.Markers>();

        public static GMapOverlay _homesOverlay;
        public static GMapOverlay _apartmentsOverlay;
        public static GMapOverlay _organizationsOverlay;
        public static GMapOverlay _postOfficeOverlay;
        public static GMapOverlay _regionsOverlay;
        public static GMapOverlay _boundsOverlay;
        public static GMapOverlay _routesOverlay;

        public static int _transparencyColor = 70;
        public static int minZoomLevel = 9;
        public static int maxZoomLevel = 18;
        public static int[] autoModMap = new int[2] { 14, 16 };
        public static bool isAutoModeActive = false;

        public static async Task LoadRegions(GMapControl gMap)
        {
            try
            {
                List<DataBase.Regions> regions;

                // Директор видит все, почтальон - только свои
                if (UserData.CurrentUser.Employee.Role == "Директор")
                {
                    var response = await DataBase._client.From<DataBase.Regions>().Get();
                    regions = response.Models.ToList();
                }
                else
                {
                    if (UserData.CurrentUser.RegionIds == null || !UserData.CurrentUser.RegionIds.Any())
                        return;

                    var response = await DataBase._client.From<DataBase.Regions>().Filter("id", "IN", UserData.CurrentUser.RegionIds.ToArray()).Get();
                    regions = response.Models.ToList();
                }

                if (!regions.Any())
                    return;

                foreach (var region in regions)
                {
                    var nodesResponse = await DataBase._client.From<DataBase.Nodes>().Where(n => n.IdRegion == region.Id).Get();
                    var nodes = nodesResponse.Models.OrderBy(n => n.Number).ToList();

                    if (nodes.Count < 3)
                        continue;

                    // Точки полигона (отсортированы по Number)
                    var points = nodes.Select(n => new PointLatLng(n.Latitude, n.Longitude)).ToList();

                    // Цвет из БД - правильная конвертация
                    Color regionColor;
                    try
                    {
                        // Проверяем формат цвета
                        string colorStr = region.Color?.Trim();
                        if (string.IsNullOrEmpty(colorStr))
                            regionColor = Color.Gray;
                        else if (!colorStr.StartsWith("#"))
                            regionColor = ColorTranslator.FromHtml("#" + colorStr);
                        else
                            regionColor = ColorTranslator.FromHtml(colorStr);
                    }
                    catch
                    {
                        regionColor = Color.Gray;
                    }

                    Color fillColor = Color.FromArgb(_transparencyColor, regionColor);

                    // Полигон участка (regionsOverlay) - некликабельный для обычного просмотра
                    var polygon = new GMapPolygon(points, region.Name)
                    {
                        Fill = new SolidBrush(fillColor),
                        Stroke = new Pen(regionColor, 2),
                        IsHitTestVisible = false
                    };
                    _regionsOverlay.Polygons.Add(polygon);

                    // Границы (boundsOverlay) - линии БЕЗ меток
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        int nextIndex = (i + 1) % nodes.Count;
                        var startPoint = new PointLatLng(nodes[i].Latitude, nodes[i].Longitude);
                        var endPoint = new PointLatLng(nodes[nextIndex].Latitude, nodes[nextIndex].Longitude);

                        var borderLine = new GMapPolygon(new List<PointLatLng> { startPoint, endPoint }, $"Border_{region.Id}_{i}")
                        {
                            Stroke = new Pen(regionColor, 3),
                            Fill = Brushes.Transparent,
                            IsHitTestVisible = false
                        };
                        _boundsOverlay.Polygons.Add(borderLine);
                    }

                    Logger.Debug($"Загружен участок {region.Name}, количество узлов {nodes.Count}");
                }

                gMap.Refresh();
                Logger.Info($"Загружено {regions.Count} участков");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки участков", ex);
                Logger.ShowError("Не удалось загрузить участки с сервера");
            }
        }

        public static async Task LoadMarkers(GMapControl gMap)
        {
            try
            {
                // Если пользователь директор - загружаем все маркеры
                if (UserData.CurrentUser.Employee.Role == "Директор")
                {
                    // Получаем все маркеры без фильтрации
                    var markersResponse = await DataBase._client.From<DataBase.Markers>().Get();
                    _userMarkers = markersResponse.Models.ToList();

                    await DisplayInterfaceOnMap(gMap, _userMarkers.ToList());
                }
                else
                {
                    // Если у пользователя нет регионов, выходим
                    if (UserData.CurrentUser.RegionIds == null || !UserData.CurrentUser.RegionIds.Any())
                        return;

                    // Получаем все метки для регионов пользователя
                    // Используем IN для поиска по нескольким IdRegion
                    var markersResponse = await DataBase._client.From<DataBase.Markers>().Filter("IdRegion", "IN", UserData.CurrentUser.RegionIds.ToArray()).Get();
                    _userMarkers = markersResponse.Models.ToList();

                    await DisplayInterfaceOnMap(gMap, _userMarkers.ToList());
                }
                Logger.Info($"Загружено {_userMarkers.Count} меток");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки меток", ex);
                Logger.ShowError("Не удалось загрузить метки с сервера");
            }
        }

        public static async Task DisplayInterfaceOnMap(GMapControl gMap, List<DataBase.Markers> markersData)
        {
            if (!markersData.Any())
                return;

            foreach (var marker in markersData)
                await Marker.AddMarkerToMap(marker);

            gMap.Refresh();
        }

        public static async Task InitializeMap(GMapControl gMap)
        {
            gMap.MapProvider = streetProvider;
            gMap.MinZoom = minZoomLevel;
            gMap.MaxZoom = maxZoomLevel;
            gMap.Zoom = minZoomLevel + (maxZoomLevel - minZoomLevel) / 2;
            gMap.ShowCenter = false;
            gMap.DragButton = MouseButtons.Left;
            gMap.Position = await SetStartPoisitionMap(gMap);

            // Создаем слои
            _homesOverlay = new GMapOverlay("markers");
            gMap.Overlays.Add(_homesOverlay);
            _postOfficeOverlay = new GMapOverlay("pochta");
            gMap.Overlays.Add(_postOfficeOverlay);
            _regionsOverlay = new GMapOverlay("regions");
            gMap.Overlays.Add(_regionsOverlay);
            _boundsOverlay = new GMapOverlay("bounds");
            gMap.Overlays.Add(_boundsOverlay);
            _routesOverlay = new GMapOverlay("routes");
            gMap.Overlays.Add(_routesOverlay);
        }

        private static async Task<PointLatLng> SetStartPoisitionMap(GMapControl gMap)
        {
            var response = await DataBase._client.From<DataBase.Markers>().Where(x => x.TypeBuilding == "Почта").Single();
            if (response != null)
            {
                PointLatLng point = new PointLatLng(response.Latitude, response.Longitude);
                startPosition = point;
                Logger.Debug("Начальная позиция найдена");
                return point;
            }
            else
            {
                Logger.Error("Начальная позиция не найдена");
                return startPosition;
            }
        }

        public static async Task RefreshMap(GMapControl gMap)
        {
            _regionsOverlay.Clear();
            _boundsOverlay.Clear();
            _homesOverlay.Clear();
            _postOfficeOverlay.Clear();
            await LoadMarkers(gMap);
            await LoadRegions(gMap);
            Logger.Info("Карта загружена");
        }

        public static void ApplyAutoZoomSettings(GMapControl gMap, bool check)
        {
            if (!check)
                return;

            double zoom = gMap.Zoom;
            if (zoom >= autoModMap[0])
            {
                // Видны только маркеры и граница
                if (_regionsOverlay != null)
                    _regionsOverlay.IsVisibile = false;
                if (_boundsOverlay != null)
                    _boundsOverlay.IsVisibile = true;
                if (_homesOverlay != null)
                    _homesOverlay.IsVisibile = true;
            }
            else if (zoom >= autoModMap[0] && zoom < autoModMap[1])
            {
                // Видно всё
                if (_regionsOverlay != null)
                    _regionsOverlay.IsVisibile = true;
                if (_boundsOverlay != null)
                    _boundsOverlay.IsVisibile = true;
                if (_homesOverlay != null)
                    _homesOverlay.IsVisibile = true;
            }
            else
            {
                // Видно только границу и регион
                if (_regionsOverlay != null)
                    _regionsOverlay.IsVisibile = true;
                if (_boundsOverlay != null)
                    _boundsOverlay.IsVisibile = true;
                if (_homesOverlay != null)
                    _homesOverlay.IsVisibile = false;
            }
            gMap.Refresh();
        }

        public static async Task LoadBorders(GMapControl gMap)
        {
            try
            {
                _boundsOverlay.Clear();
                _regionsOverlay.Clear();

                // Загружаем регионы и их границы из БД
                var regions = await DataBase._client.From<DataBase.Regions>().Get();
                var nodes = await DataBase._client.From<DataBase.Nodes>().Get();

                foreach (var region in regions.Models)
                {
                    // 1. Цвет региона из строки - улучшенная конвертация
                    Color regionColor;
                    try
                    {
                        string colorStr = region.Color?.Trim();
                        if (string.IsNullOrEmpty(colorStr))
                            regionColor = Color.Red;
                        else if (!colorStr.StartsWith("#"))
                            regionColor = ColorTranslator.FromHtml("#" + colorStr);
                        else
                            regionColor = ColorTranslator.FromHtml(colorStr);
                    }
                    catch
                    {
                        regionColor = Color.Red;
                    }

                    Color fillColor = Color.FromArgb(_transparencyColor, regionColor);

                    // 2. Получаем узлы этого региона (отсортированы по Number)
                    var regionNodesList = nodes.Models.Where(n => n.IdRegion == region.Id).OrderBy(n => n.Number).ToList();

                    if (regionNodesList.Count < 3)
                        continue;

                    // 3. Создаем точки для полигона
                    var regionNodes = regionNodesList.Select(n => new PointLatLng(n.Latitude, n.Longitude)).ToList();

                    // 4. СОЗДАЕМ ПОЛИГОН УЧАСТКА (_regionsOverlay) - КЛИКАБЕЛЬНЫЙ для редактирования
                    var polygon = new GMapPolygon(regionNodes, region.Name)
                    {
                        Fill = new SolidBrush(fillColor),
                        Stroke = new Pen(regionColor, 2),
                        IsHitTestVisible = true, // Кликабельный для выбора
                        Tag = region.Id // ID региона для идентификации
                    };
                    _regionsOverlay.Polygons.Add(polygon);

                    // 5. ВНЕШНЯЯ ГРАНИЦА - закрытая линия
                    var borderLine = new GMapPolygon(regionNodes, $"Border_{region.Id}")
                    {
                        Stroke = new Pen(regionColor, 3) { DashStyle = DashStyle.Solid },
                        Fill = Brushes.Transparent,
                        IsHitTestVisible = false
                    };
                    _boundsOverlay.Polygons.Add(borderLine);

                    // 6. СОЗДАЕМ МЕТКИ ГРАНИЦ (_boundsOverlay) - кликабельные для редактирования
                    for (int i = 0; i < regionNodesList.Count; i++)
                    {
                        var currentNode = regionNodesList[i];
                        Marker.CreateMarkerBorderRegion(gMap, regionNodes[i], currentNode);
                    }

                    Logger.Debug($"Загружен участок для редактирования {region.Name}, количество узлов {regionNodesList.Count}");
                }

                gMap.Overlays.Add(_regionsOverlay);
                gMap.Overlays.Add(_boundsOverlay);
                gMap.Refresh();
                Logger.Info($"Загружено {regions.Models.Count} участков для редактирования");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки участков для редактирования", ex);
                Logger.ShowError("Не удалось загрузить участки для редактирования с сервера");
            }
        }
    }
}
