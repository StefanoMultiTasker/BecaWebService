using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using BecaWebService.Models.Users;
using Entities.Models;
using Entities.Contexts;
using BecaWebService.Helpers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using BecaWebService.Authorization;

namespace BecaWebService.Services
{
    public interface IUserService
    {
        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
        AuthenticateResponse RefreshToken(string token, string ipAddress);
        void RevokeToken(string token, string ipAddress);
        IEnumerable<BecaUser> GetAll();
        BecaUser GetById(int id);
        UserMenuResponse GetMenuByUser(int idUtente);
    }

    public class UserService : IUserService
    {
        private DbdatiContext _context;
        private IJwtUtils _jwtUtils;
        private readonly AppSettings _appSettings;

        public UserService(
            DbdatiContext context,
            IJwtUtils jwtUtils,
            IOptions<AppSettings> appSettings)
        {
            _context = context;
            _jwtUtils = jwtUtils;
            _appSettings = appSettings.Value;
        }

        public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
        {
            var user = _context.BecaUsers.SingleOrDefault(x => x.UserName == model.Username);

            // validate
            if (user == null || EncryptedString(model.Password) != user.Pwd)
                throw new AppException("Username or password is incorrect");

            // authentication successful so generate jwt and refresh tokens
            var jwtToken = _jwtUtils.GenerateJwtToken(user);
            var refreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
            user.RefreshTokens.Add(refreshToken);

            // remove old refresh tokens from user
            removeOldRefreshTokens(user);

            // save changes to db
            _context.Update(user);
            _context.SaveChanges();

            return new AuthenticateResponse(user, jwtToken, refreshToken.Token);
        }

        public AuthenticateResponse RefreshToken(string token, string ipAddress)
        {
            var user = getUserByRefreshToken(token);
            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);
            //var refreshToken = new RefreshToken();
            if (refreshToken.IsRevoked)
            {
                // revoke all descendant tokens in case this token has been compromised
                revokeDescendantRefreshTokens(refreshToken, user, ipAddress, $"Attempted reuse of revoked ancestor token: {token}");
                _context.Update(user);
                _context.SaveChanges();
            }

            if (!refreshToken.IsActive)
                throw new AppException("Invalid token");

            // replace old refresh token with a new one (rotate token)
            var newRefreshToken = rotateRefreshToken(refreshToken, ipAddress);
            user.RefreshTokens.Add(newRefreshToken);

            // remove old refresh tokens from user
            removeOldRefreshTokens(user);

            // save changes to db
            _context.Update(user);
            _context.SaveChanges();

            // generate new jwt
            var jwtToken = _jwtUtils.GenerateJwtToken(user);

            return new AuthenticateResponse(user, jwtToken, newRefreshToken.Token);
        }

        public void RevokeToken(string token, string ipAddress)
        {
            var user = getUserByRefreshToken(token);
            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);
            //var refreshToken = new RefreshToken();

            if (!refreshToken.IsActive)
                throw new AppException("Invalid token");

            // revoke token and save
            revokeRefreshToken(refreshToken, ipAddress, "Revoked without replacement");
            _context.Update(user);
            _context.SaveChanges();
        }

        public IEnumerable<BecaUser> GetAll()
        {
            return _context.BecaUsers;
        }

        public BecaUser GetById(int id)
        {
            var user = _context.BecaUsers.Find(id);
            if (user == null) throw new KeyNotFoundException("User not found");
            return user;
        }

        public UserMenuResponse GetMenuByUser(int idUtente)
        {
            var rawMenu = _context.RawUserMenu.Where(m => m.idUtente == idUtente).ToList();
            if (rawMenu == null) throw new KeyNotFoundException("Menu non configurato");

            UserMenuResponse menu = new UserMenuResponse();
            //List<UserMenuCompany>
            var companies = rawMenu.GroupBy(
                c => c.idCompany,
                (key) => new { idCompany = key }
                );
            foreach (var company in companies)
            {
                UserMenuCompany c = new UserMenuCompany();
                c.idCompany = company.Key;

                var areas = rawMenu.Where(m => m.idCompany == c.idCompany).GroupBy(
                    c => new { c.idArea, c.Area},
                    (key) => new { idArea = key.idArea, Area = key.Area }
                    );
                foreach(var area in areas)
                {
                    UserMenuArea a = new UserMenuArea();
                    a.idArea = area.Key.idArea;
                    a.Area = area.Key.Area;

                    var panels = rawMenu.Where(m => m.idCompany == c.idCompany && m.idArea == a.idArea).GroupBy(
                        c => new { c.idPanel, c.Panel },
                        (key) => new { idPanel = key.idPanel, Panel = key.Panel }
                        );
                    foreach (var panel in panels)
                    {
                        UserMenuPanel p = new UserMenuPanel();
                        p.idPanel = panel.Key.idPanel;
                        p.Panel = panel.Key.Panel;

                        var items = rawMenu
                            .Where(m => m.idCompany == c.idCompany && m.idArea == a.idArea && m.idPanel == p.idPanel);
                        foreach (UserMenu item in items)
                        {
                            UserMenuItem i = new UserMenuItem();
                            i.idItem = item.idItem;
                            i.Caption = item.Caption;
                            i.DescMenuItem = item.DescMenuItem;
                            i.IconType = item.IconType;
                            i.Icon = item.Icon;
                            i.idGroup = item.idGroup;
                            i.Position = item.Position;
                            i.Form = item.Form;
                            i.DetailsForm = item.DetailsForm;
                            i.CustomForm = item.CustomForm;
                            i.GridWait4Param = item.GridWait4Param;
                            i.Parameters = item.Parameters;
                            i.flAdd = item.flAdd;
                            i.flEdit = item.flEdit;
                            i.flDel = item.flDel;
                            i.flDetail = item.flDetail;
                            i.flList = item.flList;
                            i.flExcel = item.flExcel;

                            p.Menu.Add(i);
                        }

                        a.Panels.Add(p);
                    }
                    c.Areas.Add(a);
                }
                menu.Companies.Add(c);
            }
            return menu;
        }

        // helper methods

        private BecaUser getUserByRefreshToken(string token)
        {
            var user = _context.BecaUsers.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
            //var user = _context.BecaUsers.SingleOrDefault(u => u.UserName=="A");

            if (user == null)
                throw new AppException("Invalid token");

            return user;
        }

        private RefreshToken rotateRefreshToken(RefreshToken refreshToken, string ipAddress)
        {
            var newRefreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
            revokeRefreshToken(refreshToken, ipAddress, "Replaced by new token", newRefreshToken.Token);
            return newRefreshToken;
        }

        private void removeOldRefreshTokens(BecaUser user)
        {
            // remove old inactive refresh tokens from user based on TTL in app settings
            user.RefreshTokens.RemoveAll(x =>
                !x.IsActive &&
                x.Created.AddDays(_appSettings.RefreshTokenTTL) <= DateTime.UtcNow);
        }

        private void revokeDescendantRefreshTokens(RefreshToken refreshToken, BecaUser user, string ipAddress, string reason)
        {
            // recursively traverse the refresh token chain and ensure all descendants are revoked
            if (!string.IsNullOrEmpty(refreshToken.ReplacedByToken))
            {
                var childToken = user.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken.ReplacedByToken);
                //var childToken = new RefreshToken();
                if (childToken.IsActive)
                    revokeRefreshToken(childToken, ipAddress, reason);
                else
                    revokeDescendantRefreshTokens(childToken, user, ipAddress, reason);
            }
        }

        private void revokeRefreshToken(RefreshToken token, string ipAddress, string reason = null, string replacedByToken = null)
        {
            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason;
            token.ReplacedByToken = replacedByToken;
        }

        private string EncryptedString(string text)
        {
            //generate a 128 - bit salt using a cryptographically strong random sequence of nonzero values
            //byte[] salt = new byte[128 / 8];
            //using (var rngCsp = new RNGCryptoServiceProvider())
            //{
            //    rngCsp.GetNonZeroBytes(salt);
            //}
            byte[] salt = Encoding.ASCII.GetBytes(_appSettings.Salt);
            Console.WriteLine($"Salt: {Convert.ToBase64String(salt)}");

            // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: text,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));
            return hashed;
        }
    }
}
