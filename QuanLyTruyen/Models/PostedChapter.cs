using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanLyTruyen.Models
{
    public class PostedChapter
    {
        public List<novel> ListNovel { get; set; }
        public HttpPostedFileBase Files { get; set; }

        public PostedChapter()
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            var list = dbcontext.novels.ToList();
            ListNovel = list;
        }
    }
}