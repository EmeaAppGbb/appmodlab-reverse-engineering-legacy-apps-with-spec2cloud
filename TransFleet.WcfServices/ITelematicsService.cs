using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace TransFleet.WcfServices
{
    [ServiceContract]
    public interface ITelematicsService
    {
        [OperationContract]
        void ReceiveVehicleData(VehicleTelematicsData data);

        [OperationContract]
        VehicleStatus GetVehicleStatus(int vehicleId);

        [OperationContract]
        void SendCommand(int vehicleId, VehicleCommand command);
    }

    [DataContract]
    public class VehicleTelematicsData
    {
        [DataMember]
        public int VehicleId { get; set; }

        [DataMember]
        public decimal Latitude { get; set; }

        [DataMember]
        public decimal Longitude { get; set; }

        [DataMember]
        public decimal? Speed { get; set; }

        [DataMember]
        public decimal? Heading { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public int? OdometerReading { get; set; }

        [DataMember]
        public decimal? FuelLevel { get; set; }

        [DataMember]
        public decimal? EngineTemperature { get; set; }

        [DataMember]
        public bool IgnitionOn { get; set; }
    }

    [DataContract]
    public class VehicleStatus
    {
        [DataMember]
        public int VehicleId { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public decimal? LastKnownLatitude { get; set; }

        [DataMember]
        public decimal? LastKnownLongitude { get; set; }

        [DataMember]
        public DateTime? LastContactTime { get; set; }

        [DataMember]
        public bool IsOnline { get; set; }
    }

    [DataContract]
    public class VehicleCommand
    {
        [DataMember]
        public string CommandType { get; set; } // DisableEngine, LockDoors, FlashLights, etc.

        [DataMember]
        public string Parameters { get; set; }

        [DataMember]
        public DateTime IssueTime { get; set; }
    }
}
