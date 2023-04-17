using Entities.Models;
using Newtonsoft.Json;

namespace BecaWebService.Models.Users
{
    public class AuthenticateResponse
    {
        public int idUtente { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public string EMail { get; set; }
        public string Phone { get; set; }
        public bool isConfirmed { get; set; }
        public bool isPrivacyRead { get; set; }
        public bool isPwdChanged { get; set; }
        public string Token { get; set; }
        public List<UserCompany> Companies { get; set; }

        [JsonIgnore] // refresh token is returned in http only cookie
        public string RefreshToken { get; set; }

        public AuthenticateResponse(BecaUser user, string jwtToken, string refreshToken)
        {
            idUtente = user.idUtente;
            Username = user.UserName;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Title = user.Title;
            Phone = user.EMail;
            Phone = user.Phone;
            isConfirmed = user.isConfirmed;
            isPrivacyRead = user.isPrivacyRead;
            isPwdChanged = user.isPwdChanged;
            Token = jwtToken;
            RefreshToken = refreshToken;
            Companies = user.Companies;
        }
    }
}
