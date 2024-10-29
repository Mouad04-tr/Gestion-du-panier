using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SleekClothing.Data;
using SleekClothing.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SleekClothing.Helpers
{
    public class CartHelper
    {
        const string COOKIE_NAME = "SLKCARTDATA";

        #region cookie logic

        //Crée un cookie vide pour le panier avec une durée de vie de 240 jours.
        //Elle initialise le cookie avec une liste vide d'articles.
        private static void CreateCartCookie(HttpContext httpContext)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(240) // Expiration du cookie
            };

            // Initialiser l cookie avec une liste vide avec une liste vide de produits
            var initialValue = JsonConvert.SerializeObject(new List<Product>());
            httpContext.Response.Cookies.Append(COOKIE_NAME, initialValue, cookieOptions);
        }



        //Récupère les articles du panier depuis le cookie. Si le cookie n'existe pas,
        //elle retourne une liste vide.
        public static List<Product> GetUserCartCookie(HttpContext httpContext)
        {
            var cookieValue = httpContext.Request.Cookies[COOKIE_NAME]; //accéder aux données du panier stockées dans le cookie
            return cookieValue == null
                ? new List<Product>()
                : JsonConvert.DeserializeObject<List<Product>>(cookieValue) ?? new List<Product>();
        }

        //Ajouter un produit au panier
        public static void AddToCartCookie(Product newProduct, HttpContext httpContext)
        {
            List<Product> cartItems = new List<Product>();
            var cookieValue = httpContext.Request.Cookies[COOKIE_NAME];

            // Si le cookie n'existe pas, on le crée avec une liste vide
            if (cookieValue == null)
            {
                CreateCartCookie(httpContext);
            }
            else
            {
                cartItems = JsonConvert.DeserializeObject<List<Product>>(cookieValue) ?? new List<Product>();
            }

            // Vérifier si le produit est déjà dans le panier
            var existingProduct = cartItems.FirstOrDefault(p => p.Id == newProduct.Id);
            if (existingProduct != null)
            {
                existingProduct.CartQuantity += 1; // Mise à jour de la quantité si le produit existe
            }
            else
            {
                newProduct.CartQuantity = 1; // Initialisation de la quantité pour un nouveau produit
                cartItems.Add(newProduct); // Ajouter le nouveau produit au panier
            }

            // Mettre à jour le cookie
            var newCookieValue = JsonConvert.SerializeObject(cartItems, Formatting.Indented);
            httpContext.Response.Cookies.Append(COOKIE_NAME, newCookieValue);
        }
        

        //Supprimer un produit du panier
        public static void RemoveFromCartCookie(Product product, HttpContext httpContext)
        {
            var cookieValue = httpContext.Request.Cookies[COOKIE_NAME];
            if (string.IsNullOrEmpty(cookieValue)) return;

            var cartItems = JsonConvert.DeserializeObject<List<Product>>(cookieValue);

            // Supprimer le produit du panier
            cartItems.RemoveAll(item => item.Id == product.Id);

            if (cartItems.Count == 0)
            {
                // Supprimer le cookie si le panier est vide
                DeleteCartCookie(httpContext);
            }
            else
            {
                var newCookieValue = JsonConvert.SerializeObject(cartItems, Formatting.Indented);
                httpContext.Response.Cookies.Append(COOKIE_NAME, newCookieValue);
            }
        }

        //Supprimer le cookie du panier
        public static void DeleteCartCookie(HttpContext httpContext)
        {
            httpContext.Response.Cookies.Delete(COOKIE_NAME);
        }

        //Regroupe les articles du panier par ID de produit pour éviter
        //les doublons et additionner les quantités des articles identiques.
        public static List<Product> GetGroupedCartItemsCookie(HttpRequest httpRequest)
        {
            var cookieValue = httpRequest.Cookies[COOKIE_NAME];
            if (cookieValue == null)
            {
                return new List<Product>(); // retourne une liste vide si le cookie est null
            }

            var products = JsonConvert.DeserializeObject<List<Product>>(cookieValue); //recuperer la liste actuelle des produits

            var groupedProducts = products
                .GroupBy(u => u.Id)
                .Select(group => new Product
                {
                    Id = group.Key,
                    CartQuantity = group.Sum(p => p.CartQuantity), // Correctement additionner les quantités
                    Name = group.First().Name,
                    Price = group.First().Price,
                    Description = group.First().Description,
                    ImageLocation = group.First().ImageLocation,
                    Category = group.First().Category
                })
                .ToList();

            return groupedProducts;
        }

        //Compter le nombre de produits unique existant dans le panier
        public static int GetCartItemsCountCookie(HttpContext httpContext)
        {
            var cookieValue = httpContext.Request.Cookies[COOKIE_NAME];
            if (cookieValue == null)
            {
                return 0; // retourne 0 si aucun panier n'existe dans le cookie
            }

            var products = JsonConvert.DeserializeObject<List<Product>>(cookieValue);
            return products.Count;
        }

        //Calcule le total du panier 
        public static decimal GetCartTotalCookie(HttpRequest httpRequest)
        {
            var items = GetGroupedCartItemsCookie(httpRequest);
            return items.Sum(item => item.PriceAfterDiscount * item.CartQuantity);
        }

        #endregion

        #region db logic (using Cart and CartItem models)

        public static void AddToCartDb(Product product, ApplicationDbContext context)
        {
            var cart = context.Carts.FirstOrDefault();
            if (cart == null)
            {
                cart = new Cart { CartItems = new List<CartItem>() };
                context.Carts.Add(cart);
            }

            // Vérifier si le produit existe déjà dans le panier
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == product.Id);
            if (cartItem != null)
            {
                cartItem.Quantity += 1; // Mettre à jour la quantité
            }
            else
            {
                cartItem = new CartItem { ProductId = product.Id, Quantity = 1 };
                cart.CartItems.Add(cartItem);
            }

            context.SaveChanges();
        }

        public static void ClearCartDb(ApplicationDbContext context)
        {
            var cart = context.Carts.FirstOrDefault();
            if (cart != null)
            {
                cart.CartItems.Clear();
                context.SaveChanges();
            }
        }

        public static void RemoveFromCartDb(Product product, ApplicationDbContext context)
        {
            var cart = context.Carts.FirstOrDefault();
            if (cart != null)
            {
                var itemsToRemove = cart.CartItems.Where(item => item.ProductId == product.Id).ToList();

                foreach (var item in itemsToRemove)
                {
                    cart.CartItems.Remove(item);
                }

                context.SaveChanges();
            }
        }

        public static List<Product> GetCartItemsDb(ApplicationDbContext context)
        {
            var cart = context.Carts.FirstOrDefault();
            if (cart == null) return new List<Product>();

            return cart.CartItems.Select(ci => new Product
            {
                Id = ci.ProductId,
                CartQuantity = ci.Quantity
            }).ToList();
        }

        public static int GetCartItemsCountDb(ApplicationDbContext context)
        {
            var cart = context.Carts.FirstOrDefault();
            return cart?.CartItems.Count ?? 0;
        }

        public static decimal GetCartTotalDb(ApplicationDbContext context)
        {
            var cart = context.Carts.FirstOrDefault();
            if (cart == null) return 0;

            return cart.CartItems.Sum(item => item.Product.PriceAfterDiscount * item.Quantity);
        }

        #endregion

        #region convert cookie items to db items

        public static void ConvertToDB(HttpContext httpContext, ApplicationDbContext context)
        {
            var cookieItems = GetUserCartCookie(httpContext);

            if (cookieItems.Count == 0) return;

            foreach (var product in cookieItems)
            {
                AddToCartDb(product, context);
            }

            DeleteCartCookie(httpContext);
        }

        #endregion
    }
}
