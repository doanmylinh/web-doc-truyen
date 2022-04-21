using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanLyTruyen.Models
{
    public class Series
    {
        public novel Novel { get; set; }
        public List<novelChapter> listChap { get; set; }
        public List<tag> listTag { get; set; }
    }
}