using AutoMapper;
using BecaWebService.Authorization;
using BecaWebService.ExtensionsLib;
using BecaWebService.Helpers;
using BecaWebService.Models.Communications;
using BecaWebService.Models.Users;
using Contracts;
using DeviceDetectorNET;
using Entities;
using Entities.Contexts;
using Entities.Models;
using ExtensionsLib;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;
using static NLog.LayoutRenderers.Wrappers.ReplaceLayoutRendererWrapper;

namespace BecaWebService.Services
{
    public interface IUserService
    {
        Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress, IHeaderDictionary headers);
        AuthenticateResponse LoginById(int id, string ipAddress);
        AuthenticateResponse RefreshToken(string token, string ipAddress);
        void RevokeToken(string token, string ipAddress);
        IEnumerable<BecaUser> GetAll();
        BecaUser GetById(int id);
        UserMenuResponse GetMenuByUser(int idUtente);
        List<UserMenuItem> GetMenuByProfile(int idProfile, int idCompany);
        List<UserMenuItem> GetMenuAll();
        Task<GenericResponse> AddOrUpdateUserAsync(BecaUserDTO userDto);
        Task<GenericResponse> CreatePassword(int idUtente);
        Task<GenericResponse> RequestResetPassword(UserResetRequest req, string domain);
        Task<GenericResponse> ResetPassword(string token);
        Task<GenericResponse> GenerateUserName(BecaUserDTO userDto);
        Task<GenericResponse> changePassword(string pwd);
    }

    public class UserService : IUserService
    {
        private DbBecaContext _context;
        //private DbMemoryContext _memoryContext;
        private IMyMemoryCache _memoryCache;
        private IJwtUtils _jwtUtils;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppSettings _appSettings;
        private readonly ILoggerManager _logger;

        const Int16 resetValidity = 30;
        public UserService(IDependencies deps, DbBecaContext context, IJwtUtils jwtUtils, IOptions<AppSettings> appSettings,
            IHttpContextAccessor httpContextAccessor, ILoggerManager logger) //DbMemoryContext memoryContext,
        {
            _context = context;
            _memoryCache = deps.memoryCache;
            _jwtUtils = jwtUtils;
            _appSettings = appSettings.Value;
            this._httpContextAccessor = httpContextAccessor;
            _logger = logger;
            //_memoryContext = memoryContext;
        }

        public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress, IHeaderDictionary headers)
        {
            try
            {
                var user = _context.BecaUsers.SingleOrDefault(x => x.UserName.ToLower() == model.Username.ToLower());

                if (user == null)
                {
                    throw new AppException("Username non valida");
                }
                // validate
                if (user.Pwd.ToLower() == "migrazione")
                {
                    if (user.Companies.Count() == 0 || user.Companies.Count(c => c.idUtenteLoc != null) == 0)
                    {
                        throw new AppException("Utente migrato in modo errato");
                    }
                    try
                    {
                        string pwd = getLegacyPasswordByUser(user.Companies.First(c => c.idUtenteLoc != null));
                        _logger.LogInfo($"Logging utente migrato: username {user.UserName}, passsord digitata {model.Password}, password criptata {pwd}");

                        Simple3Des cry = new Simple3Des("BecaW3bC1phKey");
                        string chkPwd = cry.DecryptData(pwd);
                        _logger.LogInfo($"Logging utente migrato: username {user.UserName}, passsword memorizzata {chkPwd}");

                        if (chkPwd != model.Password)
                        {
                            throw new AppException("Username o password non validi");
                        }

                        string newPwd = EncryptedString(chkPwd);
                        _logger.LogInfo($"hashing della password: {newPwd}");
                        user.Pwd = newPwd;

                        _context.Entry(user).State = EntityState.Modified;
                        _logger.LogInfo("Impostata");
                    }
                    catch (Exception ex) {
                        _logger.LogError($"authenticate error: {model.Username}, {ex.Message}, {(ex.InnerException == null ? ex.Source : ex.InnerException.Message)}");
                        throw new AppException(ex.Message);
                    }
                }
                else
                {
                    if (user == null || EncryptedString(model.Password) != user.Pwd)
                        throw new AppException("Username o password non validi");
                }

                // authentication successful so generate jwt and refresh tokens
                var jwtToken = _jwtUtils.GenerateJwtToken(user);
                var refreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
                //_logger.LogInfo($"refreshToken: {refreshToken.}");
                //user.RefreshTokens.Add(refreshToken);
                bool saveLogin = await this.SaveLogin(user.idUtente, ipAddress, headers);
                _logger.LogInfo("Login data saved");
                getFilialiByUser(ref user);

                // remove old refresh tokens from user
                removeOldRefreshTokens(user);

                foreach (UserCompany company in user.Companies)
                {
                    company.LegacyToken = _jwtUtils.GenerateLegacyToken(user, company.idCompany);
                }

                // save changes to db
                //_context.Update(user);
                _context.SaveChanges();
                //_context.Entry(user).State = EntityState.Detached;

                // Store the user in the cache
                var userCopy = GetById(user.idUtente);
                //_memoryCache.Cache.Set($"User_{user.idUtente}", user.deepCopy(), TimeSpan.FromMinutes(30)); // Configura il tempo di scadenza come desiderato

                //var userCopy = GetById(user.idUtente); // user.deepCopy();
                //var existingUser = _memoryContext.Users.SingleOrDefault(u => u.idUtente == user.idUtente);
                //if (existingUser != null)
                //{
                //    _memoryContext.Entry(existingUser).CurrentValues.SetValues(user);
                //}
                //else
                //{
                //    _memoryContext.Users.Add(userCopy);
                //}
                ////if (_memoryContext.Users.SingleOrDefault(u => u.idUtente == user.idUtente) != null)
                ////    _memoryContext.Users.Update(_memoryContext.Users.Find(user.idUtente));
                ////else
                ////    _memoryContext.Users.Add(user.deepCopy());

                //// Explicitly set the state to Added or Modified
                //_memoryContext.Entry(userCopy).State = existingUser != null ? EntityState.Modified : EntityState.Added;

                //try
                //{
                //    _memoryContext.SaveChanges();
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine($"Error: {ex.Message}");
                //    Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                //    throw;
                //}

                return new AuthenticateResponse(user, jwtToken, refreshToken.Token);
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException == null ? "" : ex.InnerException.Message;
                throw new AppException($"{ex.Message} {inner}");
            }
        }

        public string getLegacyPasswordByUser(UserCompany company)
        {
            Connection? cnn = _context.Companies
                .Where(c => c.idCompany == company.idCompany)
                .SelectMany(c => c.Connections)
                .OrderByDescending(c => c.Default)
                .FirstOrDefault();
            if (cnn == null)
            {
                throw new AppException("DB Legacy non corretto");
            }

            string dbName = cnn.ConnectionString;
            DbDatiContext db = new DbDatiContext(null, dbName);

            List<BecaUser> res = db.ExecuteQuery<BecaUser>("legacyUser", "Select Pwd From AnagrafeUtenti Where idUtente = {0}", false, (new List<object>() { company.idUtenteLoc }).ToArray());
            if (res == null || res.Count == 0)
            {
                throw new AppException("Utente non trovato sul DB Legacy");
            }

            return res[0].GetPropertyString("Pwd");
        }

        public AuthenticateResponse LoginById(int id, string ipAddress)
        {
            int idCompany = ((Company)_httpContextAccessor.HttpContext.Items["Company"]).idCompany;
            var user = _context.BecaUsers.SingleOrDefault(x => x.Companies.Any(c => c.idCompany == idCompany && c.idUtenteLoc == id));

            // validate
            if (user == null)
                throw new AppException("Username or password is incorrect");

            // authentication successful so generate jwt and refresh tokens
            var jwtToken = _jwtUtils.GenerateJwtToken(user);
            var refreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
            user.RefreshTokens.Add(refreshToken);
            getFilialiByUser(ref user);

            // remove old refresh tokens from user
            removeOldRefreshTokens(user);

            foreach (UserCompany company in user.Companies)
            {
                company.LegacyToken = _jwtUtils.GenerateLegacyToken(user, company.idCompany);
            }

            // save changes to db
            _context.SaveChanges();

            // Store the user in the cache
            var userCopy = GetById(user.idUtente);

            return new AuthenticateResponse(user, jwtToken, refreshToken.Token);
        }

        private async Task<bool> SaveLogin(int idUtente, string IP, IHeaderDictionary headers)
        {
            try
            {
                //_logger.LogInfo("salvo il login");
                string url = $@"http://www.geoplugin.net/json.gp?ip={IP}";
                RestClient client = new RestClient(url);
                RestRequest request = new RestRequest("", Method.Get);
                request.Timeout = TimeSpan.FromMinutes(10);
                RestResponse response = await client.ExecuteAsync(request);
                string res = response.Content;

                UserLogin login = new UserLogin();
                login.idUtente = idUtente;
                login.dtLogin = DateTime.Now;
                login.IP = IP;

                if (headers.ContainsKey("User-Agent"))
                {
                    login.UserAgent = headers["User-Agent"].ToString().left(255);
                    // Inizializza DeviceDetector
                    var deviceDetector = new DeviceDetector(login.UserAgent);
                    deviceDetector.Parse();

                    // Controlla se il dispositivo è un bot
                    if (deviceDetector.IsBot())
                    {
                        _logger.LogWarn("Il client è un bot.");
                        //return;
                    }

                    // Informazioni sul browser
                    var clientInfo = deviceDetector.GetClient(); // Ottieni il client (browser, versione, ecc.)
                    if (clientInfo != null && clientInfo.Match!=null)  login.Browser = clientInfo.Match.Name.left(255);

                    // Informazioni sul sistema operativo
                    var osInfo = deviceDetector.GetOs(); // Ottieni il sistema operativo
                    if(osInfo!= null && osInfo.Match!=null ) login.OS = ($"{osInfo.Match.Name} {osInfo.Match.Platform}").left(255);

                    // Informazioni sul dispositivo
                    var device = deviceDetector.GetDeviceName().left(255); // Ottieni il nome del dispositivo
                    login.Device = device;

                    // Controlla se il dispositivo è mobile
                    bool isMobile = deviceDetector.IsMobile();
                    login.Mobile = (short?)(isMobile ? 1 : 0);
                } else
                {
                    if (headers.ContainsKey("sec-ch-ua")) login.Browser = headers["sec-ch-ua"].ToString().left(255);
                    if (headers.ContainsKey("sec-ch-ua-platform")) login.OS = headers["sec-ch-ua-platform"].ToString().left(255);
                    if (headers.ContainsKey("sec-ch-ua-mobile")) login.Mobile =
                            headers["sec-ch-ua-mobile"].ToString().Contains("1") ? 1 : headers["sec-ch-ua-mobile"].ToString().Contains("0") ? 0 : null;
                }

                if (res != null)
                {
                    dynamic json = JsonConvert.DeserializeObject<dynamic>(res);

                    login.Citta = json["geoplugin_city"];
                    login.Citta = login.Citta.left(250);
                    login.PV = json["geoplugin_regionCode"];
                    login.PV = login.PV.left(50);
                    login.Regione = json["geoplugin_region"];
                    login.Regione = login.Regione.left(250);
                    login.Nazione = json["geoplugin_countryName"];
                    login.Nazione = login.Nazione.left(250);
                }

                _context.UserLogins.Add(login);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"non sono riuscito: {ex.Message}");
                return false;
            }
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
            var user = _memoryCache.GetOrSetCache($"UserById_{id}", () =>
            {
                return _context.BecaUsers.Find(id);
            });

            //var user = _memoryContext.Users.Find(id);
            //if (user == null)
            //{
            //    user = _context.BecaUsers.Find(id);
            //    _memoryContext.Users.Add(user);
            //    _memoryContext.SaveChanges();
            //}
            if (user == null) throw new KeyNotFoundException("User not found");
            return user;
        }

        public UserMenuResponse GetMenuByUser(int idUtente)
        {
            IList<UserMenu> rawUserMenu = _context.RawUserMenu.Where(m => m.idUtente == idUtente).ToList();
            if (rawUserMenu == null) throw new KeyNotFoundException("Menu non configurato");

            var parents = rawUserMenu
                .Where(m => m.ParentItem != null)
                .GroupBy(m => new { m.idCompany, m.CompanyName, m.ParentItem },
                    (key) => new { idCompany = key.idCompany, CompanyName = key.CompanyName, ParentItem = key.ParentItem })
                .ToList();

            foreach (var parent in parents)
            {
                addParentUserMenu(ref rawUserMenu, parent.Key.ParentItem, parent.Key.idCompany, parent.Key.CompanyName);
            }

            UserMenuResponse menu = new UserMenuResponse();
            var companies = rawUserMenu.GroupBy(
                c => c.idCompany,
                (key) => new { idCompany = key }
            );
            foreach (var company in companies)
            {
                UserMenuCompany c = new UserMenuCompany();
                c.idCompany = company.Key;
                foreach (UserMenu item in rawUserMenu
                    .Where(m => m.ParentItem == null && m.idCompany == c.idCompany)
                    .OrderBy(m => m.idGroup)
                    .ThenBy(m => m.Position).ToList())
                {
                    UserMenuItem i = createMenuItem(item);
                    c.Menu.Add(i);
                    AddMenuItems(ref i, rawUserMenu, c.idCompany);
                }
                if (c.Menu.Count == 1)
                {
                    List<UserMenuItem> level2 = c.Menu[0].Items;
                    c.Menu.RemoveAt(0);
                    c.Menu.AddRange(level2);
                }
                menu.Companies.Add(c);
            }

            return menu;
        }

        private void addParentUserMenu(ref IList<UserMenu> rawUserMenu, int? parentItem, int idCompany, string CompanyName)
        {
            if (!rawUserMenu.Any(m => m.idItem == parentItem && m.idCompany == idCompany))
            {
                BasicMenu rawMenu = _context.RawMenu.FirstOrDefault(m => m.idItem == parentItem);
                UserMenu newMenu = new UserMenu(rawMenu);
                newMenu.idUtente = rawUserMenu[0].idUtente;
                newMenu.UserName = rawUserMenu[0].UserName;
                newMenu.idCompany = idCompany;
                newMenu.CompanyName = CompanyName;
                rawUserMenu.Add(newMenu);
                if (rawMenu.ParentItem != null)
                    addParentUserMenu(ref rawUserMenu, rawMenu.ParentItem, idCompany, CompanyName);
            }
        }

        private void AddMenuItems(ref UserMenuItem menu, IList<UserMenu> allItems, int idCompany)
        {
            int itemId = menu.idItem;
            IList<UserMenu> subItems = allItems
                .Where(m => m.ParentItem == itemId && m.idCompany == idCompany)
                    .OrderBy(m => m.idGroup)
                    .ThenBy(m => m.Position).ToList()
                .ToList();
            foreach (UserMenu item in subItems)
            {
                UserMenuItem i = createMenuItem(item);
                menu.Items.Add(i);
                if (item.isContainer)
                    AddMenuItems(ref i, allItems, idCompany);
            }
        }

        private UserMenuItem createMenuItem(UserMenu item)
        {
            UserMenuItem i = new UserMenuItem();
            i.idItem = item.idItem;
            i.ParentItem = item.ParentItem;
            i.Caption = item.Caption;
            i.DescMenuItem = item.DescMenuItem;
            i.IconType = item.IconType;
            i.Icon = item.Icon;
            i.idGroup = item.idGroup;
            i.Position = item.Position;
            i.isContainer = item.isContainer;
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

            return i;
        }

        public List<UserMenuItem> GetMenuAll()
        {
            IList<BasicMenu> rawMenu = _context.RawMenu.ToList();
            if (rawMenu == null) throw new KeyNotFoundException("Menu non configurato");

            List<UserMenuItem> menu = new List<UserMenuItem>();

            foreach (BasicMenu item in rawMenu
                .Where(m => m.ParentItem == null)
                .OrderBy(m => m.idGroup)
                .ThenBy(m => m.Position).ToList())
            {
                UserMenuItem i = createBasicMenuItem(item);
                menu.Add(i);
                AddBasicMenuItems(ref i, rawMenu);
            }
            if (menu.Count == 1)
            {
                List<UserMenuItem> level2 = menu[0].Items;
                menu.RemoveAt(0);
                menu.AddRange(level2);
            }

            return menu;
        }

        private void AddBasicMenuItems(ref UserMenuItem menu, IList<BasicMenu> allItems)
        {
            int itemId = menu.idItem;
            IList<BasicMenu> subItems = allItems
                .Where(m => m.ParentItem == itemId)
                .OrderBy(m => m.idGroup)
                .ThenBy(m => m.Position).ToList()
                .ToList();
            foreach (BasicMenu item in subItems)
            {
                UserMenuItem i = createBasicMenuItem(item);
                menu.Items.Add(i);
                if (item.isContainer)
                    AddBasicMenuItems(ref i, allItems);
            }
        }

        private UserMenuItem createBasicMenuItem(BasicMenu item)
        {
            UserMenuItem i = new UserMenuItem();
            i.idItem = item.idItem;
            i.ParentItem = item.ParentItem;
            i.Caption = item.Caption;
            i.DescMenuItem = item.DescMenuItem;
            i.IconType = item.IconType;
            i.Icon = item.Icon;
            i.idGroup = item.idGroup;
            i.Position = item.Position;
            i.isContainer = item.isContainer;
            i.Form = item.Form;
            i.DetailsForm = item.DetailsForm;
            i.CustomForm = item.CustomForm;
            i.GridWait4Param = item.GridWait4Param;
            i.Parameters = item.Parameters;
            i.flAdd = false;
            i.flEdit = false;
            i.flDel = false;
            i.flDetail = false;
            i.flList = false;
            i.flExcel = false;

            return i;
        }

        public List<UserMenuItem> GetMenuByProfile(int idProfile, int idCompany)
        {
            IList<ProfileMenu> rawMenu = _context.RawProfileMenu.Where(m => m.idProfile == idProfile && m.idCompany == idCompany).ToList();
            if (rawMenu == null) throw new KeyNotFoundException("Menu non configurato");

            var parents = rawMenu
                .Where(m => m.ParentItem != null)
                .GroupBy(m => new { m.ParentItem },
                    (key) => new { ParentItem = key.ParentItem })
                .ToList();

            foreach (var parent in parents)
            {
                addParentProfileMenu(ref rawMenu, parent.Key.ParentItem, idCompany, idProfile);
            }

            List<UserMenuItem> menu = new List<UserMenuItem>();

            foreach (ProfileMenu item in rawMenu
                .Where(m => m.ParentItem == null)
                    .OrderBy(m => m.idGroup)
                    .ThenBy(m => m.Position).ToList())
            {
                UserMenuItem i = createProfileMenuItem(item);
                menu.Add(i);
                AddProfileMenuItems(ref i, rawMenu);
            }
            if (menu.Count == 1)
            {
                List<UserMenuItem> level2 = menu[0].Items;
                menu.RemoveAt(0);
                menu.AddRange(level2);
            }

            return menu;
        }

        private void addParentProfileMenu(ref IList<ProfileMenu> rawUserMenu, int? parentItem, int idCompany, int idProfile)
        {
            if (!rawUserMenu.Any(m => m.idItem == parentItem))
            {
                BasicMenu rawMenu = _context.RawMenu.FirstOrDefault(m => m.idItem == parentItem);
                ProfileMenu newMenu = new ProfileMenu(rawMenu);
                newMenu.idCompany = rawUserMenu[0].idCompany;
                newMenu.idProfile = rawUserMenu[0].idProfile;
                rawUserMenu.Add(newMenu);
                if (rawMenu.ParentItem != null)
                    addParentProfileMenu(ref rawUserMenu, rawMenu.ParentItem, idCompany, idProfile);
            }
        }

        private UserMenuItem createProfileMenuItem(ProfileMenu item)
        {
            UserMenuItem i = new UserMenuItem();
            i.idItem = item.idItem;
            i.ParentItem = item.ParentItem;
            i.Caption = item.Caption;
            i.DescMenuItem = item.DescMenuItem;
            i.IconType = item.IconType;
            i.Icon = item.Icon;
            i.idGroup = item.idGroup;
            i.Position = item.Position;
            i.isContainer = item.isContainer;
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

            return i;
        }

        private void AddProfileMenuItems(ref UserMenuItem menu, IList<ProfileMenu> allItems)
        {
            int itemId = menu.idItem;
            IList<ProfileMenu> subItems = allItems
                .Where(m => m.ParentItem == itemId)
                .OrderBy(m => m.idGroup)
                .ThenBy(m => m.Position).ToList()
                .ToList();
            foreach (ProfileMenu item in subItems)
            {
                UserMenuItem i = createProfileMenuItem(item);
                menu.Items.Add(i);
                if (item.isContainer)
                    AddProfileMenuItems(ref i, allItems);
            }
        }

        public void getFilialiByUser(ref BecaUser user)
        {
            foreach (UserCompany company in user.Companies)
            {
                Company? _company = _context.Companies.FirstOrDefault(c => c.idCompany == company.idCompany);
                if (_company == null) return;

                Connection? cnn = _context.Companies
                    .Where(c => c.idCompany == _company.idCompany)
                    .SelectMany(c => c.Connections)
                    .OrderByDescending(c => c.Default)
                    .FirstOrDefault();
                if (cnn == null) return;
                string dbName = cnn.ConnectionString;
                DbDatiContext db = new DbDatiContext(null, dbName);

                UserProfile? profile = company.Profiles
                    .Where(c => c.isDefault == true)
                    .FirstOrDefault() ?? company.Profiles.FirstOrDefault();
                if (profile == null) return;

                string sql = "";
                List<object> pars = new List<object>();
                if (company.hasBusinessUnit)
                {
                    switch (profile.Flags)
                    {
                        case "C":
                            sql = "Select * From v4Beca_FilialiClienti Where idUtente = {0}";
                            pars.Add(user.idUtenteLoc(_company.idCompany));
                            company.BusinessUnits1 = new List<UserBusinessUnit>();
                            break;
                        case "I":
                            sql = "Select * From v4Beca_FilialiLavoratori Where idUtente = {0}";
                            pars.Add(user.idUtenteLoc(_company.idCompany));
                            company.BusinessUnits1 = new List<UserBusinessUnit>();
                            break;
                        case "F":
                            sql = "Select * From v4Beca_FilialiUtenti Where idUtente = {0}";
                            pars.Add(user.idUtenteLoc(_company.idCompany));
                            company.BusinessUnits1 = db.ExecuteQuery<UserBusinessUnit>("_UserBusinessUnit", sql, false, pars.ToArray());
                            break;
                        default:
                            sql = "Select * From v4Beca_FilialiSede";
                            company.BusinessUnits1 = db.ExecuteQuery<UserBusinessUnit>("_UserBusinessUnit", sql, false, pars.ToArray());
                            break;
                    }
                }
                else
                {
                    company.BusinessUnits1 = new List<UserBusinessUnit>();
                }
            }
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

        public async Task<GenericResponse> GenerateUserName(BecaUserDTO userDto)
        {
            bool userOk = false;
            int count = 1;
            int nameChars = 1;
            string extraNumber = "";
            string userName = "";

            if ((userDto.firstName ?? "") == "" || (userDto.lastName ?? "") == "") return new GenericResponse("Inserire nome e congome");

            int safeCount = 100 + userDto.firstName.Length;

            while (count < safeCount)
            {
                if (count > userDto.firstName.Length) extraNumber = (count - userDto.firstName.Length).ToString();
                userName = $"{userDto.firstName.ToLower().left(nameChars)}{extraNumber}.{userDto.lastName.ToLower()}";
                int check = _context.BecaUsers.Count(u => u.UserName == userName);
                if (check == 0)
                {
                    userDto.userName = userName;
                    return new GenericResponse(userDto);
                }
                count++;
                if (count <= userDto.firstName.Length) nameChars++;
            }
            return new GenericResponse("Non sono riuscito a generare uno username adeguato");
        }

        public async Task<GenericResponse> AddOrUpdateUserAsync(BecaUserDTO userDto)
        {
            //BecaUserEntity user;
            BecaUser user;

            if (userDto.idUtente.HasValue)
            {
                // Update
                user = await _context.BecaUsers.FindAsync(userDto.idUtente!.Value);
                if (user == null) return null;

                if (user.UserName != userDto.userName)
                {
                    int check = _context.BecaUsers.Count(u => u.UserName == userDto.userName);
                    if (check > 0) return new GenericResponse("Username già esistente");
                }

                user.UserName = userDto.userName;
                user.FirstName = userDto.firstName;
                user.LastName = userDto.lastName;
                user.Title = userDto.title;
                user.Phone = userDto.phone;
                user.email = userDto.email;
                user.isConfirmed = userDto.isConfirmed;
                user.isPrivacyRead = userDto.isPrivacyRead;
                user.isPwdChanged = userDto.isPwdChanged;

                _context.Entry(user).State = EntityState.Modified;
            }
            else
            {
                int check = _context.BecaUsers.Count(u => u.UserName == userDto.userName);
                if (check > 0) return new GenericResponse("Username già esistente");
                // Insert
                user = new BecaUser//Entity
                {
                    UserName = userDto.userName,
                    FirstName = userDto.firstName,
                    LastName = userDto.lastName,
                    Title = userDto.title,
                    Phone = userDto.phone,
                    email = userDto.email,
                    Pwd = "xxx" //EncryptedString(pwd)
                };

                //_context.BecaUserentities.Add(user);
                _context.BecaUsers.Add(user);
            }

            await _context.SaveChangesAsync();

            return new GenericResponse(user);
        }

        public async Task<GenericResponse> changePassword(string pwd)
        {
            try
            {
                BecaUser? tokenUser = (BecaUser)_httpContextAccessor.HttpContext.Items["User"];
                if (tokenUser == null) return new GenericResponse("Token non valido");

                BecaUser? user = await _context.BecaUsers.FindAsync(tokenUser.idUtente);
                if (user == null) return new GenericResponse("Utente non trovato");

                user.Pwd = EncryptedString(pwd);
                user.isPwdChanged = true;

                _context.Entry(user).State = EntityState.Modified;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return new GenericResponse($"Si è verificato un erorre nella modifica della password: {ex.Message}");
            }
            return new GenericResponse(true);
        }

        public async Task<GenericResponse> CreatePassword(int idUtente)
        {
            string pwd = GenerateRandomPassword(8);
            try
            {
                BecaUser? user = await _context.BecaUsers.FindAsync(idUtente);
                if (user == null) return new GenericResponse("Utente non trovato");

                user.Pwd = EncryptedString(pwd);
                user.isPwdChanged = false;

                _context.Entry(user).State = EntityState.Modified;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return new GenericResponse($"Si è verificato un erorre nella generazione della password o nell'invio: {ex.Message}");
            }
            return await UserSendCrentials(idUtente, pwd);
        }

        public async Task<GenericResponse> RequestResetPassword(UserResetRequest req, string domain)
        {
            string pwd = GenerateRandomPassword(25, false);
            try
            {
                BecaUser? user = null;

                //if((req.UserName ?? "") != "")
                //{
                //    user = await _context.BecaUsers.FirstOrDefaultAsync(u => u.UserName == req.UserName);
                //}

                if ((req.UserName ?? "") == "" && (req.email ?? "") != "")
                {
                    List<BecaUser> users = _context.BecaUsers.Where(u => u.email == req.email).ToList();
                    if (users.Count == 1) user = users[0];
                    else
                    {
                        //users = users.Where(u => u.Companies.Count(c=> domain.ToLower().Contains((c.urlDomain ?? "mybeca").ToLower()))>0).ToList();
                        users = users.Where(u => u.Companies.Count(c => req.apl!.ToLower().Contains((c.MainFolder ?? "mybeca").ToLower())) > 0).ToList();
                        if (users.Count == 1) user = users[0];
                    }
                }

                if (user == null)
                {
                    UserSendResetRequest(req);
                    return new GenericResponse(true);
                };

                UserReset? reset = await _context.UsersReset.FindAsync(user.idUtente);
                if (reset != null)
                {
                    if (reset.dtScadenza < DateTime.Now)
                    {
                        reset.token = pwd;
                        reset.dtScadenza = DateTime.Now.AddMinutes(resetValidity);

                        _context.UsersReset.Update(reset);
                    }
                    else
                    {
                        return new GenericResponse(true, "Il reset è già stato chiesto");
                    }
                }
                else
                {
                    _context.UsersReset.Add(new UserReset { idUtente = user.idUtente, token = pwd, dtScadenza = DateTime.Now.AddMinutes(resetValidity) });
                }
                await _context.SaveChangesAsync();

                //UserCompany? cpy = user.Companies.FirstOrDefault(c => domain.ToLower().Contains((c.urlDomain ?? "mybeca").ToLower())) ?? user.Companies.FirstOrDefault(c=>c.isDefault==true);
                UserCompany? cpy = user.Companies.FirstOrDefault(c => req.apl!.ToLower().Contains((c.MainFolder ?? "beca").ToLower())) ?? user.Companies.FirstOrDefault(c => c.isDefault == true);
                string sender = cpy == null ? "credenziali.bw@becaweb.it" : cpy.mail2 ?? cpy.senderEmail;
                await UserSendReset(user.idUtente, pwd, sender);
                return new GenericResponse(true);
            }
            catch (Exception ex)
            {
                return new GenericResponse($"Si è verificato un erorre nella generazione del token di reset: {ex.Message}");
            }
        }

        public async Task<GenericResponse> ResetPassword(string token)
        {
            try
            {
                UserReset? reset = await _context.UsersReset.FirstOrDefaultAsync(r => r.token == token);
                if (reset == null)
                {
                    return new GenericResponse(false, "https://www.mytemporary.it/reset/reset-nonvalido.html");
                }
                if (reset.dtScadenza < DateTime.Now)
                {
                    return new GenericResponse(false, "https://www.mytemporary.it/reset/reset-scaduto.html");
                }

                await CreatePassword(reset.idUtente);
                _context.UsersReset.Remove(reset);
                await _context.SaveChangesAsync();
                return new GenericResponse(true, "https://www.mytemporary.it/reset/reset-ok.html");
            }
            catch (Exception)
            {
                return new GenericResponse(false, "https://www.mytemporary.it/reset/reset-errore.html");
            }
        }


        private string GenerateRandomPassword(int length, bool useSpecial = true)
        {
            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string digitChars = "0123456789";
            const string specialChars = "!@?";

            Random random = new Random();

            // Garantire almeno un carattere per ogni categoria
            char upper = upperChars[random.Next(upperChars.Length)];
            char lower = lowerChars[random.Next(lowerChars.Length)];
            char digit = digitChars[random.Next(digitChars.Length)];
            char special = useSpecial ? specialChars[random.Next(specialChars.Length)] : '\0';

            // Generare il resto della password casualmente dai gruppi di caratteri
            string allChars = upperChars + lowerChars + digitChars + (useSpecial ? specialChars : "");
            int remainingLength = useSpecial ? length - 4 : length - 3; // Ridurre di 3 o 4 in base all'uso di caratteri speciali

            string remainingChars = new string(Enumerable.Repeat(allChars, remainingLength)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            // Combinare tutti i caratteri
            string password = useSpecial
                ? upper.ToString() + lower.ToString() + digit.ToString() + special.ToString() + remainingChars
                : upper.ToString() + lower.ToString() + digit.ToString() + remainingChars;

            // Mescolare i caratteri
            string pwd = new string(password.OrderBy(c => random.Next()).ToArray());
            return pwd;
        }

        private async Task<GenericResponse> UserSendCrentials(int idUtente, string pwd)
        {
            try
            {
                BecaUser? user = await _context.BecaUsers.FindAsync(idUtente);
                if (user == null) return new GenericResponse("Utente non trovato");

                UserCompany? cpy = user.Companies.FirstOrDefault(c => c.isDefault == true);
                if (cpy == null) return new GenericResponse("Nessuna company associata");

                //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                System.Net.Mail.SmtpClient objSMTP = new System.Net.Mail.SmtpClient
                {
                    Host = "pro.eu.turbo-smtp.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential("gruppoedp@abeaform.it", "ddHu39eX")
                };
                string owner = cpy.senderEmail;
                //owner = "postmaster@abeaform.it";
                System.Net.Mail.MailMessage objMail = new System.Net.Mail.MailMessage();
                objMail.Sender = new MailAddress(owner, owner);
                objMail.From = new MailAddress(owner, owner);
                objMail.ReplyToList.Add(new MailAddress(owner));

                objMail.Subject = $"Invio credenziali accesso My{cpy.CompanyName}";
                objMail.IsBodyHtml = true;
                objMail.BodyEncoding = System.Text.Encoding.UTF8;
                objMail.Body = ($"{cpy.InvioCredenziali}");
                objMail.Body = objMail.Body.Replace("#PWD#", pwd);
                objMail.To.Add(new MailAddress($"{user.email}"));

                objSMTP.Send(objMail);
                return new GenericResponse(true);
            }
            catch (Exception ex)
            {
                return new GenericResponse(ex, ex.Message);
            }
        }

        private async Task<GenericResponse> UserSendReset(int idUtente, string pwd, string sender)
        {
            try
            {
                BecaUser? user = await _context.BecaUsers.FindAsync(idUtente);
                if (user == null) return new GenericResponse("Utente non trovato");

                UserCompany? cpy = user.Companies.FirstOrDefault(c => c.isDefault == true);
                if (cpy == null) return new GenericResponse("Nessuna company associata");

                //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                System.Net.Mail.SmtpClient objSMTP = new System.Net.Mail.SmtpClient
                {
                    Host = "pro.eu.turbo-smtp.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential("gruppoedp@abeaform.it", "ddHu39eX")
                };
                string owner = sender;
                //owner = "postmaster@abeaform.it";
                System.Net.Mail.MailMessage objMail = new System.Net.Mail.MailMessage();
                objMail.Sender = new MailAddress(owner, owner);
                objMail.From = new MailAddress(owner, owner);
                objMail.ReplyToList.Add(new MailAddress(owner));

                objMail.Subject = $"Richiesta di reset delle credenziali per l'accesso a My{cpy.CompanyName}";
                objMail.IsBodyHtml = true;
                objMail.BodyEncoding = System.Text.Encoding.UTF8;
                objMail.Body = ($"{cpy.ResetCredenziali.Replace("#token#", pwd)}");
                objMail.Body = objMail.Body.Replace("#PWD#", pwd);
                objMail.To.Add(new MailAddress($"{user.email}"));

                objSMTP.Send(objMail);
                return new GenericResponse(true);
            }
            catch (Exception ex)
            {
                return new GenericResponse(ex, ex.Message);
            }
        }

        private GenericResponse UserSendResetRequest(UserResetRequest req)
        {
            try
            {
                //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                System.Net.Mail.SmtpClient objSMTP = new System.Net.Mail.SmtpClient
                {
                    Host = "pro.eu.turbo-smtp.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential("gruppoedp@abeaform.it", "ddHu39eX")
                };
                string owner = "credenziali.bw@attalgroup.it";
                string dest = "credenziali@attalgroup.it";
                //owner = "postmaster@abeaform.it";
                System.Net.Mail.MailMessage objMail = new System.Net.Mail.MailMessage();
                objMail.Sender = new MailAddress(owner, owner);
                objMail.From = new MailAddress(owner, owner);
                objMail.ReplyToList.Add(new MailAddress(owner));

                objMail.Subject = $"TEST - Richiesta di reset delle credenziali per l'accesso a My{req.apl}";
                objMail.IsBodyHtml = true;
                objMail.BodyEncoding = System.Text.Encoding.UTF8;
                objMail.Body = $"<p>E' stato richiesto il reset della password per un utente che non è stato possibile indentificare a sistema.</p>" +
                    $"<p>email: {req.email}</P>" +
                    $"<p>Nome: {req.Nome}</P>" +
                    $"<p>Cognome: {req.Cognome}</P>" +
                    $"<p>Cliente: {req.Cliente}</P>" +
                    $"<p>APL: {req.apl}</P>" +
                    $"<p></P>";
                objMail.To.Add(new MailAddress(dest));
                objMail.Bcc.Add(new MailAddress("s.bettica@abeaform.it"));

                objSMTP.Send(objMail);
                return new GenericResponse(true);
            }
            catch (Exception ex)
            {
                return new GenericResponse(ex, ex.Message);
            }
        }
    }
}
