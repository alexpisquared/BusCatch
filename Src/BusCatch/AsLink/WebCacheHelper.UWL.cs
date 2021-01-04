using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AsLink
{
    public class WebCacheHelper
    {
        static ApplicationDataContainer _lclStngs;
        static StorageFolder _lclFoldr;// = ApplicationData.Current.LocalFolder;

        static WebCacheHelper()
        {
            _lclStngs = ApplicationData.Current.LocalSettings;  // Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;      // Store and retrieve settings and other app data        https://msdn.microsoft.com/en-us/library/windows/apps/mt299098.aspx
            _lclFoldr = ApplicationData.Current.LocalFolder;
        }

        public static async Task<string> TryGetFromCache(string url)
        {
            var safeName = keyFromUrl(url);

            if (
              _lclStngs.Values[url] != null &&
              _lclStngs.Values[url] is long &&
              DateTime.Now.Ticks < ((long)_lclStngs.Values[url])
              )
            {
                //77 Debug.WriteLine("==>> In cache: key len: {0,3},    Key: '{1}',    ", safeName.Length, safeName);

                try
                {
                    StorageFile storageFile = await _lclFoldr.GetFileAsync(safeName);
                    return await FileIO.ReadTextAsync(storageFile);
                }
                catch (Exception ex) { ex.Pop("static#01"); }
            }

            return null;
        }
        public static string TryGetFromCacheSmall4kValue(string url)
        {
            var key = url; // keyFromUrl(url);
            var exp = key + "_exp";


            if (
              _lclStngs.Values.ContainsKey(key) &&
              _lclStngs.Values.ContainsKey(exp) &&
              _lclStngs.Values[exp] != null &&
              _lclStngs.Values[exp] is long &&
              DateTime.Now.Ticks < ((long)_lclStngs.Values[exp])
              )
                Debug.WriteLine("==>> In cache: key len: {0,3}, str len: {1,7:N0},    Key: '{2}',    val: '{3}...'", key.Length, (_lclStngs.Values[key] as string).Length, key, (_lclStngs.Values[key] as string).Substring(0, 16));

            //return _lclStngs.Values[key] as string;

            return null;
        }

        public static async Task PutToCache(string url, string val, DateTime expiresOn)
        {
            try
            {
                var key = keyFromUrl(url);
                var exp = url;

                //77 Debug.WriteLine("[[== Storing; key len: {0,3}, str len: {1,7:N0},    Key: '{2}',    val: '{3}...'==]]", key.Length, val.Length, key, val.Length > 16 ? val.Substring(0, 16) : val);

                _lclStngs.Values[exp] = expiresOn.Ticks;

                StorageFile sampleFile = await _lclFoldr.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(sampleFile, val);
            }
            catch (Exception ex) { ex.Pop("static#01"); }
        }
        public static void PutToCacheSmall4kValue(string url, string val, DateTime expiresOn)
        {
            try
            {
                var key = url; // keyFromUrl(url);
                var exp = key + "_exp";

                //77 Debug.WriteLine("[[== Storing; key len: {0,3}, str len: {1,7:N0},    Key: '{2}',    val: '{3}...'==]]", key.Length, val.Length, key, val.Substring(0, 16));

                _lclStngs.Values[exp] = expiresOn.Ticks;
                _lclStngs.Values[key] = val.Substring(0, 4000);
            }
            catch (Exception ex) { ex.Pop("static#01"); }
        }

        public static string keyFromUrl(string url)
        {
            //return url;

            var fn = url
              .Replace("-", "_")
              .Replace(".", "_")
              .Replace(",", "_")
              .Replace(";", "_")
              .Replace(":", "_")
              .Replace("?", "_")
              .Replace("!", "_")
              .Replace("&", "_")
              .Replace("=", "_")
              .Replace("~", "_")
              .Replace("!", "_")
              .Replace("@", "_")
              .Replace("#", "_")
              .Replace("$", "_")
              .Replace("%", "_")
              .Replace("^", "_")
              .Replace("&", "_")
              .Replace("(", "_")
              .Replace(")", "_")
              .Replace("+", "_")
              .Replace("`", "_")
              .Replace("'", "_")
              .Replace("/", "_")
              .Replace("|", "_")
              .Replace("*", "_")
              .Replace("\"", "_")
              .Replace("\\", "_")
              .Replace(":", "-")
              .Replace("http", "")
              .Replace("?", "!")
              .Replace("|", "!")
              .Replace("---", "");

            var l = 1000;
            if (fn.Length > l)
                fn = fn.Substring(fn.Length - l, l);

            return fn;
        }

    }
}
