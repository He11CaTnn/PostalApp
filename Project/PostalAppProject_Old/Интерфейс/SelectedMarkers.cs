using CuoreUI.Controls;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Интерфейс
{
    public static class SelectedMarkers
    {
        public static List<GMapMarker> _points = new List<GMapMarker>();
        public static bool _selectionMode = false;

        public static async Task SelectMarker(GMapMarker marker, cuiButton countSelected)
        {
            Guid id = Guid.Parse(marker.Tag.ToString());
            var response = await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == id).Single();

            if (Map._homesOverlay.Markers.IndexOf(marker) != -1)
            {
                Map._homesOverlay.Markers.Remove(marker);
                await Marker.AddMarkerToMap(response, true);
                Logger.Debug($"Выделение метки {marker.Tag} по координатам {marker.Position.Lat} {marker.Position.Lng}");
            }

            if (countSelected != null)
                countSelected.Content = _points.Count.ToString();
        }

        public static async Task RemoveMarkerSelection(GMapMarker marker, cuiButton countSelected, int oldMarkersCount)
        {
            Guid id = Guid.Parse(marker.Tag.ToString());
            var response = await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == id).Single();

            if (Map._homesOverlay.Markers.IndexOf(marker) != -1)
            {
                Map._homesOverlay.Markers.Remove(marker);
                await Marker.AddMarkerToMap(response);
                Logger.Debug($"Отмена выделения метки {marker.Tag} по координатам {marker.Position.Lat} {marker.Position.Lng}");
            }

            _points.Remove(marker);
            if (countSelected != null)
                countSelected.Content = _points.Count.ToString();

            if (_points.Count == 0)
            {
                _selectionMode = false;
                if (countSelected != null && oldMarkersCount > 0 && countSelected.Content != oldMarkersCount.ToString())
                    countSelected.Content = oldMarkersCount.ToString();
                else if (countSelected != null && oldMarkersCount == 0 && countSelected.Content != "----")
                    countSelected.Content = "----";
            }
        }

        public static async Task ClearAllSelection(GMapControl map, cuiButton countSelected, cuiButton timeRoute, int oldMarkersCount)
        {
            foreach (var marker in _points.ToArray())
                await RemoveMarkerSelection(marker, countSelected, oldMarkersCount);

            timeRoute.Content = Route.FormatTime(Route.estimatedTimeMinutes);
            _points.Clear();
            _selectionMode = false;
            map.Refresh();
            Logger.Debug($"Снятие выделения со всех меток");
        }
    }
}
