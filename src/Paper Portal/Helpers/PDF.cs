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
            if (! validate(InputFile))
            {
                Error = "File couldn't be Validated!";
                return false;
            }
            if (! encrypt(InputFile.OpenReadStream(), FilePath))
            {
                Error = "File couldn't be Encrypted!";
                return false;
            }
            return true;
        }

        public Stream download(string filePath, string encKey)
        {
            
            return decrypt(filePath, encKey); ;
        }

        public bool Verify (string originalHash, string filePath)
        {
            var encrypt = new Encrypt();
            string fileHash = encrypt.ComputeHash(filePath);
            return encrypt.VerifyHash(originalHash, fileHash);
        }

        private bool validate(IFormFile file)
        {
            return true;
        }

        private bool encrypt (Stream input, string output)
        {
            Encrypt encrypt = new Encrypt();
            encrypt.EncryptFile(input, output);
            EncKey = encrypt.Password;
            Hash = encrypt.ComputeHash(output);
            return true;
        }
        
        private Stream decrypt (string input, string encKey)
        {
            var encrypt = new Encrypt();
            encrypt.Password = encKey;
            return encrypt.DecryptFile(input, encKey);
        }

        private void AddQRCode(string msg, Stream input, string output)
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
