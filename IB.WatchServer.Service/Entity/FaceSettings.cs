using System.ComponentModel.DataAnnotations;


namespace IB.WatchServer.Service.Entity
{
    /// <summary>
    /// Configuration settings for the watchFace API
    /// </summary>
    public class FaceSettings
    {
        /// <summary>
        /// Service url template
        /// </summary>
        [Required, Url]
        public string BaseUrl { get; set; }

        /// <summary>
        /// Service api key
        /// </summary>
        [Required]
        [StringLength(maximumLength: 64, MinimumLength = 64)]
        public string ApiKey { get; set; }
    }
}
