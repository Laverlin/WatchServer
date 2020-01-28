namespace IB.WatchServer.Service.Entity
{
    public class ErrorResponse : BaseApiResponse
    {
        public string Message { get; set; }

        public int Code { get; set; }
    }
}
