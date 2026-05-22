using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace YamyRestaurant.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult AutoLogin(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login");

                // Decode token
                var decodedBytes = Convert.FromBase64String(token);
                var decodedString = Encoding.UTF8.GetString(decodedBytes);

                // Split values
                var data = decodedString.Split('|');

                int userId = Convert.ToInt32(data[0]);
                string username = data[1];
                string database = data[2];

                // Store session
                HttpContext.Session.SetInt32("UserId", userId);
                HttpContext.Session.SetString("UserName", username);
                HttpContext.Session.SetString("DatabaseName", database);

                // redirect to home
                return RedirectToAction("Home", "Home");
            }
            catch
            {
                return RedirectToAction("Login");
            }
        }
    }
}
