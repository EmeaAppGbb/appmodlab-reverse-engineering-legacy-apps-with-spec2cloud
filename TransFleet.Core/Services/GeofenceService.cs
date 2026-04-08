using System;
using System.Collections.Generic;
using System.Linq;
using TransFleet.Data;
using TransFleet.Data.Entities;

namespace TransFleet.Core.Services
{
    public interface IGeofenceService
    {
        Geofence GetGeofenceById(int geofenceId);
        IEnumerable<Geofence> GetGeofencesByFleet(int fleetId);
        void CreateGeofence(Geofence geofence);
        void UpdateGeofence(Geofence geofence);
        void DeleteGeofence(int geofenceId);
        bool IsVehicleInGeofence(int vehicleId, int geofenceId);
        IEnumerable<GeofenceAlert> CheckGeofenceViolations(int fleetId);
    }

    public class GeofenceService : IGeofenceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public GeofenceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public Geofence GetGeofenceById(int geofenceId)
        {
            return _unitOfWork.Repository<Geofence>().GetById(geofenceId);
        }

        public IEnumerable<Geofence> GetGeofencesByFleet(int fleetId)
        {
            return _unitOfWork.Repository<Geofence>()
                .Find(g => g.FleetId == fleetId && g.IsActive);
        }

        public void CreateGeofence(Geofence geofence)
        {
            if (geofence == null)
                throw new ArgumentNullException(nameof(geofence));

            geofence.IsActive = true;
            geofence.CreatedDate = DateTime.UtcNow;

            _unitOfWork.Repository<Geofence>().Add(geofence);
            _unitOfWork.SaveChanges();
        }

        public void UpdateGeofence(Geofence geofence)
        {
            if (geofence == null)
                throw new ArgumentNullException(nameof(geofence));

            geofence.ModifiedDate = DateTime.UtcNow;
            _unitOfWork.Repository<Geofence>().Update(geofence);
            _unitOfWork.SaveChanges();
        }

        public void DeleteGeofence(int geofenceId)
        {
            var geofence = _unitOfWork.Repository<Geofence>().GetById(geofenceId);
            if (geofence == null)
                throw new InvalidOperationException($"Geofence with ID {geofenceId} not found.");

            geofence.IsActive = false;
            geofence.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Repository<Geofence>().Update(geofence);
            _unitOfWork.SaveChanges();
        }

        public bool IsVehicleInGeofence(int vehicleId, int geofenceId)
        {
            var latestPosition = _unitOfWork.Repository<GPSPosition>()
                .Find(gps => gps.VehicleId == vehicleId)
                .OrderByDescending(gps => gps.Timestamp)
                .FirstOrDefault();

            if (latestPosition == null)
                return false;

            var geofence = _unitOfWork.Repository<Geofence>().GetById(geofenceId);
            if (geofence == null || !geofence.IsActive)
                return false;

            // Simplified point-in-polygon check (in real implementation, use actual GeoJSON parsing)
            return IsPointInGeofence(
                (double)latestPosition.Latitude, 
                (double)latestPosition.Longitude, 
                geofence.Polygon);
        }

        public IEnumerable<GeofenceAlert> CheckGeofenceViolations(int fleetId)
        {
            var alerts = new List<GeofenceAlert>();
            var geofences = GetGeofencesByFleet(fleetId).ToList();
            
            if (!geofences.Any())
                return alerts;

            var vehicles = _unitOfWork.Repository<Vehicle>()
                .Find(v => v.FleetId == fleetId && v.Status == "Active")
                .ToList();

            foreach (var vehicle in vehicles)
            {
                var latestPosition = _unitOfWork.Repository<GPSPosition>()
                    .Find(gps => gps.VehicleId == vehicle.VehicleId)
                    .OrderByDescending(gps => gps.Timestamp)
                    .FirstOrDefault();

                if (latestPosition == null)
                    continue;

                foreach (var geofence in geofences)
                {
                    bool isInside = IsPointInGeofence(
                        (double)latestPosition.Latitude, 
                        (double)latestPosition.Longitude, 
                        geofence.Polygon);

                    bool shouldAlert = (geofence.AlertType == "Entry" && isInside) ||
                                      (geofence.AlertType == "Exit" && !isInside) ||
                                      (geofence.AlertType == "Both");

                    if (shouldAlert)
                    {
                        alerts.Add(new GeofenceAlert
                        {
                            VehicleId = vehicle.VehicleId,
                            VIN = vehicle.VIN,
                            GeofenceId = geofence.GeofenceId,
                            GeofenceName = geofence.Name,
                            AlertType = isInside ? "Entry" : "Exit",
                            Timestamp = latestPosition.Timestamp,
                            Latitude = latestPosition.Latitude,
                            Longitude = latestPosition.Longitude
                        });
                    }
                }
            }

            return alerts;
        }

        private bool IsPointInGeofence(double latitude, double longitude, string polygon)
        {
            // Simplified implementation - in production would use proper GeoJSON parsing
            // and ray-casting algorithm for point-in-polygon detection
            return true;
        }
    }

    public class GeofenceAlert
    {
        public int VehicleId { get; set; }
        public string VIN { get; set; }
        public int GeofenceId { get; set; }
        public string GeofenceName { get; set; }
        public string AlertType { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
