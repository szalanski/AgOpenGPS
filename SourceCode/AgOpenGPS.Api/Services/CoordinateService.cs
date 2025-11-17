using System;
using AgOpenGPS.Api.Abstractions;
using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Services
{
    /// <summary>
    /// Service responsible for local plane coordinate transformations.
    /// Manages the local plane origin and provides conversion between WGS84 geographic
    /// coordinates and local plane coordinates (easting/northing).
    /// Thread-safe implementation using lock-based synchronization.
    /// </summary>
    public class CoordinateService : ICoordinateService
    {
        private readonly object _lock = new object();
        private CoordinateTransformer? _transformer;

        /// <summary>
        /// Initialize local plane with field origin.
        /// Must be called before coordinate conversions.
        /// </summary>
        /// <param name="origin">WGS84 position to use as local plane origin</param>
        public void InitializeLocalPlane(Wgs84Position origin)
        {
            lock (_lock)
            {
                _transformer = new CoordinateTransformer(origin);
            }
        }

        /// <summary>
        /// Check if local plane has been initialized.
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                lock (_lock)
                {
                    return _transformer != null;
                }
            }
        }

        /// <summary>
        /// Get current local plane origin (null if not initialized).
        /// </summary>
        public Wgs84Position? Origin
        {
            get
            {
                lock (_lock)
                {
                    return _transformer?.Origin;
                }
            }
        }

        /// <summary>
        /// Convert WGS84 geographic coordinates to local plane coordinates.
        /// </summary>
        /// <param name="wgs84">WGS84 position to convert</param>
        /// <returns>Local plane position (easting/northing in meters)</returns>
        /// <exception cref="InvalidOperationException">Thrown if local plane not initialized</exception>
        public LocalPosition ConvertToLocal(Wgs84Position wgs84)
        {
            lock (_lock)
            {
                if (_transformer == null)
                {
                    throw new InvalidOperationException("Local plane not initialized. Call InitializeLocalPlane first.");
                }

                return _transformer.ToLocal(wgs84);
            }
        }

        /// <summary>
        /// Convert local plane coordinates to WGS84 geographic coordinates.
        /// </summary>
        /// <param name="local">Local plane position to convert</param>
        /// <returns>WGS84 position</returns>
        /// <exception cref="InvalidOperationException">Thrown if local plane not initialized</exception>
        public Wgs84Position ConvertToWgs84(LocalPosition local)
        {
            lock (_lock)
            {
                if (_transformer == null)
                {
                    throw new InvalidOperationException("Local plane not initialized. Call InitializeLocalPlane first.");
                }

                return _transformer.ToWgs84(local);
            }
        }

        /// <summary>
        /// Get local plane information for diagnostics/UI.
        /// Returns null if not initialized.
        /// </summary>
        public LocalPlaneInfo? GetLocalPlaneInfo()
        {
            lock (_lock)
            {
                if (_transformer == null)
                {
                    return null;
                }

                var origin = _transformer.Origin;

                // Calculate conversion factors at the origin
                double metersPerDegreeLat = CalculateMetersPerDegreeLat(origin.Latitude);
                double metersPerDegreeLon = CalculateMetersPerDegreeLon(origin.Latitude);

                return new LocalPlaneInfo(origin, metersPerDegreeLat, metersPerDegreeLon);
            }
        }

        /// <summary>
        /// Update local plane origin (e.g., when loading different field).
        /// </summary>
        /// <param name="newOrigin">New WGS84 position to use as origin</param>
        public void UpdateOrigin(Wgs84Position newOrigin)
        {
            lock (_lock)
            {
                _transformer = new CoordinateTransformer(newOrigin);
            }
        }

        /// <summary>
        /// Set or update the local plane origin.
        /// Creates new transformer regardless of current state.
        /// </summary>
        public void SetOrigin(Wgs84Position origin)
        {
            lock (_lock)
            {
                _transformer = new CoordinateTransformer(origin);
            }
        }

        /// <summary>
        /// Calculates meters per degree of latitude at a given latitude.
        /// Formula from WGS84 ellipsoid model.
        /// </summary>
        private static double CalculateMetersPerDegreeLat(double latitude)
        {
            double latRad = latitude * Math.PI / 180.0;

            return 111132.92
                   - 559.82 * Math.Cos(2.0 * latRad)
                   + 1.175 * Math.Cos(4.0 * latRad)
                   - 0.0023 * Math.Cos(6.0 * latRad);
        }

        /// <summary>
        /// Calculates meters per degree of longitude at a given latitude.
        /// Longitude lines converge at the poles, so this varies with latitude.
        /// Formula from WGS84 ellipsoid model.
        /// </summary>
        private static double CalculateMetersPerDegreeLon(double latitude)
        {
            double latRad = latitude * Math.PI / 180.0;

            return 111412.84 * Math.Cos(latRad)
                   - 93.5 * Math.Cos(3.0 * latRad)
                   + 0.118 * Math.Cos(5.0 * latRad);
        }
    }
}
