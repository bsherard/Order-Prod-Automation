using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Net.Mail;
using System.IO;
using System.Net.Mime;


namespace OrderProductionHealthTest
{
    class Program
    {
        static void Main(string[] args)
        {
            MemoryStream ssStream = null;
            Exception error = null;
            String email = "qa_"+ DateTime.Now.ToUniversalTime().ToString(@"yyyy-MM-dd_HH-mm-ss-fff") +"@rhapsody.lan";
            FirefoxDriver d = null;
            try
            {
                d = new FirefoxDriver();
                d.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 10));
                d.Navigate().GoToUrl("https://order.rhapsody.com/checkout/coupon?code=RHPNOCTSTUS&email=" + email + "&cmpid=monitor");
                //d.Navigate().GoToUrl("https://order-int.internal.rhapsody.com/checkout/coupon?code=RHPNOCTSTUS&email=" + email + "&cmpid=monitor");
                d.FindElement(By.Id("password")).SendKeys("password");
                d.FindElement(By.Id("confirmpassword")).SendKeys("password");
                d.FindElement(By.Id("terms")).Click();
                d.FindElement(By.Id("accountsetupsubmit")).Click();

                String pageType = "";
                WebDriverWait wait = new WebDriverWait(d, TimeSpan.FromSeconds(10));
                wait.IgnoreExceptionTypes(new Type[] { typeof(NoSuchElementException), typeof(StaleElementReferenceException) });
                IWebElement metatag = wait.Until<IWebElement>((dr) =>
                {
                    IWebElement temp = dr.FindElement(By.XPath(@"/html/head/meta[@name='page-type']"));
                    return (temp.GetAttribute("content") == "receipt") ?  temp : null;
                });

                pageType = metatag.GetAttribute("content");
                if (pageType != "receipt")
                {
                    throw new Exception("Receipt page not found. Found pagetype value: " + pageType);
                }

               
            }
            catch (Exception e)
            {
                error = e;
                ssStream = new MemoryStream(d.GetScreenshot().AsByteArray);
            }

            try
            {
                if (d != null)
                {
                    d.Close();
                    d.Quit();
                }
            } 
            catch (Exception ex)
            {
                //do nothing
            }

            if (error != null)
            {
                SmtpClient client = new SmtpClient();
                client.Port = 587;
                client.Host = "smtp.gmail.com";
                client.EnableSsl = true;
                client.Timeout = 20000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential("RhapsodyOrderNocHealthCheck@gmail.com", "brainEcosystem");

                MailMessage message = new MailMessage();
                message.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                message.To.Add("bsherard@rhapsody.com");
                message.To.Add("rhapsody_noc@rhapsody.com");
                message.Subject = "Order production health check failed";
                message.From = new System.Net.Mail.MailAddress("RhapsodyOrderNocHealthCheck@gmail.com");
                message.Body =
@"Health check has failed on the order-test-1102.corp.rhapsody.com test machine.

attempted user: " + email + @"
path used: https://order.rhapsody.com/checkout/coupon
coupon code used: RHPNOCTSTUS

" + error.GetType().Name + @" log: 
" + error.ToString();

                Attachment att = new Attachment(ssStream, new ContentType(@"image/bmp"));
                att.Name = "screenshot";
                message.Attachments.Add(att);
                client.Send(message);
            }

            string exceptionMessage = (error == null ) ? "No Exception" : error.Message;

            using (StreamWriter writer = new StreamWriter("lastAutomationAttemptEmail.txt", false))
            {
                writer.WriteLine(email);
                writer.WriteLine(exceptionMessage);
            }
        }
    }
}