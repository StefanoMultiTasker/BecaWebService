using AutoMapper;
using Contracts;
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
        private DbdatiContext _repoContext;
        private IdbaFunzioniAreeRepository _Area;
        private IdbaFunzioniGruppiRepository _Gruppo;
        private IdbaFunzioniRepository _Voce;
        private IdbaFunzioniCfgRepository _VoceConfig;
        private IMenuLivelloRepository _MenuLivello;
        private IBecaViewRepository _BecaView;
        private readonly IMapper _mapper;

        public IdbaFunzioniAreeRepository Area
        {
            get
            {
                if (_Area == null)
                {
                    _Area = new DbaFunzioniAreeRepository(_repoContext);
                }
                return _Area;
            }
        }

        public IdbaFunzioniGruppiRepository Gruppo
        {
            get
            {
                if (_Gruppo == null)
                {
                    _Gruppo = new DbaFunzioniGruppiRepository(_repoContext);
                }
                return _Gruppo;
            }
        }

        public IdbaFunzioniRepository Voce
        {
            get
            {
                if (_Voce == null)
                {
                    _Voce = new DbaFunzioniRepository(_repoContext);
                }
                return _Voce;
            }
        }

        public IdbaFunzioniCfgRepository VoceConfig
        {
            get
            {
                if (_VoceConfig == null)
                {
                    _VoceConfig = new DbaFunzioniCfgRepository(_repoContext);
                }
                return _VoceConfig;
            }
        }

        public IMenuLivelloRepository MenuLivello
        {
            get
            {
                if (_MenuLivello == null)
                {
                    _MenuLivello = new MenuLivelloRepository(_repoContext);
                }
                return _MenuLivello;
            }
        }

        public IBecaViewRepository BecaView
        {
            get
            {
                if (_BecaView == null)
                {
                    _BecaView = new BecaViewRepository(_repoContext, _mapper);
                }
                return _BecaView;
            }
        }

        public RepositoryWrapper(DbdatiContext repositoryContext, IMapper mapper)
        {
            _repoContext = repositoryContext;
            _mapper = mapper;
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
