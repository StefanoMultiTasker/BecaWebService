using AutoMapper;
using Contracts;
using Entities;
using Entities.Contexts;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private IDependencies _deps;
        private DbBecaContext _repoContext;
        private IBecaViewRepository _BecaView;
        private readonly IMapper _mapper;

        public IBecaViewRepository BecaView
        {
            get
            {
                if (_BecaView == null)
                {
                    _BecaView = new BecaViewRepository(_deps);
                }
                return _BecaView;
            }
        }

        public RepositoryWrapper(IDependencies deps)
        {
            _deps = deps;
            _repoContext = _deps.context;
        }

        public async Task SaveAsync()
        {
            await _repoContext.SaveChangesAsync();
        }

        public void ReadToken(string token)
        {
            token = token.Replace("Bearer ", "");
            IEnumerable<System.Security.Claims.Claim> claims = GetClaimsFromToken(token.ToString());
            //Utente loggedUser = new Utente();
            if (Int32.TryParse(claims.SingleOrDefault(p => p.Type.Contains("NameIdentifier".ToLower()))?.Value, out int idUtente))
            {
                _repoContext.idUtente = idUtente;
            } else
            {
                _repoContext.idUtente = -1;
            }
            _repoContext.domain = claims.SingleOrDefault(p => p.Type.Contains("PrimarySid".ToLower())).Value;
        }

        public IEnumerable<System.Security.Claims.Claim> GetClaimsFromToken(string token)
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
