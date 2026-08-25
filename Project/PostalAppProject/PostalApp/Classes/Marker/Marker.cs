using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace PostalApp
{
    public static class Marker
    {
        public static List<string> _typeBuildings = new List<string>() { "Частный дом", "Многоквартирный дом", "Здание организаций" };

        public static async Task AddMarkerToMap(DataBase.Markers markerData, bool isSelected = false)
        {
            try
            {
                var point = new PointLatLng(markerData.Latitude, markerData.Longitude);

                // Выбор иконки в зависимости от типа здания
                string buildingType = markerData.TypeBuilding?.ToLower().Trim();

                Bitmap originalBitmap;

                if (!isSelected)
                {
                    if (buildingType.Contains("частный дом") || buildingType.Contains("не указан") || string.IsNullOrEmpty(buildingType))
                        originalBitmap = Properties.Resources.Дом1;
                    else if (buildingType.Contains("post office"))
                        originalBitmap = Properties.Resources.ПочтовыйЯщик1;
                    else if (buildingType.Contains("многоквартирный дом"))
                        originalBitmap = Properties.Resources.Дом2;
                    else
                        originalBitmap = Properties.Resources.Орёл;
                }
                else
                {
                    originalBitmap = Properties.Resources.Маршрут2;
                }

                // Загружаем иконку и масштабируем
                double scale = 0.2;
                int newWidth = (int)(originalBitmap.Width * scale);
                int newHeight = (int)(originalBitmap.Height * scale);
                Bitmap resizedIcon = new Bitmap(originalBitmap, new Size(newWidth, newHeight));

                var marker = new GMarkerGoogle(point, resizedIcon)
                {
                    Tag = markerData.Id,
                    Offset = new Point(-newWidth / 2, -newHeight)
                };
                int count = await SearchCountSubscriptionsInMarker(markerData);

                TooltipMarker(marker, new List<string> { $"Улица: {markerData.Street}", $"Дом: {markerData.House}", $"Тип строения: {markerData.TypeBuilding}", $"Активные подписки: {count}" });

                if (buildingType.Contains("частный дом") || buildingType.Contains("не указан") || string.IsNullOrEmpty(buildingType))
                    Map._homesOverlay.Markers.Add(marker);
                else if (buildingType.Contains("многоквартирный дом"))
                    Map._apartmentsOverlay.Markers.Add(marker);
                else if (buildingType.Contains("многоквартирный дом"))
                    Map._organizationsOverlay.Markers.Add(marker);
                else if (buildingType.Contains("Почтовое отделение"))
                    Map._postOfficeOverlay.Markers.Add(marker);
                else
                    Map._homesOverlay.Markers.Add(marker);

                if (isSelected)
                    SelectedMarkers._points.Add(marker);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при добавлении метки", ex);
                Logger.ShowError("Не удалось загрузить метку с сервера");
            }
        }

        private static async Task<int> SearchCountSubscriptionsInMarker(DataBase.Markers markerData)
        {
            // Парсинг ID читателей из строки
            List<Guid> reasersIds = new List<Guid>();
            string id = string.Empty;

            for (int i = 0; i < markerData.IdReaders.Length; i++)
            {
                if (markerData.IdReaders[i] == ',')
                {
                    if (!string.IsNullOrEmpty(id))
                        reasersIds.Add(Guid.Parse(id));
                    id = string.Empty;
                }
                else
                    id += markerData.IdReaders[i];
            }

            // Добавляем последний ID (если есть)
            if (!string.IsNullOrEmpty(id))
                reasersIds.Add(Guid.Parse(id));

            // Если нет читателей - возвращаем 0
            if (reasersIds.Count == 0)
                return 0;

            // Один запрос вместо N запросов
            var response = await DataBase._client.From<DataBase.Subscriptions>()
                .Filter("id", "IN", reasersIds.ToArray())
                .Get();

            return response.Models.Count;
        }

        private static void TooltipMarker(GMapMarker marker, List<string> markerData)
        {
            marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;

            marker.ToolTipText = "\n";
            foreach (var item in markerData)
                marker.ToolTipText += $"{item}\n";

            marker.ToolTip.Fill = Brushes.White;
            marker.ToolTip.Foreground = Brushes.Black;
            marker.ToolTip.Stroke = Pens.Black;
            marker.ToolTip.TextPadding = new Size(10, 10);
        }

        public static void CreateMarkerBorderRegion(GMapControl gMap, PointLatLng point, DataBase.Nodes node)
        {
            var marker = new GMarkerGoogle(point, GMarkerGoogleType.blue_small)
            {
                Tag = node.Id
            };
            Map._boundsOverlay.Markers.Add(marker);
        }
    }
}
