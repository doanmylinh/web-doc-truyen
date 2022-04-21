using QuanLyTruyen.Models;
using System;
using System.Collections.Generic;
using System.Data.Linq.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace QuanLyTruyen.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            //Retrive Cookie and auto login:
            HttpCookie reqCookies = Request.Cookies["UserVal"];
            if (reqCookies != null)
            {
                string User_name = reqCookies["user"].ToString();
                string User_code = reqCookies["code"].ToString();

                string User = CryptoMethod.Instance.Decrypt(User_name);
                string Pass = CryptoMethod.Instance.Decrypt(User_code);
                if (Methods.Instance.CheckLogin(User, Pass))
                {
                    Session["ID"] = Methods.Instance.SHA256(User);
                    Session["userName"] = User;
                }
            }
            ViewBag.session = Session.SessionID;
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            List<novel> listnovel = (from ln in dbcontext.novels select ln).ToList();
            return View(listnovel);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult NewChapter()
        {
            PostedChapter pc = new PostedChapter();
            return View(pc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewChapter(int selectNovelID, string chapterName, HttpPostedFileBase Files, string chapContent)
        {
            PostedChapter pc = new PostedChapter();
            if (ModelState.IsValid)
            {
                NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
                novel lnv = dbcontext.novels.FirstOrDefault(n => n.NovelID == selectNovelID);
                if (lnv.NovelStatus.Value == 1)
                {
                    ViewBag.Message = "Novel is already complete.";
                    return View(pc);
                }
                var file = Files;
                if (file != null && file.ContentLength > 0)
                {
                    try
                    {
                        //init chapter to save
                        novelChapter nch = new novelChapter();
                        nch.ChapterName = chapterName;
                        nch.NovelID = selectNovelID;
                        var prevChap = dbcontext.novelChapters.Where(c => c.NovelID == selectNovelID).OrderByDescending(c => c.ChapterID).FirstOrDefault();
                        if (prevChap != null)  /*Có chapter trước => tạo reference*/
                        {
                            nch.PrevID = prevChap.ChapterID;
                        }

                        //save the file & update link
                        string ext = Path.GetExtension(file.FileName);
                        int id = dbcontext.novelChapters.Max(n => n.ChapterID) + 1;
                        string name = selectNovelID.ToString() + "_" + id.ToString() + ext;
                        if ((ext.ToLower() != ".txt") && (ext.ToLower() != ".text"))
                        {
                            ViewBag.Message = "File uploaded fail. Format is not supported.";
                        }
                        else
                        {
                            string path = Path.Combine(Server.MapPath("~/DATA"), name);
                            file.SaveAs(path);
                            nch.ChapterLink = name;
                            ViewBag.Message = "File saved Sucessfully! ";
                        }

                        //insert into database
                        dbcontext.GetTable<novelChapter>().InsertOnSubmit(nch);
                        dbcontext.SubmitChanges();
                        prevChap = dbcontext.novelChapters.FirstOrDefault(c => c.ChapterID == nch.PrevID);
                        var currChap = dbcontext.novelChapters.FirstOrDefault(c => c.ChapterID == nch.ChapterID);
                        prevChap.NextID = currChap.ChapterID;
                        dbcontext.SubmitChanges();
                    }
                    catch (Exception)
                    {
                        int id = dbcontext.novelChapters.Max(n => n.ChapterID) + 1;
                        string name = selectNovelID.ToString() + "_" + id.ToString() + ".txt";
                        string path = Path.Combine(Server.MapPath("~/DATA"), name);
                        using (FileStream fs = System.IO.File.Create(path))
                        {    
                            Byte[] content = new UTF8Encoding(true).GetBytes(chapContent);                            
                            fs.Write(content, 0, content.Length);
                        }
                        ViewBag.Message = "File created Sucessfully! ";
                    }
                }
                else
                {
                    ViewBag.Message = "You have not specified a file.";
                }
                return View(pc);
            }
            else
            {
                NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
                List<novel> listnovel = (from ln in dbcontext.novels select ln).ToList();
                return View("Index", listnovel);
            }
        }
        //-----------------------------------------------------------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string User, string Pass, bool remember = false)
        {
            if (Methods.Instance.CheckLogin(User, Pass))
            {
                if (remember)
                {
                    CreateCookie(User, Pass);
                }
                Session["ID"] = Methods.Instance.SHA256(User);
                Session["userName"] = User;
                NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
                List<novel> listnovel = (from ln in dbcontext.novels select ln).ToList();
                return View("Index", listnovel);
            }
            else
            {
                ViewBag.Message = "Sai thông tin đăng nhập, vui lòng nhập lại";
                return View();
            }
        }

        private void CreateCookie(string user, string pass)
        {
            //Create a cookie that last for a year.
            HttpCookie cookie = new HttpCookie("UserVal");
            cookie["user"] = CryptoMethod.Instance.Encrypt(user);
            cookie["code"] = CryptoMethod.Instance.Encrypt(pass);
            cookie.Expires = DateTime.Now.AddYears(1);

            //send the fking cookie to the client
            Response.Cookies.Add(cookie);
        }
        //-------------------------------------------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult ManageAccount(string username)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            member user = dbcontext.members.FirstOrDefault(m => m.username == username);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageAccount(int id, string oldPassword, string newPassword, string rePassword)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            var currMem = dbcontext.members.FirstOrDefault(m => m.MemberID == id && m.passcode == oldPassword);
            if (newPassword.Equals(rePassword))
            {
                if (currMem != null)
                {
                    currMem.passcode = newPassword;
                    dbcontext.SubmitChanges();
                    ViewBag.FlagControl = 1;
                    ViewBag.AlertPassword = "Đổi mật khẩu thành công.";
                    return View(currMem);
                }
                else
                {
                    ViewBag.FlagControl = 0;
                    ViewBag.AlertPassword = "Nhập sai mật khẩu: Mật khẩu cũ không trùng khớp.";
                    member user = dbcontext.members.FirstOrDefault(m => m.MemberID == id);
                    return View(user);
                }
            }
            else
            {
                ViewBag.FlagControl = 0;
                ViewBag.AlertPassword = "Nhập sai mật khẩu: Mật khẩu nhập lại không trùng khớp.";
                member user = dbcontext.members.FirstOrDefault(m => m.MemberID == id);
                return View(user);
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------

        public ActionResult Logout()
        {
            ClearCookie();
            Session.Clear();
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            List<novel> listnovel = (from ln in dbcontext.novels select ln).ToList();
            return View("Index", listnovel);
        }
        private void ClearCookie()
        {
            HttpCookie cookie = Request.Cookies["UserVal"];
            if (cookie != null)
            {
                cookie.Expires = DateTime.Now.AddSeconds(10);
                Response.SetCookie(cookie);
                Response.Flush();
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------------
        public ActionResult Series(int id = 0)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            Series model = new Series
            {
                Novel = (from lnv in dbcontext.novels where lnv.NovelID == id select lnv).FirstOrDefault(),
                listChap = (from nc in dbcontext.novelChapters where nc.NovelID == id select nc).ToList(),
                listTag = (from t in dbcontext.tags join nt in dbcontext.noveltags on t.TagID equals nt.TagID where nt.NovelID == id select t).ToList(),
            };
            return View(model);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------

        public string RemoveUnicode(string text)
        {
            string[] arr1 = new string[] {
                "á", "à", "ả", "ã", "ạ",
                "â", "ấ", "ầ", "ẩ", "ẫ", "ậ",
                "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
                "đ",
                "é","è","ẻ","ẽ","ẹ",
                "ê","ế","ề","ể","ễ","ệ",
                "í","ì","ỉ","ĩ","ị",
                "ó","ò","ỏ","õ","ọ",
                "ô","ố","ồ","ổ","ỗ","ộ",
                "ơ","ớ","ờ","ở","ỡ","ợ",
                "ú","ù","ủ","ũ","ụ",
                "ư","ứ","ừ","ử","ữ","ự",
                "ý","ỳ","ỷ","ỹ","ỵ",};
            string[] arr2 = new string[] {
                "a", "a", "a", "a", "a",
                "a", "a", "a", "a", "a", "a",
                "a", "a", "a", "a", "a", "a",
                "d",
                "e","e","e","e","e",
                "e","e","e","e","e","e",
                "i","i","i","i","i",
                "o","o","o","o","o",
                "o","o","o","o","o","o",
                "o","o","o","o","o","o",
                "u","u","u","u","u",
                "u","u","u","u","u","u",
                "y","y","y","y","y",};
            for (int i = 0; i < arr1.Length; i++)
            {
                text = text.Replace(arr1[i], arr2[i]);
                text = text.Replace(arr1[i].ToUpper(), arr2[i].ToUpper());
            }
            return text;
        }
        bool CheckContains(string[] param, string source)
        {
            bool result = true;
            foreach (var item in param)
            {
                if (!RemoveUnicode(source.Trim().ToLower()).Contains(RemoveUnicode(item.Trim().ToLower())))
                {
                    result = false;
                    return result;
                }
            }
            return result;
        }

        bool CheckTag(string[] param, noveltag[] source)
        {
            for (int i = 0; i < param.Length; i++)
            {
                bool flag = false;
                for (int j = 0; j < source.Length; j++)
                {
                    if (RemoveUnicode(source[j].tag.TagName.ToLower()).Contains(RemoveUnicode(param[i].Trim().ToLower())))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    return false;
                }
            }
            return true;
        }

        [HttpGet]

        public ActionResult Search(string searchtext)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            var novels = dbcontext.novels.ToList();
            if (searchtext == null)
            {
                searchtext = "";
                return View(novels);
            }

            string[] text = searchtext.Trim().Split(',');

            var lnv = novels.Where(l => CheckContains(text, l.NovelName)).ToList();
            var lnvtag = novels.Where(l => CheckTag(text, l.noveltags.ToArray()) && !lnv.Contains(l));
            lnv.AddRange(lnvtag);
            ViewBag.searchtext = searchtext;          
            
            return View(lnv);
        }
        //-----------------------------------------------------------------------------------------------------------------------------------

        //Menubar - Navigation Bar
        [ChildActionOnly]
        public ActionResult RenderMenu()
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            List<novel> listnovel = (from ln in dbcontext.novels select ln).ToList();
            return PartialView("_MenuBar", listnovel);
        }

        //Sitemap - Footer
        [ChildActionOnly]
        public ActionResult RenderFooter()
        {
            return PartialView("_Footer");
        }
        //-----------------------------------------------------------------------------------------------------------------------------------





        public ActionResult ViewChapter(int id = 1)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            novelChapter ch = dbcontext.novelChapters.FirstOrDefault(m => m.ChapterID == id);
            string path = Path.Combine(Server.MapPath("~/DATA"), ch.ChapterLink);

            if (Path.GetExtension(path).ToLower() == ".txt" || Path.GetExtension(path).ToLower() == ".text")
            {
                using (StreamReader reader = System.IO.File.OpenText(path))
                {
                    ViewBag.NovelBody = reader.ReadToEnd();
                }
            }
            else
            {
                if (Path.GetExtension(path).ToLower() == ".docx")
                {

                }
            }

            return View(ch);
        }
        //-----------------------------------------------------------------------------------------------------------------------------------


        public ActionResult EditChapter(int id)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            novelChapter ch = dbcontext.novelChapters.FirstOrDefault(m => m.ChapterID == id);
            return View(ch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditChapter(int id, string chapterName, HttpPostedFileBase content, string data)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            novelChapter ch = dbcontext.novelChapters.FirstOrDefault(m => m.ChapterID == id);
            string name = ch.NovelID + "_" + ch.ChapterID + Path.GetExtension(content.FileName);
            string path = Path.Combine(Server.MapPath(@"~/DATA"), name);
            if(content != null)
            {
                content.SaveAs(path);
                ch.ChapterLink = path;
                ch.ChapterName = chapterName;
            }
            else
            {
                path = Path.Combine(Server.MapPath(@"~/DATA"), ch.ChapterLink);
                using (FileStream fs = System.IO.File.Create(path))
                {
                    Byte[] filecontent = new UTF8Encoding(true).GetBytes(data);
                    fs.Write(filecontent, 0, filecontent.Length);
                }
            }
            dbcontext.SubmitChanges();

            return RedirectToAction("ViewChapter", new { id });
        }
        //-----------------------------------------------------------------------------------------------------------------------------------
        public ActionResult DeleteChapter(int id)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            //get the chapter
            novelChapter delChap = dbcontext.novelChapters.FirstOrDefault(nc => nc.ChapterID == id);

            //delete it
            if (delChap != null)
            {
                if (System.IO.File.Exists(delChap.ChapterLink))
                {
                    System.IO.File.Delete(delChap.ChapterLink);
                }
                dbcontext.GetTable<novelChapter>().DeleteOnSubmit(delChap);
                dbcontext.SubmitChanges();
            }

            //return to the series page
            Series model = new Series
            {
                Novel = (from lnv in dbcontext.novels where lnv.NovelID == id select lnv).FirstOrDefault(),
                listChap = (from nc in dbcontext.novelChapters where nc.NovelID == id select nc).ToList(),
                listTag = (from t in dbcontext.tags join nt in dbcontext.noveltags on t.TagID equals nt.TagID where nt.NovelID == id select t).ToList(),
            };
            return View("Series", model);
        }

        public ActionResult DeleteNovel(int id)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            //get the series
            novel delNovel = dbcontext.novels.FirstOrDefault(dn => dn.NovelID == id);
            //get the chapters associated with it
            List<novelChapter> delChaps = dbcontext.novelChapters.Where(dc => dc.NovelID == id).ToList();

            //delete all chapter associated with it, then delete it
            if (delNovel != null)
            {
                //delete the chapter
                foreach (var item in delChaps)
                {
                    if (System.IO.File.Exists(item.ChapterLink))
                    {
                        System.IO.File.Delete(item.ChapterLink);
                    }
                }
                dbcontext.GetTable<novelChapter>().DeleteAllOnSubmit(delChaps);
                dbcontext.SubmitChanges();

                //delete the novel
                dbcontext.GetTable<novel>().DeleteOnSubmit(delNovel);
                dbcontext.SubmitChanges();
            }

            //return to the index page
            List<novel> listnovel = (from ln in dbcontext.novels select ln).ToList();
            return View("Index", listnovel);
        }

        //------------------------------------------------------------------------------------------------------------------------

        public ActionResult NewSeries()
        {
            PostedSeries ps = new PostedSeries();
            return View(ps);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewSeries(PostedSeries m)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            novel n = new novel();
            //get name
            n.NovelName = m.NovelName;

            //get summary
            n.Synopsis = m.NovelSynopsis;

            //get the cover image
            if (m.NovelCover != null && m.NovelCover.ContentLength > 0)
            {
                Image img = Image.FromStream(m.NovelCover.InputStream);
                using (MemoryStream ms = new MemoryStream())
                {
                    img.Save(ms, img.RawFormat);
                    n.CoverImage = ms.ToArray();
                }
            }

            //set the status as "Ongoing"
            n.NovelStatus = 0;


            //insert into database and get the new ID:
            dbcontext.GetTable<novel>().InsertOnSubmit(n);
            dbcontext.SubmitChanges();
            novel newInsert = dbcontext.novels.Where(s => s.NovelName.Equals(n.NovelName)
                                                        && s.Synopsis.Equals(n.Synopsis)
                                                        && s.CoverImage.Equals(n.CoverImage)
                                                    ).OrderByDescending(s => s.NovelID).FirstOrDefault();


            //get the id tags and insert tags
            List<int> ListTagID = new List<int>();
            foreach (var item in m.NovelSelectTags)
            {
                if (item.Selected == true)
                {
                    int id = int.Parse(item.Value);
                    ListTagID.Add(id);
                }
            }

            foreach (var item in ListTagID)
            {
                noveltag nt = new noveltag();
                nt.NovelID = newInsert.NovelID;
                nt.TagID = item;
                dbcontext.GetTable<noveltag>().InsertOnSubmit(nt);
            }
            dbcontext.SubmitChanges();


            List<novel> listnovel = (from ln in dbcontext.novels select ln).ToList();
            return View("Index", listnovel);
        }
        //--------------------------------------------------------------------------------------------------------------------------
        public ActionResult AccountManager()
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            List<member> listMember = dbcontext.members.ToList();
            return View(listMember);
        }

        public ActionResult DeleteMember(int id)
        {
            List<member> listMember;
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            listMember = dbcontext.members.ToList();
            member delMem = listMember.FirstOrDefault(m => m.MemberID == id);
            dbcontext.GetTable<member>().DeleteOnSubmit(delMem);
            dbcontext.SubmitChanges();
            listMember = dbcontext.members.ToList();

            return View("AccountManager" ,listMember);
        }

        public ActionResult DowngradeMember(int id)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            member target = dbcontext.members.FirstOrDefault(m => m.MemberID == id);
            if (target.accounttype == 1) target.accounttype = 0;
            dbcontext.SubmitChanges();
            List<member> listMember = dbcontext.members.ToList();
            return View("AccountManager", listMember);
        }

        public ActionResult UpgradeMember(int id)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            member target = dbcontext.members.FirstOrDefault(m => m.MemberID == id);
            if (target.accounttype == 0) target.accounttype = 1;
            dbcontext.SubmitChanges();
            List<member> listMember = dbcontext.members.ToList();
            return View("AccountManager", listMember);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForcePassword(int id, string pass)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            member target = dbcontext.members.FirstOrDefault(m => m.MemberID == id);
            target.passcode = pass;
            dbcontext.SubmitChanges();
            List<member> listMember = dbcontext.members.ToList();
            return View("AccountManager", listMember);
        }

        //--------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddMember(string newuser, string newpass, int newtype)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            member newMem = new member()
            {
                username = newuser,
                passcode = newpass,
                accounttype = newtype,
            };
            dbcontext.GetTable<member>().InsertOnSubmit(newMem);
            dbcontext.SubmitChanges();
            List<member> listMember = dbcontext.members.ToList();
            return View("AccountManager", listMember);
        }

        //-------------------------------------------------------------------------------------------------------
        public ActionResult FinishNovel(int id)
        {
            NovelDataClassesDataContext dbcontext = new NovelDataClassesDataContext();
            novel lnv = dbcontext.novels.Single(l => l.NovelID == id);
            lnv.NovelStatus = 1;
            dbcontext.SubmitChanges();
            return RedirectToAction("Series", id);
        }
    }
}