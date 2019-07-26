using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas;
using Windows.UI.Xaml.Media;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using Windows.Storage;
using System.Diagnostics;
using System.Collections.Specialized;

namespace Brain_uwp.Utils
{
    class Utils
    {
        public static byte[] CropPicture(byte[] arr, int width, int height, int xPos, int yPos, int xw, int yh)
        {
            byte[] result = new byte[xw * yh];

            for (int y = yPos; y < yPos + yh; y++)
            {
                for (int x = xPos; x < xPos + xw; x++)
                {
                    result[(x - xPos) + (y - yPos) * yh] = arr[x + y * width];
                }
            }

            return result;
        }
        public static void SendMessage(object msg)
        {
            byte[] message = (byte[])msg;
            TcpClient tcpClient;
            byte[] buffer = new byte[256];

            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1337));

                try
                {
                    Console.WriteLine("Sending message...");
                    Send(tcpClient.Client, message, 0, message.Length, 5000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    Console.WriteLine("Conexión a servidor rehusada. Puede que el servidor no esté disponible.");
                }
                else
                {
                    Console.WriteLine("Socket error:" + e);
                }
            }
        }

        private static void Send(Socket socket, byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount;
            int sent = 0;  // how many bytes is already sent

            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                    throw new Exception("Timeout.");
                try
                {
                    sent += socket.Send(buffer, offset + sent, size - sent, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably full, wait and try again
                        Thread.Sleep(30);
                    }
                    else
                        throw ex;  // any serious error occurr
                }
            } while (sent < size);
        }
        public static async Task<ImageSource> SaveToImageSource(byte[] imageBuffer)
        {
            ImageSource imageSource = null;
            using (MemoryStream stream = new MemoryStream(imageBuffer))
            {
                var ras = stream.AsRandomAccessStream();
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.PngDecoderId, ras);
                var provider = await decoder.GetPixelDataAsync();
                byte[] buffer = provider.DetachPixelData();
                WriteableBitmap bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                await bitmap.PixelBuffer.AsStream().WriteAsync(buffer, 0, buffer.Length);
                imageSource = bitmap;
            }
            return imageSource;
        }

        public static async Task<SoftwareBitmap> ResizeSoftwareBitmap(SoftwareBitmap softwareBitmap, double scaleFactor)
        {
            var resourceCreator = CanvasDevice.GetSharedDevice();
            var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(resourceCreator, softwareBitmap);
            var canvasRenderTarget = new CanvasRenderTarget(resourceCreator, (int)(softwareBitmap.PixelWidth * scaleFactor), (int)(softwareBitmap.PixelHeight * scaleFactor), 96);

            using (var cds = canvasRenderTarget.CreateDrawingSession())
            {
                cds.DrawImage(canvasBitmap, canvasRenderTarget.Bounds);
            }

            var pixelBytes = canvasRenderTarget.GetPixelBytes();

            var writeableBitmap = new WriteableBitmap((int)(softwareBitmap.PixelWidth * scaleFactor), (int)(softwareBitmap.PixelHeight * scaleFactor));
            using (var stream = writeableBitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixelBytes, 0, pixelBytes.Length);
            }

            var scaledSoftwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Rgba16, (int)(softwareBitmap.PixelWidth * scaleFactor), (int)(softwareBitmap.PixelHeight * scaleFactor));
            scaledSoftwareBitmap.CopyFromBuffer(writeableBitmap.PixelBuffer);

            return scaledSoftwareBitmap;
        }

        public async static Task<byte[]> ImageToBytes(BitmapImage image)
        {
            RandomAccessStreamReference streamRef = RandomAccessStreamReference.CreateFromUri(image.UriSource);
            IRandomAccessStreamWithContentType streamWithContent = await streamRef.OpenReadAsync();
            byte[] buffer = new byte[streamWithContent.Size];
            await streamWithContent.ReadAsync(buffer.AsBuffer(), (uint)streamWithContent.Size, InputStreamOptions.None);
            return buffer;
        }

        public static async Task<byte[]> ResizeImage(byte[] imageData, int reqWidth, int reqHeight)
        {
            System.Diagnostics.Debug.WriteLine("1");
            var memStream = new MemoryStream(imageData);

            System.Diagnostics.Debug.WriteLine("2");
            IRandomAccessStream imageStream = memStream.AsRandomAccessStream();
            System.Diagnostics.Debug.WriteLine("3");
            var decoder = await BitmapDecoder.CreateAsync(imageStream);
            System.Diagnostics.Debug.WriteLine("4");
            if (decoder.PixelHeight > reqHeight || decoder.PixelWidth > reqWidth)
            {
            System.Diagnostics.Debug.WriteLine("5");
                using (imageStream)
                {
            System.Diagnostics.Debug.WriteLine("6");
                    var resizedStream = new InMemoryRandomAccessStream();

            System.Diagnostics.Debug.WriteLine("7");
                    BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                    double widthRatio = (double)reqWidth / decoder.PixelWidth;
                    double heightRatio = (double)reqHeight / decoder.PixelHeight;

                    double scaleRatio = Math.Min(widthRatio, heightRatio);

                    if (reqWidth == 0)
                        scaleRatio = heightRatio;

                    if (reqHeight == 0)
                        scaleRatio = widthRatio;

                    uint aspectHeight = (uint)Math.Floor(decoder.PixelHeight * scaleRatio);
                    uint aspectWidth = (uint)Math.Floor(decoder.PixelWidth * scaleRatio);

                    encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;

                    encoder.BitmapTransform.ScaledHeight = aspectHeight;
                    encoder.BitmapTransform.ScaledWidth = aspectWidth;

            System.Diagnostics.Debug.WriteLine("8");
                    await encoder.FlushAsync();
            System.Diagnostics.Debug.WriteLine("9");
                    resizedStream.Seek(0);
            System.Diagnostics.Debug.WriteLine("10");
                    var outBuffer = new byte[resizedStream.Size];
            System.Diagnostics.Debug.WriteLine("11");
                    uint x = await resizedStream.WriteAsync(outBuffer.AsBuffer());
                    return outBuffer;
                }
            }
            return imageData;
        }

        public static async Task<byte[]> TranscodeImageFile(WriteableBitmap wb)
        {
            using (var ras = wb.PixelBuffer.AsStream().AsRandomAccessStream())
            {
                System.Diagnostics.Debug.WriteLine("1");
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(ras);

                System.Diagnostics.Debug.WriteLine("2");
                var memStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                System.Diagnostics.Debug.WriteLine("3");
                BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(memStream, decoder);

                encoder.BitmapTransform.ScaledWidth = 320;
                encoder.BitmapTransform.ScaledHeight = 240;

                System.Diagnostics.Debug.WriteLine("4");
                await encoder.FlushAsync();

                System.Diagnostics.Debug.WriteLine("5");
                var outbuff = new byte[memStream.Size];
                System.Diagnostics.Debug.WriteLine("6");
                await memStream.WriteAsync(outbuff.AsBuffer());
                System.Diagnostics.Debug.WriteLine("7");
                return outbuff;
            }
        }


        public static async Task<byte[]> TranscodeImageFile(byte[] data)
        {
            using (IRandomAccessStream stream = data.AsBuffer().AsStream().AsRandomAccessStream())
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                var memStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                var outstream = new InMemoryRandomAccessStream();
                BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(memStream, decoder);

                encoder.BitmapTransform.ScaledWidth = 320;
                encoder.BitmapTransform.ScaledHeight = 240;

                await encoder.FlushAsync();

                memStream.Seek(0);
                await RandomAccessStream.CopyAsync(memStream, outstream);
                memStream.Dispose();

                var outbuff = new byte[outstream.Size];
                await outstream.WriteAsync(outbuff.AsBuffer());
                return outbuff;
            }
        }

        private static string HttpUploadFile(string url, StorageFile file, string paramName, string contentType, NameValueCollection nvc)
        {
            string result = "";
            Debug.WriteLine(string.Format("Uploading {0} to {1}", file.Path, url));
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

            Stream rs = wr.GetRequestStream();

            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, file.Name, contentType);
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

            int numofretries = 16;

            for (int i = 1; i <= numofretries; ++i)
            {
                try
                {
                    FileStream fileStream = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    byte[] buffer = new byte[4096];
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        for (int j = 1; j <= 5; j++)
                        {
                            try
                            {
                                rs.Write(buffer, 0, bytesRead);
                                break;
                            }
                            catch (OutOfMemoryException e)
                            {
                                Thread.Sleep(1000 * j);
                                Debug.WriteLine("Memery Aceeption Write " + i);
                            }
                        }
                    }
                    fileStream.Close();
                    break; // When done we can break loop
                }
                catch (IOException e) when (i <= numofretries)
                {
                    Debug.WriteLine("Access to file stream failed, trying again in " + 1000 * i + " second, Currently " + i + " times tried");
                    Thread.Sleep(1000 * i);
                }
            }

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                result = reader2.ReadToEnd();
                Debug.WriteLine(string.Format("File uploaded {0}, server response is: {1}", url, result));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error uploading file", ex);
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
            }
            finally
            {
                wr = null;
            }

            return result;
        }

    }
}
