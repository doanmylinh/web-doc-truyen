using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace QuanLyTruyen.Models
{
    public class Methods
    {
        private static Methods instance;
        private Methods() { }

        public static Methods Instance {
            get {
                if (instance == null) instance = new Methods();
                return instance;
            } 
            private set => instance = value; 
        }

        public byte[] LogoBytesArray()
        {
            Image imageIn = Image.FromFile(Path.Combine(HttpRuntime.AppDomainAppPath, @"DATA\SouthCenterLogoVipProMark2.jpg"));
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, imageIn.RawFormat);
                return ms.ToArray();
            }
        }

        public byte[] GetNovelImageArray(novel lnv)
        {
            if (lnv.CoverImage == null)
            {
                Image imageIn = Image.FromFile(Path.Combine(HttpRuntime.AppDomainAppPath, @"DATA\CoverNotAvailable.gif"));
                using (var ms = new MemoryStream())
                {
                    imageIn.Save(ms, imageIn.RawFormat);
                    return ms.ToArray();
                }
            }
            return lnv.CoverImage.ToArray();
        }
        
        public string GetChapterContent(novelChapter ch)
        {
            string path = ch.ChapterLink;
            using (StreamReader reader = System.IO.File.OpenText(path))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }

        public string LimitString(string src, int at)
        {
            if(at <= 10 || at >= src.Length)
            {
                return src;
            }
            string result = src.Substring(0, at - 3) + "...";
            return result;
        }

        public bool CheckLogin(string User, string Pass)
        {
            if (User == null || User == string.Empty) return false;
            if (Pass == null || Pass == string.Empty) return false;
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            int loginNum = dbcontext.members.Where(m => m.username == User && m.passcode == Pass).Count();
            return loginNum == 1;
        }

        public string SHA256(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }

        public member GetMember(string username)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            return dbcontext.members.FirstOrDefault(m => m.username.Equals(username));
        }
    }
}