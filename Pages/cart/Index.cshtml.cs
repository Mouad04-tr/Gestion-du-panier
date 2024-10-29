using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SleekClothing.Data;
using SleekClothing.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SleekClothing.Pages.cart
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public decimal CartTotal { get; set; }


        public IList<Product> Products { get; set; } = new List<Product>();

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Méthode appelée lors du chargement de la page
        public void OnGet()
        {
            // Récupérer les produits du panier stockés dans la session
            Products = HttpContext.Session.Get<List<Product>>("CartProducts") ?? new List<Product>();

            CartTotal = Products.Sum(p => p.PriceAfterDiscount * p.CartQuantity);

        }

        // Ajouter un produit au panier
        public IActionResult OnPostAddToCart(int productId)
        {
            var product = _context.Products.FirstOrDefault(x => x.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            // Récupérer le panier actuel
            var cartProducts = HttpContext.Session.Get<List<Product>>("CartProducts") ?? new List<Product>();

            // Vérifier si le produit existe déjà dans le panier
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

            // Mettre à jour la session avec les produits modifiés
            HttpContext.Session.Set("CartProducts", cartProducts);

            return RedirectToPage();
        }

        // Retirer un produit du panier
        public IActionResult OnPostRemoveFromCart(int productId)
        {
            // Récupérer le produit à partir de la base de données
            var product = _context.Products.FirstOrDefault(x => x.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            // Récupérer le panier actuel
            var cartProducts = HttpContext.Session.Get<List<Product>>("CartProducts");

            if (cartProducts != null)
            {
                var productInCart = cartProducts.FirstOrDefault(p => p.Id == productId);
                if (productInCart != null && productInCart.CartQuantity > 0)
                {
                    productInCart.CartQuantity--;
                    if (productInCart.CartQuantity == 0)
                    {
                        cartProducts.Remove(productInCart); // Supprimer le produit du panier si la quantité est 0
                    }

                    // Mettre à jour la session avec les produits modifiés
                    HttpContext.Session.Set("CartProducts", cartProducts);
                }
            }

            return RedirectToPage();
        }

        // Vider le panier
        public IActionResult OnPostClearCart()
        {
            // Supprimer tous les produits du panier
            HttpContext.Session.Remove("CartProducts");

            return RedirectToPage();
        }
    }
}
