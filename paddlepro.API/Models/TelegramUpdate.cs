/*using System.Text.Json.Serialization;*/
/**/
/*namespace paddlepro.API.Models;*/
/**/
/*public class TelegramUser*/
/*{*/
/*    public int Id { get; set; }*/
/*    public bool IsBot { get; set; }*/
/*    public string FirstName { get; set; }*/
/*    public string LastName { get; set; }*/
/*    public string Username { get; set; }*/
/*    public string Languagecode { get; set; }*/
/*}*/
/**/
/*public class TelegramGroup*/
/*{*/
/*    public int Id { get; set; }*/
/*    public string Title { get; set; }*/
/*    public string Type { get; set; }*/
/*    public bool AllMembersAreAdministrators { get; set; }*/
/*}*/
/**/
/*public class TelegramEntity*/
/*{*/
/*    public int Offset { get; set; }*/
/*    public int Length { get; set; }*/
/*    public string Type { get; set; }*/
/**/
/*}*/
/**/
/*public class TelegramMessage*/
/*{*/
/*    public int MessageId { get; set; }*/
/*    public TelegramUser From { get; set; }*/
/*    public TelegramGroup Chat { get; set; }*/
/*    public DateTime Date { get; set; }*/
/*    public string Text { get; set; }*/
/*    public TelegramEntity[] Entities { get; set; }*/
/*}*/
/**/
/*public class TelegramUpdate*/
/*{*/
/*    public int UpdateId { get; set; }*/
/*    public TelegramMessage Message { get; set; }*/
/*}*/
