using GastonAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Principal;

namespace GastonAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
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
        public CategoryController(GastonDbContext _context, IConfiguration _config)
        {
            _dbcontext = _context;
            _configuration = _config;
        }

        [HttpGet]
        [Route("CategoryList")]
        [Authorize]
        public IActionResult CategoryList()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            List<Category> categories = new List<Category>();

            try
            {
                categories = _dbcontext.Categories.Include(x => x.FkUserNavigation).ToList();

                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = categories });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Data not found" });
            }
        }

        [HttpGet]
        [Route("CategoryListUser")]
        [Authorize]
        public IActionResult CategoryListUser(int userId)
        {
            User objUser = _dbcontext.Users.Find(userId);

            if (objUser == null)
            {
                return NotFound(new { mensaje = "Data not found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, objUser.Id.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            List<Category> userCategories = _dbcontext.Categories.Where(x => x.FkUser == objUser.Id).ToList();

            if (userCategories.Count <= 0)
            {
                return NotFound(new { mensaje = "Data not found" });
            }
            else
            {
                try
                {
                    return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = userCategories });

                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { messsage = "Data not found" });

                }
            }


        }

        [HttpGet]
        [Route("CategoryDet")]
        [Authorize]
        public IActionResult CategoryDet(int id)
        {
            Category objCategory = _dbcontext.Categories.Find(id);

            if (objCategory == null)
            {
                return NotFound(new { mensaje = "Data not found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, objCategory.FkUser.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            try
            {
                objCategory = _dbcontext.Categories.Include(x => x.FkUserNavigation).Where(x => x.Id == id).FirstOrDefault();
                return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = objCategory });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { messsage = "Data not found" });

            }
        }

        [HttpGet]
        [Route("CategoryAmount")]
        [Authorize]
        public IActionResult CategoryAmount(int id)
        {
            Category objCategory = _dbcontext.Categories.Find(id);

            if (objCategory == null)
            {
                return NotFound(new { mensaje = "Data not found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, objCategory.FkUser.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            List<Expense> expenseList = _dbcontext.Expenses.Where(x => x.FkCategory == objCategory.Id).ToList();

            if (expenseList.Count <= 0)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { messsage = "This category has no expenses" });
            }

            double? totalAmount = 0;

            foreach (Expense expense in expenseList)
            {
                totalAmount += expense.Amount;
            }

            return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = new { category = objCategory, totalAmount = totalAmount } });

            
        }

        [HttpPost]
        [Route("SaveCategory")]
        [Authorize]
        public IActionResult SaveCategory([FromBody] Category objCategory)
        {
            if (string.IsNullOrEmpty(objCategory.Name) || string.IsNullOrEmpty(objCategory.FkUser.ToString()))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Missing Required Fields" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, objCategory.FkUser.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            try
            {
                // Check if user exists
                User objUser = _dbcontext.Users.Find(objCategory.FkUser);

                if (objUser == null)
                {
                    return NotFound(new { mensaje = "Linked user not found" });
                }

                // Check if there is already a category with the same name for the user
                objCategory.Name = objCategory.Name.Trim();

                List<Category> exists = _dbcontext.Categories.Where(x => x.Name == objCategory.Name && x.FkUser == objCategory.FkUser).ToList();
                if (exists.Count > 0)
                {
                    return StatusCode(StatusCodes.Status409Conflict, new { message = "There is already a category with name " + objCategory.Name + " for user " + objCategory.FkUser });
                }

                // Set creation date
                objCategory.CreationDate = DateTime.Now;

                _dbcontext.Categories.Add(objCategory);

                var result = _dbcontext.SaveChanges();
                if (result > 0)
                    return StatusCode(StatusCodes.Status200OK, new { message = "OK", response = "Category created successfully" });
                else
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't save data" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't save data" });

            }

        }

        
        [HttpPut]
        [Route("UpdateCategory")]
        [Authorize]
        public IActionResult UpdateCategory([FromBody] Category objCategory)
        {
            Category _Category = _dbcontext.Categories.Find(objCategory.Id);

            if (_Category == null)
            {
                return NotFound(new { mensaje = "Category not Found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            // Does the editing category not belong to the user or is it being moved to a different user?
            if (!isAdmin && 
                (!OtherFunctions.ValidateSelf(identity, _Category.FkUser.ToString()) || 
                (objCategory.FkUser != null && !OtherFunctions.ValidateSelf(identity, objCategory.FkUser.ToString()))))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            try
            {
                // Check if user exists
                User objUser;
                if (objCategory.FkUser != null)
                {
                    objUser = _dbcontext.Users.Find(objCategory.FkUser);

                    if (objUser == null)
                    {
                        return NotFound(new { mensaje = "Linked user not found" });
                    }
                }

                // Check if there is already a category with the same name for the user
                if (!string.IsNullOrEmpty(objCategory.Name))
                {
                    objCategory.Name = objCategory.Name.Trim();

                    List<Category> exists = _dbcontext.Categories.Where(x => x.Name == objCategory.Name && x.FkUser == objCategory.FkUser).ToList();
                    if (exists.Count > 0)
                    {
                        return StatusCode(StatusCodes.Status409Conflict, new { message = "There is already a category with name " + objCategory.Name + " for user " + objCategory.FkUser });
                    }
                }

                _Category.Name = !string.IsNullOrEmpty(objCategory.Name) ? objCategory.Name : _Category.Name;
                _Category.FkUser = objCategory.FkUser != null ? objCategory.FkUser : _Category.FkUser;
                _Category.CreationDate = _Category.CreationDate;

                _dbcontext.Categories.Update(_Category);
                var result = _dbcontext.SaveChanges();

                return StatusCode(StatusCodes.Status200OK, new { mensaje = "OK", response = "Data saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't save data" });

            }

        }

        [HttpDelete]
        [Route("DeleteCategory")]
        [Authorize]
        public IActionResult DeleteCategory(int id, string? mode, int? moveTo)
        {
            Category objCategory = _dbcontext.Categories.Find(id);

            if (objCategory == null)
            {
                return NotFound(new { message = "Category not found" });
            }

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            bool isAdmin = OtherFunctions.ValidateAdmin(identity);

            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, objCategory.FkUser.ToString()))
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
            }

            if (objCategory.Name == "No Category")
            {
                return BadRequest(new { message = "This category can't be deleted"});
            }

            if (mode == null)
            {
                mode = "nocategory";
            }

            try
            {
                switch (mode)
                {
                    case "move":
                        {
                            if (moveTo == null)
                            {
                                return StatusCode(StatusCodes.Status400BadRequest,  new { message = "Category to move not specified" });
                            }

                            Category toMove = _dbcontext.Categories.Find(moveTo);

                            if (toMove == null)
                            {
                                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Category to move not found" });
                            }

                            if (!isAdmin && !OtherFunctions.ValidateSelf(identity, toMove.FkUser.ToString()))
                            {
                                return StatusCode(StatusCodes.Status401Unauthorized, new { message = "You are not authorized for this operation" });
                            }

                            List<Expense> catExpenses = _dbcontext.Expenses.Where(x => x.FkCategory == objCategory.Id && x.FkUser == objCategory.FkUser).ToList();
                            foreach (Expense expense in catExpenses)
                            {
                                expense.FkCategory = toMove.Id;

                                _dbcontext.Update(expense);
                            }

                            _dbcontext.Categories.Remove(objCategory);
                            _dbcontext.SaveChanges();

                            return StatusCode(StatusCodes.Status200OK, new { message = "Category deleted and expenses moved" });
                        }

                    case "nocategory":
                        {
                            List<Expense> catExpenses = _dbcontext.Expenses.Where(x => x.FkCategory == objCategory.Id && x.FkUser == objCategory.FkUser).ToList();

                            List<Category> noCategoryList = _dbcontext.Categories.Where(x => x.Name == "No Category" && x.FkUser == objCategory.FkUser).ToList();
                            Category noCategory = new Category();

                            if (noCategoryList.Count <= 0)
                            {
                                noCategory.Name = "No Category";
                                noCategory.FkUser = objCategory.FkUser;
                                noCategory.CreationDate = DateTime.Now;

                                _dbcontext.Categories.Add(noCategory);
                            }
                            else
                            {
                                noCategory = noCategoryList[0];
                            }

                            foreach(Expense expense in catExpenses)
                            {
                                expense.FkCategory = noCategory.Id;

                                _dbcontext.Update(expense);
                            }

                            _dbcontext.Categories.Remove(objCategory);
                            _dbcontext.SaveChanges();

                            return StatusCode(StatusCodes.Status200OK, new { message = "Category deleted and expenses moved" });
                        }

                    case "delete":
                        {
                            List<Expense> catExpenses = _dbcontext.Expenses.Where(x => x.FkCategory == objCategory.Id && x.FkUser == objCategory.FkUser).ToList();

                            foreach (Expense expense in catExpenses)
                            {
                                _dbcontext.Remove(expense);
                            }

                            _dbcontext.Categories.Remove(objCategory);
                            _dbcontext.SaveChanges();

                            return StatusCode(StatusCodes.Status200OK, new { message = "Category and expenses deleted" });
                        }

                    default:
                        {
                            return StatusCode(StatusCodes.Status400BadRequest , new { message = "Invalid mode" });
                        }                        
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { message = "Couldn't delete any data" });

            }
        }
    }



}
