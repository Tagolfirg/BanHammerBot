using System;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BanHammer
{
    public class DBcontext : DbContext
    {
        public DbSet<Spamer> Spamers { get; set; }
        public DbSet<MChat> Chats { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Keyword> Keywords { get; set; }
        public DbSet<GKeyword> GKeywords { get; set; }
        public DbSet<Wantedlist> WantedList { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=databasename.db");
        }
    }
    public class Spamer
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string LanguageCode { get; set; }
        public Spamer() { }
        public Spamer(User user)
        {
            this.Id = Int32.Parse(user.Id);
            this.FirstName = user.FirstName;
            this.LastName = user.LastName;
            this.Username = user.Username;
            this.LanguageCode = user.LanguageCode;
        }
    }
    public class MChat
    {
        public long Id { get; set; }
        public ChatType Type { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public MChat() { }
        public MChat(Chat chat)
        {
            this.Id = Int64.Parse(chat.Id);
            this.Type = chat.Type;
            this.Title = chat.Title;
            this.Username = chat.Username;
            this.FirstName = chat.FirstName;
            this.LastName = chat.LastName;
        }
    }
    public class Admin
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public Admin() { }
        public Admin(string usename)
        {
            this.Username = usename;
        }
    }
    public class Keyword
    {
        public int Id { get; set; }
        public string keyword { get; set; }
        public Keyword() { }
        public Keyword(string keyword)
        {
            this.keyword = keyword;
        }
    }
    public class GKeyword
    {
        public int Id { get; set; }
        public string keyword { get; set; }
        public GKeyword() { }
        public GKeyword(string keyword)
        {
            this.keyword = keyword;
        }
    }
    public class Wantedlist
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public Wantedlist() { }
        public Wantedlist(string username)
        {
            this.Username = username;
        }
    }
}
