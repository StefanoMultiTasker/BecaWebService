using BecaWebService.Authorization;
using BecaWebService.Extensions;
using BecaWebService.Helpers;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using NLog;
using System.Reflection;

namespace BecaWebService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            LogManager.Setup().LoadConfigurationFromFile(String.Concat(Directory.GetCurrentDirectory(), "/nlog.config"));
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>());
            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.ConfigureCors();
            services.ConfigureIISIntegration();
            services.ConfigureDB(Configuration);
            //services.ConfigureAuth(Configuration);
            services.ConfigureLoggerService();

            services.AddHttpContextAccessor();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.ConfigureJSON();
            services.ConfigureMyCache();
            //services.ConfigureRepositoryWrapper();
            services.ConfigureDI();
            services.AddControllers();
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseCors(x => x
                .SetIsOriginAllowed(origin => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.All
            });

            // global error handler
            app.UseMiddleware<ErrorHandlerMiddleware>();

            // custom jwt auth middleware
            app.UseMiddleware<JwtMiddleware>();
            app.UseMiddleware<OptionsMiddleware>();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
