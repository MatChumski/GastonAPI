using GastonAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;
using System.Net.Mail;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace GastonAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        // OBLIGATORIO PARA TODOS LOS CONTROLADORES

        /*
         * Objeto de Contexto
         * Se crea una instancia del contexto de la BD
         */
        public readonly GastonDbContext _dbcontext;
        // Configuración para los usuarios (appsetings.json)
        public IConfiguration _configuration;

        /*
         * Cuando se llame el controlador, el constructor asigna
         * automáticamente el parámetro de contexto de la BD
         */
        public UserController(GastonDbContext _context, IConfiguration _config)
        {
            _dbcontext = _context;
            _configuration = _config;
        }

        [HttpGet]
        [Route("UserList")]
        public IActionResult UserList()
        {
            List<User> users = new List<User>();

            var identity = HttpContext.User.Identity as ClaimsIdentity;

            var valid = OtherFunctions.ValidateToken(identity);

            try
            {
                users = _dbcontext.Users.ToList();

                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = users });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Data not found"});
            }
        }

        [HttpGet]
        [Route("UserDet")]
        public IActionResult UserDet(int id)
        {
            User objUser = _dbcontext.Users.Find(id);

            if (objUser == null)
            {
                return NotFound(new { mensaje = "Data not found" });
            }

            try
            {
                objUser = _dbcontext.Users.Where(x => x.Id == id).FirstOrDefault();
                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = objUser});

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { messsage = "Data not found" });

            }
        }

        [HttpPost]
        [Route("SaveUser")]
        public IActionResult SaveUser([FromBody] User objUser)
        {
            // Check empty fields
            if (string.IsNullOrEmpty(objUser.Username) || string.IsNullOrEmpty(objUser.Password) || string.IsNullOrEmpty(objUser.Email))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Missing Required Fields" });
            }

            try
            {
                // Set creation date
                objUser.CreationDate = DateTime.Now;

                // Trim the strings
                objUser.Username = objUser.Username.Trim();

                objUser.Email = objUser.Email.Trim();
                
                if (objUser.Role != null)
                {
                    objUser.Role = objUser.Role.Trim();
                }

                objUser.Password = objUser.Password.Trim();

                // Check if valid Email
                try
                {
                    MailAddress m = new MailAddress(objUser.Email);
                }
                catch (FormatException)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new { message = "Invalid Email Address" });
                }

                // Check if Email already exists
                List<User> emailExists = _dbcontext.Users.Where(x => x.Email == objUser.Email).ToList();
                if (emailExists.Count > 0)
                {
                    return StatusCode(StatusCodes.Status409Conflict, new { message = "This E-Mail is already registered" });
                }

                // Check if valid role
                if ((objUser.Role != null) && (objUser.Role.ToLower() != "user" && objUser.Role.ToLower() != "admin"))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new { message = "Invalid Role" });
                }

                // Add to the database
                _dbcontext.Users.Add(objUser);
                var result = _dbcontext.SaveChanges();

                if (result > 0)
                {
                    Category noCategory = new Category();

                    noCategory.Name = "No Category";
                    noCategory.FkUser = objUser.Id;
                    noCategory.CreationDate = DateTime.Now;

                    _dbcontext.Categories.Add(noCategory);
                    _dbcontext.SaveChanges();

                    return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = "Data saved successfully" });
                }
                else
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't save data" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't save data" });

            }

        }


        [HttpPut]
        [Route("UpdateUser")]
        public IActionResult UpdateUser([FromBody] User objUser)
        {
            User _User = _dbcontext.Users.Find(objUser.Id);

            if (_User == null)
            {
                return NotFound(new { mensaje = "User not Found" });
            }

            try
            {
                _User.Username = string.IsNullOrEmpty(objUser.Username) ? _User.Username : objUser.Username;
                _User.Password = string.IsNullOrEmpty(objUser.Password) ? _User.Password : objUser.Password;
                _User.CreationDate = _User.CreationDate;

                // Check if valid Email
                if (!string.IsNullOrEmpty(objUser.Email))
                {
                    try
                    {
                        MailAddress m = new MailAddress(objUser.Email);
                    }
                    catch (FormatException)
                    {
                        return StatusCode(StatusCodes.Status400BadRequest, new { message = "Invalid Email Address" });
                    }

                    _User.Email = objUser.Email;
                }
                else
                {
                    _User.Email = _User.Email;
                }
                
                // Check if valid role
                if (!string.IsNullOrEmpty(objUser.Role))
                {
                    if (objUser.Role.ToLower() != "admin" && objUser.Role.ToLower() != "user")
                    {
                        return StatusCode(StatusCodes.Status400BadRequest, new { message = "Invalid Role" });
                    }
                    else
                    {
                        _User.Role = objUser.Role;
                    }
                }

                _dbcontext.Users.Update(_User);
                var result = _dbcontext.SaveChanges();

                return StatusCode(StatusCodes.Status200OK, new { mensaje = "OK", response = "Data saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { mensaje = "Couldn't save data" });

            }

        }

        [HttpDelete]
        [Route("DeleteUser")]
        public IActionResult DeleteUser(int id)
        {
            User ObjUser = _dbcontext.Users.Find(id);

            if (ObjUser == null)
            {
                return NotFound(new { mensaje = "User not found" });
            }

            List<Category> userCategories = _dbcontext.Categories.Where(x => x.FkUser == ObjUser.Id).ToList();
            List<Expense> userExpenses = _dbcontext.Expenses.Where(x => x.FkUser == ObjUser.Id).ToList();

            try
            {
                foreach(Expense expense in userExpenses)
                {
                    _dbcontext.Expenses.Remove(expense);
                }
                foreach(Category category in userCategories)
                {
                    _dbcontext.Categories.Remove(category);
                }

                _dbcontext.Users.Remove(ObjUser);
                _dbcontext.SaveChanges();
                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = "User and its dependencies deleted successfully" });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't delete any data" });

            }
        }
    }
}
