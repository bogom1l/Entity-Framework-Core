using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProductShop.DTOs.Import;
using ProductShop.Models;

namespace ProductShop
{
    using ProductShop.Data;

    public class StartUp
    {
        public static void Main()
        {
            ProductShopContext context = new ProductShopContext();

            //string inputJson = File.ReadAllText(@"../../../Datasets/categories-products.json");

            string result = GetUsersWithProducts(context);
            Console.WriteLine(result);
        }
        private static IMapper CreateMapper()
        {
            return new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ProductShopProfile>();
            }));
        }

        //Problem 01
        public static string ImportUsers(ProductShopContext context, string inputJson)
        {
            IMapper mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ProductShopProfile>();
            }));

            ImportUserDTO[] userDtos = JsonConvert.DeserializeObject<ImportUserDTO[]>(inputJson);

            ICollection<User> validUsers = new HashSet<User>();

            foreach (ImportUserDTO userDto in userDtos)
            {
                User user = mapper.Map<User>(userDto);

                validUsers.Add(user);
            }

            context.Users.AddRange(validUsers);
            context.SaveChanges();

            return $"Successfully imported {validUsers.Count}";
        }

        //Problem 02
        public static string ImportProducts(ProductShopContext context, string inputJson)
        {
            IMapper mapper = CreateMapper();

            ImportProductDto[] productDtos = JsonConvert.DeserializeObject<ImportProductDto[]>(inputJson);

            Product[] products = mapper.Map<Product[]>(productDtos);

            context.Products.AddRange(products);
            context.SaveChanges();

            return $"Successfully imported {products.Length}";
        }

        //Problem 03
        public static string ImportCategories(ProductShopContext context, string inputJson)
        {
            IMapper mapper = CreateMapper();

            ImportCategoryDto[] categoryDtos = 
                JsonConvert.DeserializeObject<ImportCategoryDto[]>(inputJson);

            ICollection<Category> validCategories = new HashSet<Category>();

            foreach (ImportCategoryDto categoryDto in categoryDtos)
            {
                if (String.IsNullOrEmpty(categoryDto.Name))
                {
                    continue;
                }

                Category category = mapper.Map<Category>(categoryDto);
                validCategories.Add(category);
            }

            context.Categories.AddRange(validCategories);
            context.SaveChanges();

            return  $"Successfully imported {validCategories.Count}";
        }

        //Problem 04
        public static string ImportCategoryProducts(ProductShopContext context, string inputJson)
        {
            IMapper mapper = CreateMapper();

            ImportCategoryProductDto[] categoryProductDtos =
                JsonConvert.DeserializeObject<ImportCategoryProductDto[]>(inputJson);

            ICollection<CategoryProduct> validEntries = new HashSet<CategoryProduct>();

            foreach (ImportCategoryProductDto cpDto in categoryProductDtos)
            {
                //This is not required in the problem description, but we do it for security
                //if (!context.Categories.Any(c => c.Id == cpDto.CategoryId) ||
                //    !context.Products.Any(p => p.Id == cpDto.ProductId))
                //{
                //    continue;
                //}

                CategoryProduct categoryProduct = mapper.Map<CategoryProduct>(cpDto);
                validEntries.Add(categoryProduct);
            }

            context.CategoriesProducts.AddRange(validEntries);
            context.SaveChanges();

            return $"Successfully imported {validEntries.Count}";
        }

        //Problem 05
        public static string GetProductsInRange(ProductShopContext context)
        {
            // anonymous objects solution:

            var products = context.Products
                .Where(p => p.Price >= 500 && p.Price <= 1000)
                .OrderBy(p => p.Price)
                .Select(p => new
                {
                    name = p.Name,
                    price = p.Price,
                    seller = p.Seller.FirstName + " " + p.Seller.LastName
                })
                .AsNoTracking()
                .ToArray();

            return JsonConvert.SerializeObject(products, Formatting.Indented);
        }

        //Problem 06
        public static string GetSoldProducts(ProductShopContext context)
        {
            var users = context.Users
                .Where(u => u.ProductsSold.Any(p => p.Buyer != null)) //BuyerId?
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new
                {
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    soldProducts = u.ProductsSold
                        .Where(p => p.Buyer != null)
                        .Select(p => new
                        {
                            name = p.Name,
                            price = p.Price,
                            buyerFirstName = p.Buyer.FirstName,
                            buyerLastName = p.Buyer.LastName
                        })
                        .ToArray()
                })
                .AsNoTracking()
                .ToArray();

            return JsonConvert.SerializeObject(users, Formatting.Indented);
        }

        //Problem 07
        public static string GetCategoriesByProductsCount(ProductShopContext context)
        {
            var categories = context.Categories
                .OrderByDescending(c => c.CategoriesProducts.Count)
                .Select(c => new
                {
                    category = c.Name,
                    productsCount = c.CategoriesProducts.Count,
                    averagePrice = (c.CategoriesProducts.Any() ?
                        c.CategoriesProducts.Average(cp => cp.Product.Price) : 0).ToString("f2"),
                    totalRevenue = (c.CategoriesProducts.Any() ?
                        c.CategoriesProducts.Sum(cp => cp.Product.Price) : 0).ToString("f2")
                })
                .AsNoTracking()
                .ToArray();

            return JsonConvert.SerializeObject(categories, Formatting.Indented);
        }

        //Problem 08
        public static string GetUsersWithProducts(ProductShopContext context)
        {
            var users = context.Users
                .Where(u => u.ProductsSold.Any(ps => ps.Buyer != null))
                .Select(u => new
                {
                    //UserDTO
                    firstName = u.FirstName, //Judge mistake in description lmao
                    lastName = u.LastName,
                    age = u.Age,
                    soldProducts = new
                    {
                        //ProductWrapperDTO
                        count = u.ProductsSold
                            .Count(p => p.Buyer != null),
                        products = u.ProductsSold
                            .Where(p => p.Buyer != null)
                            .Select(p => new
                            {
                                //ProductDTO
                                name = p.Name,
                                price = p.Price
                            })
                            .ToArray()
                    }
                })
                .OrderByDescending(u => u.soldProducts.count)
                .AsNoTracking()
                .ToArray();

            var userWrapperDto = new
            { //UserWrapperDTO
                usersCount = users.Length,
                users = users
            };

            return JsonConvert.SerializeObject(userWrapperDto, Formatting.Indented);
        }


    }
}