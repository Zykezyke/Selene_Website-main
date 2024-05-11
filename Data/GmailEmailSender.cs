using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;

namespace SELENE_STUDIO.Data
{
    public class GmailEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Gmail SMTP settings
            var smtpClient = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("selenetestmail@gmail.com", "eufoptaprtdtlbjq")
            };

            // Email message
            var mailMessage = new MailMessage
            {
                From = new MailAddress("selenetestmail@gmail.com"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            // Send email asynchronously
            return smtpClient.SendMailAsync(mailMessage);
        }
    }
}
