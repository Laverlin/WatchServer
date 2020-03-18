﻿
namespace IB.WatchServer.Service.Entity.V1
{
    /// <summary>
    /// Represents the location description
    /// </summary>
    public class LocationResponse : BaseApiResponse
    {
        /// <summary>
        /// The Name of the plase 
        /// </summary>
        public string CityName { get; set; }
    }
}