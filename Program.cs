using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.IO;
using System.Net;

using FlickrNet;

namespace FlickrDownload
{
    class Program
    {
        static void Main(string[] args)
        {
            StringBuilder errorLog = new StringBuilder();
            errorLog.AppendFormat("Starting Run at {0}\n", DateTime.Now.ToString());
            string baseDir = ConfigurationSettings.AppSettings["LocalDir"].ToString();
            Flickr flickr = new Flickr(ConfigurationSettings.AppSettings["APIKey"].ToString(), ConfigurationSettings.AppSettings["APISecret"].ToString());
            flickr.AuthToken = "";

            Auth auth = flickr.AuthCheckToken("");

            PhotoSearchOptions searchOptions = new PhotoSearchOptions();

            searchOptions.PrivacyFilter = PrivacyFilter.None;
            searchOptions.UserId = auth.User.UserId;
            searchOptions.PerPage = 500;

            PhotoCollection pics = new PhotoCollection();

            WebClient wget = new WebClient();
            searchOptions.Extras |= PhotoSearchExtras.DateTaken | PhotoSearchExtras.OriginalFormat | PhotoSearchExtras.OriginalUrl | PhotoSearchExtras.Description | PhotoSearchExtras.Tags;
            PhotoCollection rev = flickr.PhotosSearch(searchOptions);
         
            while (rev.Pages >= searchOptions.Page)
            
            {
                foreach(Photo pic in rev){
                        
                        //We have a picture.
                        string year = pic.DateTaken.Year.ToString("0000");
                        string month = pic.DateTaken.Month.ToString("00");
                        string day = pic.DateTaken.Day.ToString("00");
                        //  Does the directory Exist
                        //      No
                        string directory = String.Format("{0}{1}\\{2}\\{3}\\", baseDir, year, month, day);
                        try
                        {
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        string fileName = string.Format("{0}.{1}", pic.PhotoId, pic.OriginalFormat);
                        

                        string finalDestination = String.Format("{0}{1}", directory, fileName);
                        if (!File.Exists(finalDestination))
                        {
    
                            wget.DownloadFile(pic.OriginalUrl, finalDestination);
                            FileInfo fi = new FileInfo(finalDestination);
                            if (fi.Length < 10000)
                            {
                                //This shoudl catch file unavailable
                                throw new Exception("File size is too small");
                            }
                            
                            StreamWriter dataFile = new StreamWriter(String.Format("{0}{1}.xml", directory, fileName));
                            
                            StringBuilder meta = new StringBuilder();
                            meta.AppendFormat("<xml>\n\t<file>{0}</file>\n\t<title>{1}</title>\n\t<description>{2}</description>\n\t<dateTaken>{3}</dateTaken>\n\t<tags>", fileName, pic.Title, pic.Description, pic.DateTaken.ToString());
                            foreach(String tag in pic.Tags){
                                meta.AppendFormat("\n\t\t<tag>{0}</tag>", tag);                                   
                            }
                            meta.Append("\n\t</tags>\n</xml>");
                            dataFile.Write(meta.ToString());
                            dataFile.Close();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        errorLog.AppendFormat("{0}:{1}:{2}\n", directory, pic.PhotoId, ex.Message);
                    }
                }

                searchOptions.Page = rev.Page + 1;
                rev = flickr.PhotosSearch(searchOptions);
            }
            errorLog.AppendFormat("Finished Run at {0}\n", DateTime.Now.ToString());
            StreamWriter file = new StreamWriter(ConfigurationSettings.AppSettings["LocalDir"].ToString() + "/errors.log");
            file.Write(errorLog.ToString());
            file.Close();
        
        }
    }
}
