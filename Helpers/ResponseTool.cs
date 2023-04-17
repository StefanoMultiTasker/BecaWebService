using BecaWebService.Models.Communications;

namespace BecaWebService.Helpers
{
    public static class ResponseTool
    {
        public static GenericResponse toResponse(this object source)
        {
            return new GenericResponse(source);
        }
        public static GenericResponse toResponse(this List<object> source)
        {
            return new GenericResponse(source);
        }
        public static GenericResponse toResponse(this string source)
        {
            return new GenericResponse(source);
        }
    }
}
