using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNet.Http;

namespace Paper_Portal.Helpers
{
    class PDF
    {
        public static bool validate(IFormFile file)
        {
            return true;
        }

        public static void AddQRCode(string msg, Stream input, string output)
        {
            const int MARGIN = 5;

            //using (Stream inputPdfStream = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Stream outputPdfStream = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                PdfReader reader = new PdfReader(input);
                PdfStamper stamper = new PdfStamper(reader, outputPdfStream);

                BarcodeQRCode qrcode = new BarcodeQRCode(msg, 1, 1, null);
                Image image = qrcode.GetImage();

                stamper.SetEncryption(true, "12345", "asdf", 0);

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    Rectangle pageSize = reader.GetPageSize(i);
                    float x = 0 + MARGIN;
                    float y = pageSize.Height - (image.Height + MARGIN);
                    PdfContentByte pdfContentByte = stamper.GetOverContent(i);
    
                    image.SetAbsolutePosition(x, y);
                    pdfContentByte.AddImage(image);
                }
                
                stamper.Close();
            }
        }
    }
}
