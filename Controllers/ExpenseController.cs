using GastonAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Newtonsoft.Json.Serialization;
using System.Diagnostics.Contracts;
using System.Security.AccessControl;
using System.Security.Claims;
using System.Security.Principal;

namespace GastonAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseController : ControllerBase
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
        public ExpenseController(GastonDbContext _context, IConfiguration _config)
        {
            _dbcontext = _context;
            _configuration = _config;
        }

        [HttpGet]
        [Route("ExpenseList")]
        [Authorize]
        public IActionResult ExpenseList()
        {
            List<Expense> expenses = new List<Expense>();

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            try
            {
                expenses = _dbcontext.Expenses.Include(x => x.FkUserNavigation).Include(x => x.FkCategoryNavigation).ToList();

                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = expenses });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Data not found" });
            }
        }

        [HttpGet]
        [Route("ExpenseDet")]
        [Authorize]
        public IActionResult ExpenseDet(int id)
        {
            Expense objExpense = _dbcontext.Expenses.Find(id);

            if (objExpense == null)
            {
                return NotFound(new { mensaje = "Data not found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, objExpense.FkUser.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            try
            {
                objExpense = _dbcontext.Expenses.Include(x => x.FkUserNavigation).Include(x => x.FkCategoryNavigation).Where(x => x.Id == id).FirstOrDefault();
                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = objExpense });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { messsage = "Data not found" });

            }
        }

        [HttpGet]
        [Route("ExpenseListUser")]
        [Authorize]
        public IActionResult ExpenseListUser(int userId)
        {

            User _User = _dbcontext.Users.Find(userId);

            if (_User == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "User not found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, _User.Id.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            List<Expense> expenses = new List<Expense>();

            try
            {
                expenses = _dbcontext.Expenses.Where(x => x.FkUser == _User.Id).ToList();

                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = expenses });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Data not found" });
            }
        }

        [HttpGet]
        [Route("ExpenseListCategory")]
        [Authorize]
        public IActionResult ExpenseListCategory(int catId)
        {

            Category _Category = _dbcontext.Categories.Find(catId);

            if (_Category == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Category not found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, _Category.FkUser.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            List<Expense> expenses = new List<Expense>();

            try
            {
                expenses = _dbcontext.Expenses.Where(x => x.FkCategory == _Category.Id).ToList();

                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = expenses });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Data not found" });
            }
        }

        [HttpGet]
        [Route("ExpenseListDates")]
        [Authorize]
        public IActionResult ExpenseListDates(int userId, DateTime startDate, DateTime endDate)
        {

            if (DateTime.Compare(startDate, endDate) > 0)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "StartDate later than EndDate" });
            }

            User _User = _dbcontext.Users.Find(userId);

            if (_User == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "User not found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, _User.Id.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            List<Expense> expenses = new List<Expense>();

            try
            {
                expenses = _dbcontext.Expenses.Where(x => x.FkUser == _User.Id && x.Date >= startDate && x.Date <= endDate).ToList();

                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = expenses });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Data not found" });
            }
        }

        [HttpGet]
        [Route("ExpenseListFilters")]
        [Authorize]
        public IActionResult ExpenseListFilters(int userId, int? catId, DateTime? startDate = null, DateTime? endDate = null)
        {
            User _User = _dbcontext.Users.Find(userId);

            if (_User == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "User not found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            // Check if the user is the requester
            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, _User.Id.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            // Check if the category exists for the user, and if the category belongs to the requester
            if (catId != null)
            {
                Category cat = _dbcontext.Categories.Where(x => x.Id == catId && x.FkUser == _User.Id).FirstOrDefault();

                if (cat == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "This category doesn't exist for this user" });
                }

                if (!isAdmin && !OtherFunctions.ValidateSelf(identity, cat.FkUser.ToString()))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
                }
            }

            if (startDate != null && endDate != null)
            {
                if (DateTime.Compare((DateTime)startDate, (DateTime)endDate) > 0)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new { message = "StartDate later than EndDate" });
                }
            }

            List<Expense> expenses = _dbcontext.Expenses.Where(x => x.FkUser == _User.Id).ToList();

            var result = expenses;

            try
            {
                if (catId != null)
                {
                    result = result.Where(x => x.FkCategory == catId).ToList();
                }

                if (startDate != null)
                {
                    result = result.Where(x => x.Date >= startDate).ToList();
                }

                if (endDate != null)
                {
                    result = result.Where(x => x.Date <= endDate).ToList();
                }
                
                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = result });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Data not found" });
            }
        }

        [HttpPost]
        [Route("SaveExpense")]
        [Authorize]
        public IActionResult SaveExpense([FromBody] Expense objExpense)
        {
            // Check user
            if (string.IsNullOrEmpty(objExpense.FkUser.ToString()))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Missing FkUser" });
            }

            // Check user exists
            User expUser = _dbcontext.Users.Find(objExpense.FkUser);

            if (expUser == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "User not found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, expUser.Id.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            // Check amount
            if (string.IsNullOrEmpty(objExpense.Amount.ToString()))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Missing Amount" });
            }
            // Check description
            if (string.IsNullOrEmpty(objExpense.Description.ToString()))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Missing Description" });
            }
            // Check type
            if (string.IsNullOrEmpty(objExpense.Type.ToString()))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Missing Type" });
            }
            // Check date
            if (string.IsNullOrEmpty(objExpense.Date.ToString()))
            {
                objExpense.Date = DateTime.Now;
            }

            // Check category
            // If empty, it will be added to "No Category"
            if (string.IsNullOrEmpty(objExpense.FkCategory.ToString()))
            {
                List<Category> noCats = _dbcontext.Categories.Where(x => x.Name == "No Category").ToList();
                Category noCategory = new Category();

                if (noCats.Count <= 0)
                {
                    try
                    {
                        noCategory.Name = "No Category";
                        noCategory.FkUser = objExpense.FkUser;
                        noCategory.CreationDate = DateTime.Now;

                        _dbcontext.Add(noCategory);
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(StatusCodes.Status404NotFound, new { message = "Failed creating 'No Category'" });
                    }
                }
                else
                {
                    noCategory = noCats[0];
                }

                objExpense.FkCategory = noCategory.Id;
            }
            else
            {
                Category newCat = _dbcontext.Categories.Where(x => x.FkUser == objExpense.FkUser && x.Id == objExpense.FkCategory).FirstOrDefault();

                if (newCat == null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new { message = "Selected category doesn't exist for this user" });
                }

                if (!isAdmin && !OtherFunctions.ValidateSelf(identity, newCat.FkUser.ToString()))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
                }
            }

            try
            {                
                _dbcontext.Expenses.Add(objExpense);

                var result = _dbcontext.SaveChanges();
                if (result > 0)
                    return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = "Expense created successfully" });
                else
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't save data" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't save data" });

            }

        }

        [HttpPut]
        [Route("UpdateExpense")]
        [Authorize]
        public IActionResult UpdateExpense([FromBody] Expense objExpense)
        {
            Expense _Expense = _dbcontext.Expenses.Find(objExpense.Id);

            // Check if expense exists
            if (_Expense == null)
            {
                return NotFound(new { mensaje = "Expense not Found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            // Check if the user is the owner of the category
            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, _Expense.FkUser.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            // Check new user
            if (!string.IsNullOrEmpty(objExpense.FkUser.ToString()))
            {
                // Check if the new user exists
                User expUser = _dbcontext.Users.Find(objExpense.FkUser);

                if (expUser == null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new { message = "User not found" });
                }

                // Check if the new user is the requester user
                if (!isAdmin && !OtherFunctions.ValidateSelf(identity, expUser.Id.ToString()))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
                }

                _Expense.FkUser = objExpense.FkUser;
            }
            else
            {
                _Expense.FkUser = _Expense.FkUser;
            }

            _Expense.Amount = objExpense != null ? objExpense.Amount : _Expense.Amount;
            
            _Expense.Description = String.IsNullOrEmpty(objExpense.Description) ? _Expense.Description : objExpense.Description;

            _Expense.Type = String.IsNullOrEmpty(objExpense.Type) ? _Expense.Type : objExpense.Type;

            _Expense.Date = String.IsNullOrEmpty(objExpense.Date.ToString()) ? _Expense.Date : objExpense.Date ;
            
            // Check category
            // If empty, it will be added to "No Category"
            if (string.IsNullOrEmpty(objExpense.FkCategory.ToString()))
            {
                List<Category> noCats = _dbcontext.Categories.Where(x => x.Name == "No Category").ToList();
                Category noCategory = new Category();

                if (noCats.Count <= 0)
                {
                    try
                    {
                        noCategory.Name = "No Category";
                        noCategory.FkUser = objExpense.FkUser;
                        noCategory.CreationDate = DateTime.Now;

                        _dbcontext.Add(noCategory);
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(StatusCodes.Status404NotFound, new { message = "Failed creating 'No Category'" });
                    }
                }
                else
                {
                    noCategory = noCats[0];
                }

                _Expense.FkCategory = noCategory.Id;
            }
            else
            {
                Category newCat = _dbcontext.Categories.Where(x => x.FkUser == objExpense.FkUser && x.Id == objExpense.FkCategory).FirstOrDefault();

                if (newCat == null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new { message = "Selected category doesn't exist for this user" });
                }

                // Check if the new category belongs to the user
                if (!isAdmin && !OtherFunctions.ValidateSelf(identity, newCat.FkUser.ToString()))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
                }

                _Expense.FkCategory = objExpense.FkCategory;
            }

            try
            {
                _dbcontext.Expenses.Update(_Expense);

                var result = _dbcontext.SaveChanges();
                if (result > 0)
                    return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = "Expense updated successfully" });
                else
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't save data" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't save data" });

            }

        }

        [HttpDelete]
        [Route("DeleteExpense")]
        [Authorize]
        public IActionResult DeleteExpense(int id)
        {
            Expense objExpense = _dbcontext.Expenses.Find(id);

            if (objExpense == null)
            {
                return NotFound(new { message = "Expense not found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, objExpense.FkUser.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            try
            {
                _dbcontext.Expenses.Remove(objExpense);
                _dbcontext.SaveChanges();
                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = "Expense deleted succesfully" });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't delete any data" });

            }
        }
    }
}
