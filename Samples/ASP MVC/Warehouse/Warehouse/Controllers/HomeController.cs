using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Warehouse.App_Start;
using Warehouse.Models;
using Warehouse.WPOModels;

namespace Warehouse.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Products(string phrase = null, int page = 1, string sort = "nameAsc")
        {
            List<Product> products = DBManager.Manager.GetQuery<Product>(DBManager.Session)
                                                      .Where("ProductDescription LIKE '%" + phrase + "%'")
                                                      .Where("ProductName LIKE '%" + phrase + "%'", true)
                                                      .OrderBy(sort.Contains("name") ? "ProductName" : "Price", sort.Contains("Asc"))
                                                      .Skip(5 * (page - 1))
                                                      .Take(5)
                                                      .ToList();

            var model = products.Select(p => new ProductRowModel() { Id = p.Id, Name = p.Name, Price = p.Price }).ToList();

            ViewBag.Phrase = phrase;
            ViewBag.Page = page;
            ViewBag.Sort = sort;
            return View(model);
        }

        public ActionResult AddProduct()
        {
            return View(new ProductEditModel());
        }

        [HttpPost]
        public ActionResult AddProduct(ProductEditModel model)
        {
            if (ModelState.IsValid)
            {
                if (DBManager.Manager.GetQuery<Product>(DBManager.Session).GetObjectByKey(model.Id) != null)
                {
                    ModelState.AddModelError("Id", "Product with this identifier already exsists");
                    return View(model);
                }

                Product product = new Product(DBManager.Session) { Id = model.Id, Name = model.Name, Price = model.Price, ProductDescription = model.Description };
                DBManager.Session.Commit();

                TempData["Message"] = "Product has been created!";
                return RedirectToAction("Index");
            }
            else
            {
                return View(model);
            }
        }

        public ActionResult EditProduct(int id)
        {
            Product product = DBManager.Manager.GetQuery<Product>(DBManager.Session).GetObjectByKey(id);
            if (product == null)
            {
                TempData["Message"] = "Product with given identifier doesn't exist";
                return RedirectToAction("Index");
            }
            else
            {
                ProductEditModel model = new ProductEditModel { Id = product.Id, Name = product.Name, Price = product.Price, Description = product.ProductDescription };
                return View(model);
            }
        }

        [HttpPost]
        public ActionResult EditProduct(ProductEditModel model)
        {
            if (ModelState.IsValid)
            {
                Product product = DBManager.Manager.GetQuery<Product>(DBManager.Session).GetObjectByKey(model.Id);

                product.Name = model.Name;
                product.Price = model.Price;
                product.ProductDescription = model.Description;

                DBManager.Session.Commit();
                return RedirectToAction("Products");
            }
            else
            {
                return View(model);
            }
        }

        public ActionResult DeleteProduct(int id)
        {
            Product product = DBManager.Manager.GetQuery<Product>(DBManager.Session).GetObjectByKey(id);
            if (product == null)
            {
                TempData["Message"] = "Product with given identifier doesn't exist";
                return RedirectToAction("Index");
            }
            else
            {
                product.Remove();
                DBManager.Session.Commit();
                return RedirectToAction("Products");
            }
        }
    }
}