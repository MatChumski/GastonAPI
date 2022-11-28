using GastonAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using System.Diagnostics.Contracts;
using System.Security.AccessControl;
using System.Security.Claims;

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
        public IActionResult ExpenseList()
        {
            List<Expense> expenses = new List<Expense>();

            var identity = HttpContext.User.Identity as ClaimsIdentity;

            var valid = OtherFunctions.ValidateToken(identity);

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
        public IActionResult ExpenseDet(int id)
        {
            Expense objExpense = _dbcontext.Expenses.Find(id);

            if (objExpense == null)
            {
                return NotFound(new { mensaje = "Data not found" });
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

        [HttpPost]
        [Route("SaveExpense")]
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
                Category newCat = _dbcontext.Categories.Find(objExpense.FkCategory);

                if (newCat == null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new { message = "Selected category doesn't exist for this user" });
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
        public IActionResult UpdateExpense([FromBody] Expense objExpense)
        {
            Expense _Expense = _dbcontext.Expenses.Find(objExpense.Id);

            if (_Expense == null)
            {
                return NotFound(new { mensaje = "Expense not Found" });
            }

            // Check user
            if (!string.IsNullOrEmpty(objExpense.FkUser.ToString()))
            {
                // Check user exists
                User expUser = _dbcontext.Users.Find(objExpense.FkUser);

                if (expUser == null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new { message = "User not found" });
                }

                _Expense.FkUser = objExpense.FkUser;
            }
            else
            {
                _Expense.FkUser = _Expense.FkUser;
            }

            _Expense.Amount = objExpense != null ? objExpense.Amount : _Expense.Amount;
            
            _Expense.Description = String.IsNullOrEmpty(objExpense.Description) ? objExpense.Description : _Expense.Description;

            _Expense.Type = String.IsNullOrEmpty(objExpense.Type) ? objExpense.Type : _Expense.Type;

            _Expense.Date = String.IsNullOrEmpty(objExpense.Date.ToString()) ? objExpense.Date : _Expense.Date;
            
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
                Category newCat = _dbcontext.Categories.Find(objExpense.FkCategory);

                if (newCat == null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new { message = "Selected category doesn't exist for this user" });
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
        public IActionResult DeleteExpense(int id)
        {
            Expense objExpense = _dbcontext.Expenses.Find(id);

            if (objExpense == null)
            {
                return NotFound(new { message = "Expense not found" });
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
