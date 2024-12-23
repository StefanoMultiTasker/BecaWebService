using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models.Custom
{
    public class LinkResponse
    {
        public bool success { get; set; }
        public LinkResponseData? data { get; set; }
        public string? errorCode { get; set; }
        public string? error { get; set; }
    }
    public class LinkResponseData
    {
        public string? url { get; set; }
        public string? content { get; set; }
        public string? otp { get; set; }
    }

    public class GetOrderResponse
    {
        public bool success { get; set; }
        public OrderResponse? data { get; set; }
    }
    public class OrderResponse
    {
        public int id { get; set; }
        public string? code { get; set; }
        public string? subject { get; set; }
        public string? description { get; set; }
        public bool notification { get; set; }
        public OrderStateResponse? state { get; set; }
        public string? username { get; set; }
        public string? usernameHsm { get; set; }
        public string? passwordHsm { get; set; }
        public DateTime dateCreate { get; set; }
        public DateTime? dateEdit { get; set; }
        public List<OrderFirmatarioResponse>? signatories { get; set; }
        public List<OrderLog>? logs { get; set; }
        public List<OrderFileResponse>? files { get; set; }
    }
    public class OrderStateResponse
    {
        public int id { get; set; }
        public string? code { get; set; }
        public string describe { get; set; }
    }
    public class OrderFirmatarioAddResponse
    {
        public bool success { get; set; }
        public OrderFirmatarioResponse data { get; set; }
    }
    public class OrderFirmatarioResponse
    {
        public int? id { get; set; }
        public string? name { get; set; }
        public string? surname { get; set; }
        public string? fullname { get; set; }
        public string? fiscalCode { get; set; }
        public string? prefix { get; set; }
        public string? phone { get; set; }
        public string? fullphone { get; set; }
        public string? pec { get; set; }
        public string? email { get; set; }
        public string? token { get; set; }
        public string? otp { get; set; }
        public DateTime? dueOtp { get; set; }
        public bool? acceptPrivacy { get; set; }
        public bool? acceptClause { get; set; }
        public bool? signature { get; set; }
        public int? orderby { get; set; }
        public string? redirectTo { get; set; }
        public int? type { get; set; }
    }
    public class OrderLog
    {
        public int id { get; set; }
        public DateTime dateCreate { get; set; }
        public string? description { get; set; }
    }
    public class OrderFileResponse
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? filename { get; set; }
        public string? size { get; set; }
        public string? sizeKB { get; set; }
        public string? mimetype { get; set; }
        public string? contentToBase64 { get; set; }
    }
    public class OrderFileGetResponse
    {
        public bool success { get; set; }
        public OrderFileResponse data { get; set; }

    }

    public class SavinoOTP
    {
        public int id { get; set; }
        public string otp { get; set; }
        public Int64 dueDate { get; set; }
        public string? phone { get; set; }
        public string? email { get; set; }
        public string? pec { get; set; }
    }
    public class SavinoFirma
    {
        public int id { get; set; }
        public int root { get; set; }
        public Int64 date { get; set; }
        public string? fullname { get; set; }
        public string? ip { get; set; }
    }
}
