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

        public byte[] download(string filePath, string DownloaderID, int downloads, string encKey = "")
        {
            EncKey = encKey;
            var decryptedStream = decrypt(filePath, EncKey);
            // Add QrCode and TimeStamp
            var fileContents = AddInfo(DownloaderID, downloads, decryptedStream);

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

        private byte[] AddInfo(string msg, int downloads, Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (PdfReader reader = new PdfReader(input))
                using (PdfStamper stamper = new PdfStamper(reader, ms))
                {
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        Rectangle pageSize = reader.GetPageSize(i);
                        
                        float Rightx = pageSize.Width;
                        float Righty = 0;

                        float Centerx = pageSize.Width / 2;
                        float Centery = 0;

                        float Leftx = 0;
                        float Lefty = 0;

                        PdfContentByte ContentByte = stamper.GetOverContent(i);

                        //var downloads = "# " + downloads.ToString();
                        AddQRCode(ContentByte, msg, Rightx, Righty, 3);
                        AddText(ContentByte, "# " + downloads.ToString(), Centerx, Centery, 3);
                        AddTimeStamp(ContentByte, Leftx, Lefty, 3);
                    }
                }
                
                return ms.ToArray();
            }

        }
        private void AddQRCode(PdfContentByte ContentByte, string msg, float x, float y, int Margin = 5)
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

            AddText(ContentByte, timeStamp, x, y, Margin);
        }
        private void AddText(PdfContentByte ContentByte, string msg, float x, float y, int Margin = 5)
        {
            // select the font properties
            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            ContentByte.SetColorFill(BaseColor.DARK_GRAY);
            ContentByte.SetFontAndSize(bf, 6);

            x = x + Margin;
            y = y + Margin;
            ContentByte.BeginText();
            ContentByte.ShowTextAligned(PdfContentByte.ALIGN_LEFT, msg, x, y, 0);
            ContentByte.EndText();
        }
    }

}
