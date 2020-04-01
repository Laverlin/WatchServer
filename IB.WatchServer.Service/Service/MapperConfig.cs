using System;
using System.Collections.Generic;

using AutoMapper;
using IB.WatchServer.Service.Entity.WatchFace;

namespace IB.WatchServer.Service.Service
{
    public static class MapperConfig
    {
        public static IMapper CreateMapper()
        {
            return new MapperConfiguration(mc =>
            {
                mc.CreateMap<WatchRequest, RequestData>();
                mc.CreateMap<WeatherInfo, RequestData>();
                mc.CreateMap<LocationInfo, RequestData>();
                mc.CreateMap<ExchangeRateInfo, RequestData>();
                mc.CreateMap<Dictionary<string, object>, WeatherInfo>()
                    .ForMember(d => d.Temperature, c => c.MapFrom(s => s.ContainsKey("temp") ? s["temp"] : 0))
                    .ForMember(d => d.WindSpeed, c => c.MapFrom(s => s.ContainsKey("speed") ? s["speed"] : 0))
                    .ForMember(d => d.Humidity,
                        c => c.MapFrom(s => s.ContainsKey("humidity") ? Convert.ToDecimal(s["humidity"]) / 100 : 0))
                    .ForAllOtherMembers(o => o.MapFrom(s =>
                        s.ContainsKey(o.DestinationMember.Name.ToLower())
                            ? s[o.DestinationMember.Name.ToLower()]
                            : null));
            }).CreateMapper();
        }
    }
}
