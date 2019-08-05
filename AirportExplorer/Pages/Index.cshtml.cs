using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Common.Enums;
using GoogleApi.Entities.Places.Details.Request;
using GoogleApi.Entities.Places.Photos.Request;
using GoogleApi.Entities.Places.Search.NearBy.Request;

namespace AirportExplorer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        public string MapboxAccessToken { get;  }
        public string GoogleApiKey { get; set; }


        public IndexModel(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            MapboxAccessToken = configuration["Mapbox:AccessToken"];
            GoogleApiKey = configuration["google:ApiKey"];
        }

        public void OnGet()
        {

        }

        public IActionResult OnGetAirports()
        {
            var configuration = new Configuration
            {
                BadDataFound = context =>
                {

                }
            };
            using (var sr = new StreamReader(Path.Combine(_hostingEnvironment.WebRootPath, "airports.dat")))
            using (var reader = new CsvReader(sr, configuration))
            {
                FeatureCollection featureCollection = new FeatureCollection();

                while (reader.Read())
                {
                    string name = reader.GetField<string>(1);
                    string iataCode = reader.GetField<string>(4);
                    double latitude = reader.GetField<double>(6);
                    double longitude = reader.GetField<double>(7);

                    featureCollection.Features.Add(new Feature(
                        new Point(new Position(latitude, longitude)),
                        new Dictionary<string, object>
                        {
                            {"name", name },
                            {"iataCode", iataCode }
                        }));

                }

                return new JsonResult(featureCollection);
            }                              
        }


        public async Task<IActionResult> OnGetAirportDetail(string name, double latitude, double longitude)
        {
            var airportDetail = new AirportDetail();

            var searchResponse = await GooglePlaces.NearBySearch.QueryAsync(new PlacesNearBySearchRequest
            {
                Key = GoogleApiKey,
                Name = name,
                Location = new Location(latitude, longitude),
                Radius = 1000
            });

            if (!searchResponse.Status.HasValue || searchResponse.Status.Value != Status.Ok || !searchResponse.Results.Any())
                return new BadRequestResult();

            var nearbyResult = searchResponse.Results.FirstOrDefault();
            string placeId = nearbyResult.PlaceId;
            string photoReference = nearbyResult.Photos?.FirstOrDefault()?.PhotoReference;
            string photoCredit = nearbyResult.Photos?.FirstOrDefault()?.HtmlAttributions.FirstOrDefault();

            var detailResponse = await GooglePlaces.Details.QueryAsync(new PlacesDetailsRequest
            {
                Key = GoogleApiKey,
                PlaceId = placeId
            });

            if (!detailResponse.Status.HasValue || detailResponse.Status.Value != Status.Ok)
                return new BadRequestResult();

            var detailsResult = detailResponse.Result;
            airportDetail.FormattedAddress = detailsResult.FormattedAddress;
            airportDetail.PhoneNumber = detailsResult.InternationalPhoneNumber;
            airportDetail.Website = detailsResult.Website;

            if (photoReference != null)
            {
                var photosResponse = await GooglePlaces.Photos.QueryAsync(new PlacesPhotosRequest
                {
                    Key = GoogleApiKey,
                    PhotoReference = photoReference,
                    MaxWidth = 400
                });

                if (photosResponse.Buffer != null)
                {
                    airportDetail.Photo = Convert.ToBase64String(photosResponse.Buffer);
                    airportDetail.PhotoCredit = photoCredit;
                }
            }

            return new JsonResult(airportDetail);

        }

    }


    public class AirportDetail
    {
        public string FormattedAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string Photo { get; set; }
        public string PhotoCredit { get; set; }
        public string Website { get; set; }
    }


}
