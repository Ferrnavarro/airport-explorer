using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace AirportExplorer.Pages
{
    public class IndexModel : PageModel
    {
        public string MapboxAccessToken { get;  }

        public IndexModel(IConfiguration configuration)
        {
            MapboxAccessToken = configuration["Mapbox:AccessToken"];
        }

        public void OnGet()
        {

        }
    }
}
