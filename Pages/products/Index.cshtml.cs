using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SleekClothing.Data;
using SleekClothing.Helpers;
using SleekClothing.Models;

namespace SleekClothing.Pages.products
{
    public class IndexModel : PageModel
    {
        private readonly SleekClothing.Data.ApplicationDbContext _context;

        public IndexModel(SleekClothing.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string SearchTerm { get; set; }

        [BindProperty]
        public int? SelectedCategoryId { get; set; } // ID de la catégorie sélectionnée

        public IList<Product> Products { get; set; }

        public IList<Category> Categories { get; set; } // Liste des catégories

        public async Task OnGetAsync()
        {
            // Charger les catégories
            Categories = await _context.Categories.ToListAsync();

            // Charger tous les produits par défaut
            Products = await _context.Products.ToListAsync();
        }

        public IActionResult OnPostResetSearch()
        {
            return Redirect("/products");
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            // Charger les catégories pour permettre à la vue de les afficher
            Categories = await _context.Categories.ToListAsync();

            // Si aucun terme de recherche ou catégorie n'est sélectionné, redirection avec message d'erreur
            if (string.IsNullOrWhiteSpace(SearchTerm) && !SelectedCategoryId.HasValue)
            {
                TempData["info"] = "Please enter a valid search term or select a category.";
                return Redirect("/products");
            }

            var query = _context.Products.AsQueryable();

            // Filtrer par terme de recherche
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(x => x.Name.Contains(SearchTerm) || x.Category.Name.Contains(SearchTerm));
            }

            // Filtrer par catégorie
            if (SelectedCategoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == SelectedCategoryId.Value);
            }

            // Récupérer les produits filtrés
            Products = await query.ToListAsync();

            // Afficher la page avec les résultats filtrés
            return Page();
        }

        public IActionResult OnPostAddToCart(int productId)
        {
            Products = _context.Products.ToList();
            Product product = _context.Products.FirstOrDefault(x => x.Id == productId);

            // Vérifiez si le produit existe
            if (product == null)
            {
                TempData["error"] = "Product not found.";
                return Redirect("/products");
            }

            // Handle product out of stock
            if (product.IsOutOfStock)
            {
                TempData["error"] = $"{product.Name} is out of stock.";
                return Redirect("/products");
            }

            // Ajout au panier via les sessions (ou cookies)
            var cartProducts = HttpContext.Session.Get<List<Product>>("CartProducts") ?? new List<Product>();

            var existingProduct = cartProducts.FirstOrDefault(p => p.Id == productId);
            if (existingProduct != null)
            {
                existingProduct.CartQuantity++;
            }
            else
            {
                product.CartQuantity = 1;
                cartProducts.Add(product);
            }

            HttpContext.Session.Set("CartProducts", cartProducts);

            TempData["success"] = $"{product.Name} added to cart successfully!";
            return Redirect("/products");
        }



    }
}
