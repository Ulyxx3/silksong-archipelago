using System;
using System.Collections.Generic;

namespace SilksongArchipelago.Managers
{
    /// <summary>
    /// Tracks which in-game locations have been checked and coordinates
    /// sending checks to the Archipelago server.
    /// </summary>
    public class LocationManager
    {
        public event Action<long>? OnLocationChecked;

        private readonly HashSet<long> _checkedLocations = new();
        private readonly List<long> _pendingLocations = new();

        /// <summary>
        /// Check a location by its Archipelago ID.
        /// </summary>
        public void CheckLocation(long locationId)
        {
            if (_checkedLocations.Add(locationId))
            {
                SilksongArchipelagoPlugin.Log.LogInfo($"Checked location ID: {locationId}");
                OnLocationChecked?.Invoke(locationId);

                var client = SilksongArchipelagoPlugin.Instance?.ArchipelagoClient;
                if (client != null && client.IsConnected)
                {
                    client.SendLocation(locationId);
                }
                else
                {
                    _pendingLocations.Add(locationId);
                    SilksongArchipelagoPlugin.Log.LogInfo($"Client disconnected. Queued location check: {locationId}");
                }
            }
        }

        /// <summary>
        /// Send all queued locations that occurred while disconnected.
        /// </summary>
        public void FlushPendingLocations()
        {
            var client = SilksongArchipelagoPlugin.Instance?.ArchipelagoClient;
            if (client != null && client.IsConnected && _pendingLocations.Count > 0)
            {
                SilksongArchipelagoPlugin.Log.LogInfo($"Flushing {_pendingLocations.Count} pending location checks to server.");
                client.SendLocations(_pendingLocations);
                _pendingLocations.Clear();
            }
        }

        /// <summary>
        /// Restore checked locations from save data.
        /// </summary>
        public void LoadCheckedLocations(IEnumerable<long> locations)
        {
            foreach (var loc in locations)
            {
                _checkedLocations.Add(loc);
            }
        }

        public bool IsLocationChecked(long locationId) => _checkedLocations.Contains(locationId);

        public IReadOnlyCollection<long> GetAllCheckedLocations() => _checkedLocations;
    }
}
