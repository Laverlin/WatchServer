using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Infrastructure.Linq2DB;
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

        [HttpGet("RouteList/{publicId:length(7, 14)}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[RequestRateFactory(KeyField ="did", Seconds = 5)]
        public async Task<ActionResult<IEnumerable<YasRouteInfo>>> RouteList([FromRoute] string publicId)
        {
            await using var db = _dbFactory.Create();
            var routes = db.GetTable<YasRouteInfo>()
                .Join(db.GetTable<YasUserInfo>().Where(u => u.PublicId == publicId), r => r.UserId, u => u.UserId, (r, u) => r);
            var waypoints  = routes.Join(db.GetTable<YasWaypointInfo>(), r => r.RouteId, w => w.RouteId, (r, w) => w).ToArray();
            var routesArray = routes.ToArray();
            foreach(var route in routesArray)
                route.Waypoints = waypoints.Where(w => w.RouteId == route.RouteId).OrderBy(w => w.OrderId);

            return routesArray;
        }

    }
}