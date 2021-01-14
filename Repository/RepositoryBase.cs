using Contracts;
using Entities.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace Repository
{
    public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected DbdatiContext dbdatiContext { get; set; }
        protected readonly IHttpContextAccessor _httpContextAccessor;

        public RepositoryBase(DbdatiContext repositoryContext)
        {
            this.dbdatiContext = repositoryContext;
        }

        public RepositoryBase(DbdatiContext repositoryContext, IHttpContextAccessor httpContextAccessor)
        {
            this.dbdatiContext = repositoryContext;
            _httpContextAccessor = httpContextAccessor;
            this.Settings();
        }

        public IQueryable<T> GetAll()
        {
            return this.dbdatiContext.Set<T>().AsNoTracking();
        }

        public IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression)
        {
            return this.dbdatiContext.Set<T>().Where(expression).AsNoTracking();
        }

        public void Create(T entity)
        {
            this.dbdatiContext.Set<T>().Add(entity);
        }

        public void Update(T entity)
        {
            this.dbdatiContext.Set<T>().Update(entity);
        }

        public void Delete(T entity)
        {
            this.dbdatiContext.Set<T>().Remove(entity);
        }

        private void Settings()
        {
            if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = _httpContextAccessor.HttpContext.Request.Headers["Authorization"][0].Replace("Bearer ", "");
                IEnumerable<System.Security.Claims.Claim> claims = GetClaimsFromToken(token.ToString());
                //Utente loggedUser = new Utente();
                if (Int32.TryParse(claims.SingleOrDefault(p => p.Type.Contains("NameIdentifier".ToLower()))?.Value, out int idUtente))
                {
                    dbdatiContext.idUtente = idUtente;
                }
                dbdatiContext.domain = claims.SingleOrDefault(p => p.Type.Contains("PrimarySid".ToLower())).Value;
            }
        }

        private IEnumerable<System.Security.Claims.Claim> GetClaimsFromToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("BecaWebForEncrypt")),
                ValidateIssuer = false,
                ValidateAudience = false, //you might want to validate the audience and issuer depending on your use case
                ValidateLifetime = false //here we are saying that we don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            return principal.Claims;
        }
    }
}
