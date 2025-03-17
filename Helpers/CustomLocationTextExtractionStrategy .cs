using System;
using System.Collections.Generic;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Pdf.Layer;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser.Data;

namespace BecaWebService.Helpers
{
    public class CustomLocationTextExtractionStrategy : LocationTextExtractionStrategy
    {
        private readonly List<TextLocation> textLocations = new List<TextLocation>();

        public override void EventOccurred(IEventData data, EventType type)
        {
            base.EventOccurred(data, type);

            if (type == EventType.RENDER_TEXT)
            {
                TextRenderInfo renderInfo = (TextRenderInfo)data;
                Vector start = renderInfo.GetBaseline().GetStartPoint();
                Vector end = renderInfo.GetBaseline().GetEndPoint();
                float x = start.Get(0);
                float y = start.Get(1);
                float width = end.Get(0) - x;
                float height = renderInfo.GetAscentLine().GetStartPoint().Get(1) - renderInfo.GetDescentLine().GetStartPoint().Get(1);

                textLocations.Add(new TextLocation(renderInfo.GetText(), new Rectangle(x, y, width, height)));
            }
        }

        public List<TextLocation> GetTextLocations()
        {
            return textLocations;
        }
    }

    public class TextLocation
    {
        public string Text { get; }
        public Rectangle Bounds { get; }

        public TextLocation(string text, Rectangle bounds)
        {
            Text = text;
            Bounds = bounds;
        }
    }
}
