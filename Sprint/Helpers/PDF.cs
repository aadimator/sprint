using System;
using System.IO;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Http;

namespace Sprint.Helpers
{
    class PDF
    {
        public string Error { get; set; }
        public string EncKey { get; set; }
        public string Hash { get; set; }

        public bool Upload(IFormFile InputFile, string FilePath)
        {
            // Validate if the file is in correct format
            if (!Validate(InputFile))
            {
                Error = "Only PDF files are supported as of now !";
                return false;
            }
            // Check if the file is encrypted or not
            if (!Encrypt(InputFile.OpenReadStream(), FilePath))
            {
                Error = "File couldn't be Encrypted!";
                return false;
            }

            return true; // everything went great
        }

        public byte[] Print (string filePath, string DownloaderID, string encKey = "")
        {
            return Download(filePath, DownloaderID, encKey, true);
        }

        public byte[] Download(string filePath, string DownloaderID, string encKey = "", bool print = false)
        {
            EncKey = (encKey != "") ? encKey : null;
            var decryptedStream = Decrypt(filePath, EncKey);

            // Add QrCode and TimeStamp
            var fileContents = AddInfo(DownloaderID, decryptedStream, print);
            return fileContents;
        }

        // Compute the Hash of the file and then compare it with the original one
        public bool Verify(string originalHash, string filePath)
        {
            var encrypt = new Encrypt();
            string fileHash = encrypt.ComputeHash(filePath);
            return encrypt.VerifyHash(originalHash, fileHash);
        }

        private bool Validate(IFormFile file)
        {
            // TODO: Add functionality like format conversion (docx -> pdf) etc
            if (file.ContentType.Equals("application/pdf"))
                return true;

            return false;
        }

        private bool Encrypt(Stream input, string output)
        {
            Encrypt encrypt = new Encrypt();
            encrypt.EncryptFile(input, output);
            EncKey = encrypt.Password;
            Hash = encrypt.ComputeHash(output);
            return true;
        }

        private MemoryStream Decrypt(string input, string encKey = "")
        {
            var encrypt = new Encrypt()
            {
                Password = encKey
            };
            return encrypt.DecryptFile(input);
        }

        private byte[] AddInfo(string msg, Stream input, bool print = false)
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

                        if (print)
                        {
                            // Insert JavaScript to print the document after a fraction of a second (allow time to become visible).
                            string jsText = "var res = app.setTimeOut('var pp = this.getPrintParams();pp.interactive = pp.constants.interactionLevel.full;this.print(pp);', 200);";
                            //string jsTextNoWait = "var pp = this.getPrintParams();pp.interactive = pp.constants.interactionLevel.full;this.print(pp);";
                            PdfAction js = PdfAction.JavaScript(jsText, stamper.Writer);
                            stamper.Writer.AddJavaScript(js);
                        }

                        //var downloads = "# " + downloads.ToString();
                        AddQRCode(ContentByte, msg, Rightx, Righty, 3);
                        //AddText(ContentByte, "# " + downloads.ToString(), Centerx, Centery, 3);
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
            var timeStamp = DateTime.Now.ToFileTimeUtc().ToString();

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
