using BecaWebService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BecaWebService.Helpers
{
    public class OptionsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppSettings _appSettings;

        public OptionsMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
        {
            _next = next;
            _appSettings = appSettings.Value;
        }

        public async Task Invoke(HttpContext context, ICompanyService companyService)
        {
            var _Company = context.Request.Headers["Company"].FirstOrDefault()?.Split(" ").Last();
            if(_Company != null)
            {
                Int32.TryParse(_Company, out int idCompany);
                context.Items["Company"] = companyService.GetById(idCompany);
            }

            await _next(context);
        }
    }
}
