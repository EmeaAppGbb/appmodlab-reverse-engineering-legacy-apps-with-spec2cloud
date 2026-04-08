using System;
using System.Linq;
using TransFleet.Data;
using TransFleet.Data.Entities;

namespace TransFleet.WcfServices
{
    public class TelematicsService : ITelematicsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TelematicsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public void ReceiveVehicleData(VehicleTelematicsData data)
        {
            try
            {
                // Store GPS position
                var gpsPosition = new GPSPosition
                {
                    VehicleId = data.VehicleId,
                    Latitude = data.Latitude,
                    Longitude = data.Longitude,
                    Speed = data.Speed,
                    Heading = data.Heading,
                    Timestamp = data.Timestamp,
                    CreatedDate = DateTime.UtcNow
                };

                _unitOfWork.Repository<GPSPosition>().Add(gpsPosition);

                // Update vehicle odometer if provided
                if (data.OdometerReading.HasValue)
                {
                    var vehicle = _unitOfWork.Repository<Vehicle>().GetById(data.VehicleId);
                    if (vehicle != null && data.OdometerReading.Value > vehicle.OdometerReading)
                    {
                        vehicle.OdometerReading = data.OdometerReading.Value;
                        vehicle.ModifiedDate = DateTime.UtcNow;
                        _unitOfWork.Repository<Vehicle>().Update(vehicle);
                    }
                }

                _unitOfWork.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log error
                throw new Exception($"Failed to process telematics data for vehicle {data.VehicleId}", ex);
            }
        }

        public VehicleStatus GetVehicleStatus(int vehicleId)
        {
            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(vehicleId);
            if (vehicle == null)
                throw new Exception($"Vehicle {vehicleId} not found");

            var latestPosition = _unitOfWork.Repository<GPSPosition>()
                .Find(p => p.VehicleId == vehicleId)
                .OrderByDescending(p => p.Timestamp)
                .FirstOrDefault();

            var status = new VehicleStatus
            {
                VehicleId = vehicleId,
                Status = vehicle.Status
            };

            if (latestPosition != null)
            {
                status.LastKnownLatitude = latestPosition.Latitude;
                status.LastKnownLongitude = latestPosition.Longitude;
                status.LastContactTime = latestPosition.Timestamp;
                status.IsOnline = (DateTime.UtcNow - latestPosition.Timestamp).TotalMinutes < 15;
            }

            return status;
        }

        public void SendCommand(int vehicleId, VehicleCommand command)
        {
            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(vehicleId);
            if (vehicle == null)
                throw new Exception($"Vehicle {vehicleId} not found");

            // In a real implementation, this would send the command to the vehicle's telematics device
            // For this demo, we just log it
            Console.WriteLine($"Command {command.CommandType} sent to vehicle {vehicleId}");
        }
    }
}
