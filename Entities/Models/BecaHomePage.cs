using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    [PrimaryKey(nameof(idProfile), nameof(rowPosition), nameof(colPosition))]
    public class BecaHomePage
    {
        public int idProfile { get; set; }
        public Int16 rowPosition { get; set; }
        public string? rowClass { get; set; }
        public Int16 colPosition { get; set; }
        public Int16 colDimension { get; set; }
        public string? colClass { get; set; }
        public string? colTitle { get; set; }
        public string colContentType { get; set; }
        public string? colContent { get; set; }
        public string? options { get; set; }
    }

    public class BecaHomePageResponse
    {
        public List<homeModelRow>? homeModelRow { get; set; }
    }

    public class homeModelRow
    {
        public Int16 position { get; set; }
        public string styleClass { get; set; }
        public List<homeModelColumn> columns { get; set; }
    }

    public class homeModelColumn
    {
        public Int16 position { get; set; }
        public Int16 colDimension { get; set; }
        public string styleClass { get; set; }
        public string title { get; set; }
        public string contentType { get; set; }
        public string content { get; set; }
        public string? options { get; set; }
    }
}
