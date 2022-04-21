using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanLyTruyen.Models
{
    public class PostedSeries
    {
        [Required]
        [DisplayName("Tên Novel Project: ")]
        public string NovelName { get; set; }

        [DisplayName("Tóm tắt nội dung: ")]
        public string NovelSynopsis { get; set; }

        [DisplayName("File ảnh bìa: ")]
        public HttpPostedFileBase NovelCover { get; set; }

        [DisplayName("Tag: ")]
        public List<SelectListItem> NovelSelectTags { get; set; }
        
        public PostedSeries()
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            List<tag> listTag = dbcontext.tags.ToList();
            NovelSelectTags = new List<SelectListItem>();
            foreach (var item in listTag)
            {
                SelectListItem listItem = new SelectListItem
                {
                    Text = item.TagName,
                    Value = item.TagID.ToString(),
                    Selected = false
                };
                NovelSelectTags.Add(listItem);
            }
        }
    }
}