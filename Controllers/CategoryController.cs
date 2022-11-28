using GastonAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;

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
        public IActionResult CategoryList()
        {
            List<Category> categories = new List<Category>();

            var identity = HttpContext.User.Identity as ClaimsIdentity;

            var valid = OtherFunctions.ValidateToken(identity);

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
        public IActionResult CategoryListUser(int userId)
        {
            List<Category> userCategories = _dbcontext.Categories.Where(x => x.FkUser == userId).ToList();

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

        /**
         * TODO: Un usuario solo puede ver detalles de sus categorías
         */
        [HttpGet]
        [Route("CategoryDet")]
        public IActionResult CategoryDet(int id)
        {
            Category objCategory = _dbcontext.Categories.Find(id);

            if (objCategory == null)
            {
                return NotFound(new { mensaje = "Data not found" });
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


        [HttpPost]
        [Route("SaveCategory")]
        public IActionResult SaveCategory([FromBody] Category objCategory)
        {
            if (string.IsNullOrEmpty(objCategory.Name) || string.IsNullOrEmpty(objCategory.FkUser.ToString()))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { message = "Missing Required Fields" });
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

        /**
         * TODO: Solo los usuarios administradores pueden mover una categoría a otro usuario
         */
        [HttpPut]
        [Route("UpdateCategory")]
        public IActionResult UpdateCategory([FromBody] Category objCategory)
        {
            Category _Category = _dbcontext.Categories.Find(objCategory.Id);

            if (_Category == null)
            {
                return NotFound(new { mensaje = "Category not Found" });
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
        public IActionResult DeleteCategory(int id, string mode = "nocategory", int moveTo = 0)
        {
            Category objCategory = _dbcontext.Categories.Find(id);

            if (objCategory == null)
            {
                return NotFound(new { message = "Category not found" });
            }

            if (objCategory.Name == "No Category")
            {
                return BadRequest(new { message = "This category can't be deleted"});
            }

            try
            {
                switch (mode)
                {
                    case "move":
                        {
                            if (moveTo == 0)
                            {
                                return StatusCode(StatusCodes.Status400BadRequest,  new { message = "Category to move not specified" });
                            }

                            List<Expense> catExpenses = _dbcontext.Expenses.Where(x => x.FkCategory == objCategory.Id && x.FkUser == objCategory.FkUser).ToList();
                            foreach (Expense expense in catExpenses)
                            {
                                expense.FkCategory = moveTo;

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
