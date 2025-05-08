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
        public string? colStyle { get; set; }
        public string? colTitle { get; set; }
        public string colContentType { get; set; }
        public string? colContent { get; set; }
        public string? colContentDefault { get; set; }
        public string? options { get; set; }
        public string? colIcon { get; set; }
        public string? colIconColor { get; set; }
        public string? colColor { get; set; }
        public string? colFontColor { get; set; }
        public string? colRedirect { get; set; }
        public Int32? sourceDB { get; set; }
        public string? ConnectionName { get; set; }
        public string? sourceSQL { get; set; }
        public string? sourceType { get; set; }
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
        public int? idHomeBrick { get; set; }
        public Int16 position { get; set; }
        public Int16 colDimension { get; set; }
        public string? styleClass { get; set; }
        public string? title { get; set; }
        public string contentType { get; set; }
        public string? content { get; set; }
        public string? options { get; set; }
        public string? icon { get; set; }
        public string? iconColor { get; set; }
        public string? color { get; set; }
        public string? fontColor { get; set; }
        public string? redirect { get; set; }
        public bool DataFromHome { get; set; }
    }


    [PrimaryKey(nameof(idProfile), nameof(rowPosition), nameof(colPosition))]
    public class BecaHomeBuild
    {
        public int idHome { get; set; }
        public int idHomeBrick { get; set; }
        public int idProfile { get; set; }
        public Int16 rowPosition { get; set; }
        public string? rowClass { get; set; }
        public Int16 colPosition { get; set; }
        public Int16 colDimension { get; set; }
        public string? colClass { get; set; }
        public string? colStyle { get; set; }
        public string? colTitle { get; set; }
        public string colContentType { get; set; }
        public string? colContent { get; set; }
        public string? colContentDefault { get; set; }
        public string? options { get; set; }
        public string? colIcon { get; set; }
        public string? colIconColor { get; set; }
        public string? colColor { get; set; }
        public string? colFontColor { get; set; }
        public string? colRedirect { get; set; }
        public Int32? sourceDB { get; set; }
        public string? ConnectionName { get; set; }
        public string? sourceSQL { get; set; }
        public string? sourceType { get; set; }
    }
}
