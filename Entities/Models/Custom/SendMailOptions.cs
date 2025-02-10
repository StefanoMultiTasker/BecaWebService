using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models.Custom
{
    public class SendMailOptions
    {
        public SendMailOptionsOrigin Sender { get; set; }
        public SendMailOptionsOrigin Dest { get; set; }
        public SendMailOptionsOrigin Subject { get; set; }
        public SendMailOptionsOrigin Text { get; set; }
    }

    public class SendMailOptionsOrigin
    {
        public string Type { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
    }
}
