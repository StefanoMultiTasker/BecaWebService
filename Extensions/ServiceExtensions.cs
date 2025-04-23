using BecaWebService.Authorization;
using BecaWebService.ExtensionsLib;
using BecaWebService.Services;
using BecaWebService.Services.Custom;
using Contracts;
using Contracts.Custom;
using Entities;
using Entities.Contexts;
using LoggerService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Repository;
using System.Text;

namespace BecaWebService.Extensions
{
    public static class ServiceExtensions
    {

        public static void ConfigureCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });
        }

        public static void ConfigureIISIntegration(this IServiceCollection services)
        {
            services.Configure<IISOptions>(options =>
            {

            });
        }

        public static void ConfigureDB(this IServiceCollection services, IConfiguration Configuration)
        {
            services.AddDbContext<DbMemoryContext>();
            services.AddDbContext<DbBecaContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("SQLBeca"));
                //o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });
        }

        public static void ConfigureAuth(this IServiceCollection services, IConfiguration Configuration)
        {
            string? appToken = Configuration.GetSection("AppSettings:Token").Value;
            if (appToken != null)
            {
                var key = Encoding.ASCII.GetBytes(appToken);
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                {
                    //options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });
            }

        }

        public static void ConfigureLoggerService(this IServiceCollection services)
        {
            services.AddSingleton<ILoggerManager, LoggerManager>();
        }

        public static void ConfigureRepositoryWrapper(this IServiceCollection services)
        {
            //services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
        }

        public static void ConfigureMyCache(this IServiceCollection services)
        {
            services.AddSingleton<IMyMemoryCache, MyMemoryCache>();
        }

        public static void ConfigureDI(this IServiceCollection services)
        {
            services.AddScoped<IJwtUtils, JwtUtils>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICompanyService, CompanyService>();

            //services.AddScoped<IDependencies, Dependencies>();
            //services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
            services.AddScoped<FormTool>();
            //services.AddScoped<ISqlConnectionExtendedFactory, SqlConnectionExtendedFactory>();
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddScoped<IGenericService, GenericService>();
            services.AddScoped<IBecaViewRepository, BecaViewRepository>();
            services.AddScoped<IBecaRepository, BecaRepository>();

            services.AddScoped<IHomePageRepository, HomePageRepository>();
            services.AddScoped<IHomePageService, HomePageService>();

            //services.AddScoped<IMiscService, MiscService>();

            //services.ConfigureDICustom();
        }

        public static void ConfigureDICustom(this IServiceCollection services)
        {
            services.AddScoped<ISharedService, SharedService>();
            services.AddScoped<IDocumentiService, DocumentiService>();
            services.AddScoped<ILavorService, LavorService>();
            services.AddScoped<IPmsService, PmsService>();
            services.AddScoped<IPresenzeService, PresenzeService>();
            services.AddScoped<ISavinoService, SavinoService>();
            services.AddScoped<IMarketingService, MarketingService>();
            services.AddScoped<IMiscService, MiscService>();
            services.AddScoped<IPrintService, PrintService>();
            services.AddScoped<IMailService, MailService>();
        }

        public static void ConfigureJSON(this IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver { NamingStrategy = new LowerCamelCaseNamingStrategy() };
                })
                .AddJsonOptions(jsonOptions => jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = null);
        }

        public class LowerCamelCaseNamingStrategy : NamingStrategy
        {
            protected override string ResolvePropertyName(string name)
            {
                return name.ToLowerToCamelCase();
            }
        }

        public class LowerNamingStrategy : NamingStrategy
        {
            protected override string ResolvePropertyName(string name)
            {
                return name.ToLower();
            }
        }

    }
}
