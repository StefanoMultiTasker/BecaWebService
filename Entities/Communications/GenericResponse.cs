using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BecaWebService.Models.Communications
{
    public class GenericResponse : BaseResponse
    {
        public bool _result { get; private set; }
        public object _extraLoad { get; set; }

        private GenericResponse(bool success, string message, bool result) : base(success, message)
        {
            _result = result;
        }

        /// <summary>
        /// Creates a success response.
        /// </summary>
        /// <param name="dobaD">Saved dobaD.</param>
        /// <returns>Response.</returns>
        public GenericResponse(bool result) : this(true, string.Empty, result)
        { }

        public GenericResponse(object ExtraLoad) : this(true, string.Empty, true)
        { _extraLoad = ExtraLoad; }

        /// <summary>
        /// Creates am error response.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <returns>Response.</returns>
        public GenericResponse(string message) : this(false, message, false)
        { }
    }
}
