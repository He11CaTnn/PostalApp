using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Интерфейс
{
    public static class Route
    {
        // Расчет времени
        public static double metreSeconds = 2;
        public static double visitingSeconds = 30;

        // Линия маршрута
        public static Pen lineRoute = new Pen(Color.Red, 3);

        // Переменные для подсчета меток и времени
        public static int selectedMarkersCount = 0;
        public static double routeDistance = 0; // в метрах
        public static int estimatedTimeMinutes = 0; // в минутах

        public static async Task BuildRoute(GMapControl map, List<GMapMarker> selectedMarkers)
        {
            Map._routesOverlay.Clear();

            // Обновляем счетчики
            selectedMarkersCount = selectedMarkers.Count;
            routeDistance = 0;

            List<PointLatLng> pointsLatLngs = new List<PointLatLng>();
            foreach (var item in selectedMarkers)
                pointsLatLngs.Add(item.Position);

            if (selectedMarkers.Count > 2)
            {
                var optimizedRoute = FindRouteWith2Opt(pointsLatLngs);
                routeDistance = CalculateRouteDistance(optimizedRoute, true);
                for (int i = 0; i < optimizedRoute.Count - 1; i++)
                    DrawRouteBetweenPoints(map, optimizedRoute[i], optimizedRoute[i + 1]);
            }
            else if (selectedMarkers.Count == 2)
            {
                routeDistance = GetDistance(pointsLatLngs[0], pointsLatLngs[1]);
                DrawRouteBetweenPoints(map, pointsLatLngs[0], pointsLatLngs[1]);
            }

            // Расчет времени
            double walkingTimeSeconds = routeDistance * metreSeconds;
            double visitTimeSeconds = selectedMarkersCount * visitingSeconds;
            double totalSeconds = walkingTimeSeconds + visitTimeSeconds;
            estimatedTimeMinutes = (int)Math.Ceiling(totalSeconds / 60.0);

            Logger.Info($"Построен маршрут по {selectedMarkers.Count} меткам за {walkingTimeSeconds / 60} минут");
        }

        private static void DrawRouteBetweenPoints(GMapControl map, PointLatLng start, PointLatLng end)
        {
            try
            {
                var route = OpenStreetMapProvider.Instance.GetRoute(start, end, false, false, (int)map.Zoom);

                if (route != null)
                {
                    GMapRoute mapRoute = new GMapRoute(route.Points, $"Маршрут: {start} - {end}")
                    {
                        Stroke = lineRoute
                    };
                    Map._routesOverlay.Routes.Add(mapRoute);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при построении маршрута", ex);
                Logger.ShowError("Ошибка при построении маршрута");
            }
        }

        private static List<PointLatLng> FindRouteWith2Opt(List<PointLatLng> points)
        {
            if (points == null || points.Count < 4)
                return points;

            List<PointLatLng> route = new List<PointLatLng>(points);
            double bestDistance = CalculateRouteDistance(route, false);
            bool improvement = true;
            int maxIterations = 1000;
            int iterations = 0;

            while (improvement && iterations < maxIterations)
            {
                improvement = false;
                iterations++;

                for (int i = 0; i < route.Count - 2; i++)
                {
                    for (int j = i + 2; j < route.Count - 1; j++)
                    {
                        double currentDistance = GetDistance(route[i], route[i + 1]) + GetDistance(route[j], route[j + 1]);
                        double newDistance = GetDistance(route[i], route[j]) + GetDistance(route[i + 1], route[j + 1]);

                        if (newDistance < currentDistance)
                        {
                            ReverseSegment(route, i + 1, j);
                            improvement = true;
                            bestDistance = CalculateRouteDistance(route, false);
                        }
                    }
                }
            }
            return route;
        }

        private static double CalculateRouteDistance(List<PointLatLng> route, bool routeDistance)
        {
            double totalDistance = 0;
            for (int i = 0; i < route.Count - 1; i++)
                totalDistance += GetDistance(route[i], route[i + 1]);
            return totalDistance;
        }

        private static double GetDistance(PointLatLng startPoint, PointLatLng endpoint)
        {
            return OpenStreetMapProvider.Instance.Projection.GetDistance(startPoint, endpoint);
        }

        private static void ReverseSegment(List<PointLatLng> route, int start, int end)
        {
            while (start < end)
            {
                PointLatLng temp = route[start];
                route[start] = route[end];
                route[end] = temp;
                start++;
                end--;
            }
        }

        public static string FormatTime(int totalMinutes)
        {
            if (totalMinutes < 60)
                return $"{totalMinutes} мин";

            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            if (minutes == 0)
                return $"{hours} ч";
            else
                return $"{hours} ч {minutes} мин";
        }
    }
}
