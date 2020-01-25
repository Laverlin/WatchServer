using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IB.WatchServer.Service.Entity
{
    public class ErrorResponse
    {
        public string Message { get; set; }

        public int Code { get; set; }
    }
}
