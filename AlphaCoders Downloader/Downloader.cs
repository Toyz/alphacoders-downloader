using System;
using System.IO;
using System.Net;

namespace AlphaCoders_Downloader
{
    public class WebDownloader
    {
        public delegate void DownloadProgressDelegate(int percProgress, int current, int total);

        public static void Download(string uri, string localPath, DownloadProgressDelegate progressDelegate)
        {
            long remoteSize;
            string fullLocalPath; // Full local path including file name if only directory was provided.

            try
            {
                /// Get the name of the remote file.
                Uri remoteUri = new Uri(uri);
                string fileName = Path.GetFileName(remoteUri.LocalPath);

                if (Path.GetFileName(localPath).Length == 0)
                    fullLocalPath = Path.Combine(localPath, fileName);
                else
                    fullLocalPath = localPath;

                /// Have to get size of remote object through the webrequest as not available on remote files,
                /// although it does work on local files.
                using (WebResponse response = WebRequest.Create(uri).GetResponse())
                using (Stream stream = response.GetResponseStream())
                    remoteSize = response.ContentLength;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Error connecting to URI (Exception={0})", ex.Message), ex);
            }

            int bytesRead = 0, bytesReadTotal = 0;

            try
            {
                using (WebClient client = new WebClient())
                using (Stream streamRemote = client.OpenRead(new Uri(uri)))
                using (Stream streamLocal = new FileStream(fullLocalPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] byteBuffer = new byte[1024 * 1024 * 2]; // 2 meg buffer although in testing only got to 10k max usage.
                    int perc = 0;
                    while ((bytesRead = streamRemote.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                    {
                        bytesReadTotal += bytesRead;
                        streamLocal.Write(byteBuffer, 0, bytesRead);
                        int newPerc = (int)((double)bytesReadTotal / (double)remoteSize * 100);
                        if (newPerc > perc)
                        {
                            perc = newPerc;
                            if (progressDelegate != null)
                                progressDelegate(perc, bytesReadTotal, (int)remoteSize);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Error downloading file (Exception={0})", ex.Message), ex);
            }

        }
    }
}
