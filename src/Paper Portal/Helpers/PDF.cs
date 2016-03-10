using System;
using System.IO;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNet.Http;

namespace Paper_Portal.Helpers
{
    class PDF
    {
        public string Error { get; set; }
        public string EncKey { get; set; }
        public string Hash { get; set; }

        public bool upload(IFormFile InputFile, string FilePath)
        {
            if (!validate(InputFile))
            {
                Error = "File couldn't be Validated!";
                return false;
            }
            if (!encrypt(InputFile.OpenReadStream(), FilePath))
            {
                Error = "File couldn't be Encrypted!";
                return false;
            }
            return true;
        }

        public byte[] download(string filePath, string DownloaderID, string encKey = "")
        {
            EncKey = encKey;
            var decryptedStream = decrypt(filePath, EncKey);
            // Add QrCode and TimeStamp
            var fileContents = AddInfo(DownloaderID, decryptedStream);

            return fileContents;

            //return decryptedStream;
        }

        public bool Verify(string originalHash, string filePath)
        {
            var encrypt = new Encrypt();
            string fileHash = encrypt.ComputeHash(filePath);
            return encrypt.VerifyHash(originalHash, fileHash);
        }

        private bool validate(IFormFile file)
        {
            return true;
        }

        private bool encrypt(Stream input, string output)
        {
            Encrypt encrypt = new Encrypt();
            encrypt.EncryptFile(input, output);
            EncKey = encrypt.Password;
            Hash = encrypt.ComputeHash(output);
            return true;
        }

        private MemoryStream decrypt(string input, string encKey = "")
        {
            var encrypt = new Encrypt();
            encrypt.Password = encKey;
            return encrypt.DecryptFile(input);
        }

        private byte[] AddInfo(string msg, Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (PdfReader reader = new PdfReader(input))
                using (PdfStamper stamper = new PdfStamper(reader, ms))
                {
                    int Margin = 5;
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        Rectangle pageSize = reader.GetPageSize(i);
                        float x = pageSize.Width - Margin;
                        float y = 0 + Margin;
                        PdfContentByte ContentByte = stamper.GetOverContent(i);

                        AddQRCode(msg, ContentByte, x, y);
                        AddTimeStamp(ContentByte, x, y);
                    }
                }
                
                return ms.ToArray();
            }

        }
        private void AddQRCode(string msg, PdfContentByte ContentByte, float x, float y, int Margin = 10)
        {
            BarcodeQRCode qrcode = new BarcodeQRCode(msg, 1, 1, null);
            Image image = qrcode.GetImage();
            x = x - (image.Width + Margin);
            y = y + Margin;
            image.SetAbsolutePosition(x, y);
            ContentByte.AddImage(image);
        }
        private void AddTimeStamp(PdfContentByte ContentByte, float x, float y, int Margin = 5)
        {
            var timeStamp = DateTime.Now.ToString();

            // select the font properties
            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            ContentByte.SetColorFill(BaseColor.DARK_GRAY);
            ContentByte.SetFontAndSize(bf, 6);

            x = x - Margin;
            y = y + Margin;
            ContentByte.BeginText();
            ContentByte.ShowTextAligned(PdfContentByte.ALIGN_RIGHT, timeStamp, x, y, 0);
            ContentByte.EndText();
        }
    }

}
