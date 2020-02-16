using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IB.WatchServer.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class YASailController : ControllerBase
    {
        private readonly ILogger<YASailController> _logger;
        private readonly DataConnectionFactory _dbFactory;

        public YASailController (ILogger<YASailController > logger, DataConnectionFactory dbFactory)
        {
            _logger = logger;
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Process route list request. 
        /// </summary>
        /// <param name="publicId">public user ID</param>
        /// <returns>JSON with all user's routes</returns>
        [HttpGet("RouteList/{publicId:length(7, 14)}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [RequestRateFactory(KeyField ="publicId", Seconds = 2)]
        public async Task<ActionResult<IEnumerable<YasRoute>>> RouteList([FromRoute] string publicId)
        {
            await using var db = _dbFactory.Create();
            var yasUser = db.GetTable<YasUser>().SingleOrDefault(u => u.PublicId == publicId);
            if (yasUser == null)
                return NotFound(new ErrorResponse(){ StatusCode = StatusCodes.Status404NotFound, Description = "User not found" });

            var routes = db.GetTable<YasRoute>().Where(r => r.UserId == yasUser.UserId)
                .OrderByDescending(r => r.UploadTime);
            var waypoints  = routes.Join(db.GetTable<YasWaypoint>(), r => r.RouteId, w => w.RouteId, (r, w) => w).ToArray();
            var routesArray = routes.ToArray();
            foreach(var route in routesArray)
                route.Waypoints = waypoints.Where(w => w.RouteId == route.RouteId).OrderBy(w => w.OrderId);

            _logger.LogInformation("Watch app request from User {@YasUser}, {RoutesCount} routes found", yasUser, routesArray.Length);
            return routesArray;
        }
    }
}