using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;

namespace BanHammer
{
    class DB
    {
        public static void AdminAdd(string username)
        {
            using (var db = new DBcontext())
            {
                if(!db.Admins.Any(a => a.Username == username))
                {
                    db.Admins.Add(new Admin(username));
                    db.SaveChanges();
                }
            }
        }
        public static void AdminsRemove(string username)
        {
            using (var db = new DBcontext())
            {
                var a = from b in db.Admins where b.Username == username select b;
                try
                {
                    db.Admins.Remove(a.Single());
                    db.SaveChanges();
                }
                catch(Exception e) { }
            }
        }
        public static List<Admin> ReadAmdins()
        {
            List<Admin> list = new List<Admin>();
            using (var db = new DBcontext())
            {
                list = db.Admins.ToList();
            }
            return list;
        }

        public static void KeywordsAdd(string kwd)
        {
            using (var db = new DBcontext())
            {
                if (!db.Keywords.Any(a => a.keyword == kwd))
                {
                    db.Keywords.Add(new Keyword(kwd));
                    db.SaveChanges();
                }
            }
        }
        public static void KeywordsRemove(string kwd)
        {
            using (var db = new DBcontext())
            {
                var a = from b in db.Keywords where b.keyword == kwd select b;
                db.Keywords.Remove(a.Single());
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e) { }
            }
        }
        public static void KeywordsClear()
        {
            using (var db = new DBcontext())
            {
               foreach(var k in db.Keywords)
                {
                    db.Keywords.Remove(k);
                }
                db.SaveChanges();
            }
        }
        public static List<Keyword> ReadKeywords()
        {
            List<Keyword> list = new List<Keyword>();
            using (var db = new DBcontext())
            {
                list = db.Keywords.ToList();
            }
            return list;
        }

        public static void GKeywordsAdd(string kwd)
        {
            using (var db = new DBcontext())
            {
                if (!db.GKeywords.Any(a => a.keyword == kwd))
                {
                    db.GKeywords.Add(new GKeyword(kwd));
                    db.SaveChanges();
                }
            }
        }
        public static void GKeywordsRemove(string kwd)
        {
            using (var db = new DBcontext())
            {
                var a = from b in db.GKeywords where b.keyword == kwd select b;
                db.GKeywords.Remove(a.Single());
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e) { }
            }
        }
        public static void GKeywordsClear()
        {
            using (var db = new DBcontext())
            {
                foreach (var k in db.GKeywords)
                {
                    db.GKeywords.Remove(k);
                }
                db.SaveChanges();
            }
        }
        public static List<GKeyword> ReadGKeywords()
        {
            List<GKeyword> list = new List<GKeyword>();
            using (var db = new DBcontext())
            {
                list = db.GKeywords.ToList();
            }
            return list;
        }

        public static void WantedListAdd(string username)
        {
            using (var db = new DBcontext())
            {
                if (!db.WantedList.Any(a => a.Username == username))
                {
                    db.WantedList.Add(new Wantedlist(username));
                    db.SaveChanges();
                }
            }
        }
        public static void WantedListRemove(string username)
        {
            using (var db = new DBcontext())
            {
                var a = from b in db.WantedList where b.Username == username select b;
                db.WantedList.Remove(a.Single());
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e) { }
            }
        }
        public static void WantedListClear()
        {
            using (var db = new DBcontext())
            {
                foreach (var k in db.WantedList)
                {
                    db.WantedList.Remove(k);
                }
                db.SaveChanges();
            }
        }
        public static List<Wantedlist> ReadWantedList()
        {
            List<Wantedlist> list = new List<Wantedlist>();
            using (var db = new DBcontext())
            {
                list = db.WantedList.ToList();
            }
            return list;
        }

        public static void SpamersAdd(User user)
        {
            using (var db = new DBcontext())
            {
                if (!db.Spamers.Any(a => a.Id == user.Id))
                {
                    db.Spamers.Add(new Spamer(user));
                    db.SaveChanges();
                }
            }
        }
        public static void RemoveSpamer(User user)
        {
            using (var db = new DBcontext())
            {
                var a = from b in db.Spamers where b.Id == user.Id select b;
                db.Spamers.Remove(a.Single());
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e) { }
            }
        }
        public static void RemoveSpamer(string username)
        {
            using (var db = new DBcontext())
            {
                var a = from b in db.Spamers where b.Username == username select b;
                db.Spamers.Remove(a.Single());
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e) { }
            }
        }
        public static List<Spamer> ReadSpamers()
        {
            List<Spamer> list = new List<Spamer>();
            using (var db = new DBcontext())
            {
                list = db.Spamers.ToList();
            }
            return list;
        }

        public static void ChatsAdd(Chat chat)
        {
            using (var db = new DBcontext())
            {
                if (!db.Chats.Any(a => a.Id == chat.Id))
                {
                    db.Chats.Add(new MChat(chat));
                    db.SaveChanges();
                }
            }
        }
        public static void ChatsRemove(long chatid)
        {
            using (var db = new DBcontext())
            {
                var a = from b in db.Chats where b.Id == chatid select b;
                db.Chats.Remove(a.Single());
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e) { }
            }
        }
        public static List<MChat> ReadChats()
        {
            List<MChat> list = new List<MChat>();
            using (var db = new DBcontext())
            {
                list = db.Chats.ToList();
            }
            return list;
        }
    }
}
