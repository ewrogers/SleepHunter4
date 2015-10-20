using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SleepHunter.Updates
{
   public delegate void SoftwareDownloadProgressCallback(Uri uri, long bytesReceived, long totalBytesToReceive, double percentCompleted);

   public delegate void SoftwareDownloadErrorCallback(Uri uri, Exception ex);

   public sealed class SoftwareUpdateHelper
   {
      public static readonly Uri RepositoryUri = new Uri("http://sleephunter.dyndns.org/latest/");
      public static readonly Uri UpdateFileUri = new Uri(RepositoryUri, "SleepHunter.xml");

      public void DownloadSoftwareBundleAsync(Uri uri, SoftwareBundleCallback onCompleted, SoftwareDownloadProgressCallback onProgressChanged = null, SoftwareDownloadErrorCallback onError=null)
      {
         if (onCompleted == null)
            throw new ArgumentNullException("onCompleted");

         var client = new WebClient();

         client.DownloadDataCompleted += (sender, e) =>
         {
            try
            {
               if (e.Error != null && onError != null)
                  onError(uri, e.Error);

               if (e.Error == null && onCompleted != null)
               {
                  using (var memoryStream = new MemoryStream(e.Result, writable: false))
                  {
                     try
                     {
                        var bundle = SoftwareBundle.LoadFromStream(memoryStream);
                        onCompleted(bundle);
                     }
                     catch (Exception ex)
                     {
                        if (onError != null)
                           onError(uri, ex);
                     }
                  }
               }
            }
            finally { client.Dispose(); }            
         };

         client.DownloadProgressChanged += (sender, e) =>
         {
            if (onProgressChanged != null)
               onProgressChanged(uri, e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
         };

         client.DownloadDataAsync(uri);
      }
   }
}
