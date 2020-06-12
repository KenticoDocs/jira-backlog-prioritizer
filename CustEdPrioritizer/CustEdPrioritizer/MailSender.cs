using System;
using System.Net;
using System.Net.Mail;

namespace CustEdPrioritizer
{
    static class MailSender
    {
        public static string SendMail(string addressFrom, string addressTo, string addressFromPassword, string subject, string body)
        {
            try
            {
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(addressFrom, addressFromPassword)
                };
                using (var message = new MailMessage(addressFrom, addressTo)
                {
                    IsBodyHtml = false,
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                return $"The email notification could NOT be sent due to the following error: {ex.Message} (Stack trace: {ex.StackTrace})";
            }

            return null;
        }
    }
}
