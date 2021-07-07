using System;
using Microsoft.AspNetCore.Mvc;
using CoreBot.DBClass;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CoreBot.Controllers
{
    [Route("api/events/[action]")]
    [ApiController]
    public class AccountController : Controller
    {
        [HttpGet]
        public string Index()
        {
            Console.WriteLine("Info: Success at Index");
            return "Success at Index.";
        }

        [Route("{userid}")]
        public string Verify([FromRoute]string userid)
        {
            Console.WriteLine($"Info: Verify caught for {userid}");
            AccountManager.VerifyUserEmailById(userid);
            return $"Verify caught for {userid}";
        }

        [Route("{subscriptionid}")]
        public string Unsubscribe([FromRoute]string subscriptionid)
        {
            
            Console.WriteLine($"Info: Unsubscribe caught for {subscriptionid}");
            AccountManager.RemoveSubscriptionById(subscriptionid);
            return $"Unsubscribed {subscriptionid}";
        }
    }
}
