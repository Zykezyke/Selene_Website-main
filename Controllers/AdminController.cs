using SELENE_STUDIO.Data;
using SELENE_STUDIO.Models;
using SELENE_STUDIO.Models.Faker;
using SELENE_STUDIO.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using static SELENE_STUDIO.Models.Product;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using static System.Net.Mime.MediaTypeNames;
using AngleSharp.Css.Dom;
using System.Formats.Asn1;
using CsvHelper;
using System.Globalization;
using OfficeOpenXml;

namespace SELENE_STUDIO.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly LogAppDbContext _context;
        private readonly UserManager<RegUser> _userManager;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public AdminController(LogAppDbContext context, IWebHostEnvironment hostingEnvironment, UserManager<RegUser> userManager)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch all orders by default if no date range is specified
            IQueryable<Order> ordersQuery = _context.Orders;



            // Perform further calculations or data retrieval based on the filtered orders
            var totalProducts = await _context.Products.CountAsync();
            var totalPendingOrders = await ordersQuery.CountAsync(order => order.OrderStatus == OrderStatus.Pending);
            var totalProductsSold = await ordersQuery
                .Where(order => order.OrderStatus == OrderStatus.Completed)
                .SelectMany(order => order.Products)
                .SumAsync(product => product.Quantity);

            // Calculate total income for the filtered orders
            var totalPriceSoldOfEachProduct = await ordersQuery
                .Where(order => order.OrderStatus == OrderStatus.Completed)
                .SelectMany(order => order.Products)
                .ToListAsync(); // Fetch data first

            var totalPriceSoldByProduct = totalPriceSoldOfEachProduct
                .GroupBy(product => product.ProductId) // Group in memory
                .Select(group => new {
                    ProductID = group.Key,
                    TotalPriceSold = group.Sum(product => product.Quantity * product.TotalPrice)
                })
                .ToDictionary(item => item.ProductID, item => item.TotalPriceSold);

            // Calculate overall sales per month for the filtered orders
            var overallSalesPerMonth = await ordersQuery
                .Where(order => order.OrderStatus == OrderStatus.Completed)
                .ToListAsync(); // Fetch data first

            var overallSales = overallSalesPerMonth
                .GroupBy(order => new { order.Date.Year, order.Date.Month }) // Group in memory
                .Select(group => new {
                    Month = $"{group.Key.Year}-{group.Key.Month}",
                    TotalSales = group.Sum(order => order.Products.Sum(product => product.Quantity * product.TotalPrice))
                })
                .ToDictionary(item => item.Month, item => item.TotalSales);

            return View(new AdminDashboardViewModel
            {
                TotalProducts = totalProducts,
                TotalPendingOrders = totalPendingOrders,
                TotalProductsSold = totalProductsSold,
                TotalPriceSoldOfEachProduct = totalPriceSoldByProduct,
                OverallSalesPerMonth = overallSales
            });
        }
        [HttpGet]
        public async Task<IActionResult> RenderChart(string startDate, string endDate)
        {
            if (!DateTime.TryParse(startDate, out var parsedStartDate) ||
                !DateTime.TryParse(endDate, out var parsedEndDate))
            {
                // Handle invalid date format
                return BadRequest("Invalid date format");
            }

            Console.WriteLine("Received start date: " + parsedStartDate);
            Console.WriteLine("Received end date: " + parsedEndDate);

            // Fetch all orders by default if no date range is specified
            IQueryable<Order> ordersQuery = _context.Orders;

            // Filter orders based on the selected date range
            if (parsedStartDate != null && parsedEndDate != null)
            {
                // Include orders within the specified date range
                ordersQuery = ordersQuery.Where(order => order.Date.Date >= parsedStartDate && order.Date.Date <= parsedEndDate);

                Console.WriteLine("Filtered orders count: " + await ordersQuery.CountAsync());
            }

            // Perform further calculations or data retrieval based on the filtered orders
            var totalProducts = await _context.Products.CountAsync();
            var totalPendingOrders = await ordersQuery.CountAsync(order => order.OrderStatus == OrderStatus.Pending);
            var totalProductsSold = await ordersQuery
                .Where(order => order.OrderStatus == OrderStatus.Completed)
                .SelectMany(order => order.Products)
                .SumAsync(product => product.Quantity);

            // Calculate total income for the filtered orders
            var totalPriceSoldOfEachProduct = await ordersQuery
                .Where(order => order.OrderStatus == OrderStatus.Completed)
                .SelectMany(order => order.Products)
                .ToListAsync(); // Fetch data first

            var totalPriceSoldByProduct = totalPriceSoldOfEachProduct
                .GroupBy(product => product.ProductId) // Group in memory
                .Select(group => new {
                    ProductID = group.Key,
                    TotalPriceSold = group.Sum(product => product.Quantity * product.TotalPrice)
                })
                .ToDictionary(item => item.ProductID, item => item.TotalPriceSold);

            // Calculate overall sales per month for the filtered orders
            var overallSalesPerMonth = await ordersQuery
                .Where(order => order.OrderStatus == OrderStatus.Completed)
                .ToListAsync(); // Fetch data first

            var overallSales = overallSalesPerMonth
                .GroupBy(order => new { order.Date.Year, order.Date.Month }) // Group in memory
                .Select(group => new {
                    Month = $"{group.Key.Year}-{group.Key.Month}",
                    TotalSales = group.Sum(order => order.Products.Sum(product => product.Quantity * product.TotalPrice))
                })
                .ToDictionary(item => item.Month, item => item.TotalSales);

            return Json(new
            {
                TotalProducts = totalProducts,
                TotalPendingOrders = totalPendingOrders,
                TotalProductsSold = totalProductsSold,
                TotalPriceSoldOfEachProduct = totalPriceSoldByProduct,
                OverallSalesPerMonth = overallSales
            });
        }


        [HttpPost]
        public IActionResult InitiateConversation(string userId)
        {

            var user = _context.Users.FirstOrDefault(u => u.Id == userId.ToString());
            if (user == null)
            {
                return NotFound(); 
            }

            // Get the current user
            var currentUser = _userManager.GetUserAsync(User).Result;

            // Create a new conversation
            var conversation = new Conversation
            {
                InitiatorId = userId,
                ReceiverId = currentUser.Id, 
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            _context.SaveChanges();

            // Redirect to view conversation action with the conversation id
            return RedirectToAction("SendMessage", "Admin", new { id = conversation.ConversationId });
        }

public IActionResult ViewConversations(int page = 1, int pageSize = 10)
{
    // Get the current user
    var currentUser = _userManager.GetUserAsync(User).Result;

    // Retrieve conversations involving the current user
    var conversations = _context.Conversations
        .Include(c => c.Initiator)
        .Where(c => c.InitiatorId == currentUser.Id || c.ReceiverId == currentUser.Id)
        .ToList();

    // Calculate total pages
    int totalConversations = conversations.Count;
    int totalPages = (int)Math.Ceiling((double)totalConversations / pageSize);

    // Paginate conversations
    conversations = conversations.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    // Retrieve a list of users
    var users = _context.Users.ToList();

    // You can pass both conversations and users to the view using ViewBag or a ViewModel
    ViewBag.Conversations = conversations;
    ViewBag.Users = users;
    ViewBag.TotalPages = totalPages;
    ViewBag.CurrentPage = page;
    ViewBag.PageSize = pageSize;

    // Add the initiator's first name to each conversation
    foreach (var conversation in conversations)
    {
        conversation.InitiatorId = conversation.Initiator != null ? conversation.Initiator.FirstName : "Unknown";
    }

    return View();
}



        [HttpGet]
        public IActionResult SendMessage(int id) // Change parameter name to 'id' to match the route
        {
            // Retrieve the conversation including the Initiator
            var conversation = _context.Conversations
                .Include(c => c.Initiator)
                .FirstOrDefault(c => c.ConversationId == id);

            if (conversation == null)
            {
                return NotFound(); // Handle the case where the conversation does not exist
            }

            // Get the current user
            var currentUser = _userManager.GetUserAsync(User).Result;

            // Check if the current user is a participant in the conversation
            if (conversation.InitiatorId != currentUser.Id && conversation.ReceiverId != currentUser.Id)
            {
                return Forbid(); // Handle unauthorized access to the conversation
            }

            // Retrieve messages for the conversation
            var messages = _context.Messages.Where(m => m.ConversationId == id).ToList();

            // Fetch sender names for each message
            foreach (var message in messages)
            {
                var sender = _userManager.FindByIdAsync(message.SenderId).Result;
                message.SenderName = sender != null ? sender.FirstName : "Unknown";
            }

            // Pass the conversation and messages to the view
            var viewModel = new ConversationViewModel { Conversation = conversation, Messages = messages };
            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> SendMessage(int conversationId, string content, IFormFile image)
        {
            try
            {
                // Retrieve the conversation
                var conversation = await _context.Conversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound(); // Handle the case where the conversation does not exist
                }

                // Get the current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account"); // Redirect to login if user is not authenticated
                }

                // Check if the current user is a participant in the conversation
                if (conversation.InitiatorId != currentUser.Id && conversation.ReceiverId != currentUser.Id)
                {
                    return Forbid(); // Handle unauthorized access to the conversation
                }

                // Create a new message
                var message = new Message
                {
                    Content = !string.IsNullOrEmpty(content) ? content.Replace("\n", "<br/>") : "", // Handle empty content
                    ImageUrl = "",
                    SenderId = currentUser.Id,
                    ReceiverId = conversation.ReceiverId, // Assuming the current user is the sender
                    ConversationId = conversationId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow // Set the CreatedAt property to the current timestamp
                };


                if (image != null && image.Length > 0)
                {
                    // Save the image to the server
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "messages", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    // Set the image URL in the message
                    message.ImageUrl = "/uploads/messages/" + fileName;
                }

                _context.Messages.Add(message);
                await _context.SaveChangesAsync(); // Save changes asynchronously

                // Redirect back to SendMessage action to display the updated conversation
                return RedirectToAction("SendMessage", new { id = conversationId });
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as appropriate
                return StatusCode(500, "An error occurred while sending the message.");
            }
        }




        [HttpGet]
        public IActionResult FetchMessages(int conversationId)
        {
            var messages = _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .Select(m => new {
                    m.MessageId,
                    m.Content,
                    m.ImageUrl,
                    m.IsRead,
                    SenderName = m.SenderId == "1" ? "Admin" : _context.Users.FirstOrDefault(u => u.Id == m.SenderId).FirstName // Retrieve sender's name from the database
                })
                .ToList();

            foreach (var message in messages)
            {
                if (!(message.SenderName == "Admin") && !message.IsRead)
                {
                    var messageToUpdate = _context.Messages.Find(message.MessageId);
                    messageToUpdate.IsRead = true;
                }
            }
            _context.SaveChanges();
            return Json(new { messages });
        }

        [HttpGet]
        public async Task<IActionResult> GetProductName(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return Json(new { ProductName = product.ProductName });
        }

        [HttpGet]
        public IActionResult GetAvailableSizes(Category category)
        {
            var product = new Product { ProductCategory = category };
            var availableSizes = product.GetAvailableSizes();
            return Json(availableSizes);
        }
        public IActionResult Products(int page = 1, int pageSize = 5)
        {
            var totalCount = _context.Products.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Retrieve products with pagination
            var products = _context.Products
                .Include(p => p.AdditionalPrices)
                .OrderBy(p => p.ProductID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            return View(products);
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            var product = new Product(); // Create a new instance of the Product object
            return View(product);
        }


        [HttpPost]
        public IActionResult AddProduct(Product product, List<IFormFile> imageFiles)
        {
            if (ModelState.IsValid)
            {
                product.DateCreated = DateTime.Now;

                product.isHidden = false;
                // Check if at least one file is uploaded
                if (imageFiles == null || imageFiles.Count == 0)
                {
                    TempData["ErrorMessage"] = "At least one image file must be uploaded.";
                    return RedirectToAction(nameof(AddProduct));
                }

                // Check if the uploaded files are not null and are less than or equal to 3MB
                foreach (var imageFile in imageFiles)
                {
                    if (imageFile != null && imageFile.Length <= 3 * 1024 * 1024) // 3MB in bytes
                    {
                        // Handle file upload for each image
                        var mimeType = imageFile.ContentType;
                        Console.WriteLine($"Uploaded file MIME type: {mimeType}");
                        string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        var directory = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Now proceed with file creation
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            imageFile.CopyTo(fileStream);
                        }

                        // Save the file path to the list
                        product.ImagePaths.Add("/uploads/" + uniqueFileName);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "One or more uploaded files must be less than or equal to 3MB.";
                        return RedirectToAction(nameof(AddProduct));
                    }
                }

                

                // Add the product to the database
                _context.Products.Add(product);
                foreach (var size in product.GetAvailableSizes())
                {
                    // Retrieve additional price per size from the form
                    string additionalPriceKey = $"AdditionalPrices[{size}]";
                    if (Request.Form.TryGetValue(additionalPriceKey, out StringValues additionalPriceValue))
                    {
                        decimal additionalPrice;
                        if (decimal.TryParse(additionalPriceValue, out additionalPrice))
                        {
                            // Create and add AdditionalPrice entity
                            var additionalPriceEntity = new AdditionalPrice
                            {
                                Product = product, // Set the Product navigation property
                                Size = size,
                                Price = additionalPrice
                            };

                            _context.AdditionalPrices.Add(additionalPriceEntity);
                        }
                    }
                }

                _context.SaveChanges();

                // Redirect to a success page or perform other actions
                return RedirectToAction("Products", "Admin");
            }

            // If the model state is not valid or the file is too large, return to the same view with validation errors
            return View(product);
        }

        [HttpPost]
        public IActionResult ToggleProductVisibility(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductID == id);
            if (product != null)
            {
                product.isHidden = !product.isHidden; // Toggle the isHidden property
                _context.SaveChanges();
            }
            return RedirectToAction("Products", "Admin");
        }




        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            Product product = _context.Products.FirstOrDefault(st => st.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        public IActionResult EditProduct(int id, Product product, List<IFormFile> imageFiles, int[] RemovedImageIndexes)
        {
            if (ModelState.IsValid)
            {
                // Retrieve the existing product from the database
                Product existingProduct = _context.Products
                    .Include(p => p.AdditionalPrices) // Include additional prices
                    .FirstOrDefault(st => st.ProductID == id);

                if (existingProduct == null)
                {
                    return NotFound(); // Or handle the case where the product is not found
                }

                // Update non-image properties
                existingProduct.ProductName = product.ProductName;
                existingProduct.ProductCategory = product.ProductCategory;
                existingProduct.ProductDescription = product.ProductDescription;
                existingProduct.ProductSize = product.ProductSize;

                // Update additional prices per size
                foreach (var size in product.GetAvailableSizes())
                {
                    string additionalPriceKey = $"AdditionalPrices[{size}]";
                    if (Request.Form.TryGetValue(additionalPriceKey, out StringValues additionalPriceValue))
                    {
                        decimal additionalPrice;
                        if (decimal.TryParse(additionalPriceValue, out additionalPrice))
                        {
                            // Find the existing additional price entity for this size
                            var existingAdditionalPrice = existingProduct.AdditionalPrices.FirstOrDefault(ap => ap.Size == size);
                            if (existingAdditionalPrice != null)
                            {
                                // Update the existing additional price
                                existingAdditionalPrice.Price = additionalPrice;
                            }
                            else
                            {
                                // Create and add new additional price entity
                                var additionalPriceEntity = new Product.AdditionalPrice
                                {
                                    ProductId = existingProduct.ProductID,
                                    Size = size,
                                    Price = additionalPrice
                                };

                                _context.AdditionalPrices.Add(additionalPriceEntity);
                            }
                        }
                    }
                }


                List<string> imagePathsList = existingProduct.ImagePaths.ToList();

                for (int i = imagePathsList.Count; i < imageFiles.Count; i++)
                {
                    // Add a placeholder for the new image path
                    imagePathsList.Add(null);
                }


                foreach (var index in RemovedImageIndexes)
                {
                    if (index >= 0 && index < imagePathsList.Count)
                    {
                        imagePathsList.RemoveAt(index);
                    }
                }

                // Update the ICollection<string> with the modified list
                existingProduct.ImagePaths = imagePathsList;

                // Handle file uploads for images
                foreach (var imageFile in imageFiles)
                {
                    if (imageFile != null && imageFile.Length <= 3 * 1024 * 1024) // 3MB in bytes
                    {
                        // Handle file upload for each image
                        var mimeType = imageFile.ContentType;
                        Console.WriteLine($"Uploaded file MIME type: {mimeType}");

                        string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        var directory = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Now proceed with file creation
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            imageFile.CopyTo(fileStream);
                        }

                        // Add the new file path to the list of image paths
                        existingProduct.ImagePaths.Add("/uploads/" + uniqueFileName);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "One or more uploaded files must be less than or equal to 3MB.";
                        return RedirectToAction(nameof(EditProduct), new { id });
                    }
                }



                // Update the product in the database
                _context.Products.Update(existingProduct);
                _context.SaveChanges();

                // Redirect to the Products page or any other desired page
                return RedirectToAction("Products", "Admin");
            }

            // If the model state is not valid or the file is too large, return to the same view with validation errors
            return View(product);
        }




        [HttpPost]
        public IActionResult DeleteProduct()
        {
            int productId = Convert.ToInt32(Request.Form["id"]);
            Product? product = _context.Products.FirstOrDefault(st => st.ProductID == productId);

            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            _context.SaveChanges();

            return RedirectToAction("Products", "Admin");
        }

        public IActionResult Messages()
        {
            return View(_context.Messages.ToArray());
        }
        public IActionResult Order(int id)
        {
            var order = _context.Orders
                                .Include(order => order.Customer)
                                .Include(order => order.Products)
                                    .ThenInclude(product => product.Product)
                                        .ThenInclude(product => product.AdditionalPrices)
                                .FirstOrDefault(order => order.OrderNumber == id);

            if (order == null)
            {
                return NotFound();
            }

            // Get the current logged-in user
            var currentUser = _userManager.GetUserAsync(User).Result;

            // Adjust session start and end time based on the order's date
            if (currentUser.SessionEndTime < order.Date && currentUser.SessionStartTime > order.Date)
            {
                // Update session end time
                currentUser.SessionEndTime = order.Date;
                currentUser.SessionStartTime = order.Date;
            }

            // Save changes to the database
            _context.SaveChanges();

            return View(order);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateOrder(int orderNumber, string orderStatus, string paymentStatus)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderNumber == orderNumber);
            if (order == null)
            {
                return NotFound(); // Return 404 Not Found if the order is not found
            }



            // Update order status and payment status
            order.OldPaymentStatus = order.PaymentStatus;
            order.OldOrderStatus = order.OrderStatus;

            // Update order status and payment status
            if (!string.IsNullOrEmpty(orderStatus))
            {
                order.OrderStatus = Enum.Parse<OrderStatus>(orderStatus);
            }

            if (!string.IsNullOrEmpty(paymentStatus))
            {
                order.PaymentStatus = Enum.Parse<PaymentStatus>(paymentStatus);
            }

            // Save changes to the database
            await _context.SaveChangesAsync();


            return RedirectToAction("ListOrders", "Admin");
        }




        public IActionResult DeleteOrder(int id)
        {
            Order order = _context.Orders.Include(o => o.Products).FirstOrDefault(st => st.OrderNumber == id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Products != null)
            {
                _context.CartProducts.RemoveRange(order.Products);
            }

            _context.Orders.Remove(order);
            _context.SaveChanges();

            return RedirectToAction("ListOrders", "Admin");
        }


        public IActionResult ListOrders(int page = 1, int pageSize = 5)
        {
            // Get the current logged-in user
            var currentUser = _userManager.GetUserAsync(User).Result;

            // Add the currentUser to ViewBag
            ViewBag.CurrentUser = currentUser;

            // Retrieve the total count of orders
            var totalCount = _context.Orders.Count();

            // Calculate the total number of pages
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Retrieve the orders including customer information with pagination
            var orders = _context.Orders
                                .Include(order => order.Customer)
                                .OrderByDescending(order => order.Date)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToArray();

            // Pass pagination data to ViewBag
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            return View(orders);
        }



        [HttpGet]
        public IActionResult FeedbackList()
        {
            var productsWithFeedback = _context.Products.Where(p => p.Feedbacks.Any()).ToList();
            return View(productsWithFeedback);
        }

        [HttpGet]
        public IActionResult ViewFeedbacks(int productId, int page = 1, int pageSize = 5)
        {
            var product = _context.Products
                .Include(p => p.Feedbacks)
                    .ThenInclude(f => f.User)
                .FirstOrDefault(p => p.ProductID == productId);

            if (product == null)
            {
                return NotFound();
            }

            var totalCount = product.Feedbacks.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Retrieve feedbacks with pagination
            var feedbacks = product.Feedbacks
                .OrderByDescending(f => f.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass feedbacks to ViewBag
            ViewBag.Feedbacks = feedbacks;

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            return View(product);
        }



        [HttpPost]
        public IActionResult ToggleFeatured(int feedbackId, int productId)
        {
            // Get the feedback by Id
            var feedback = _context.Feedbacks.FirstOrDefault(f => f.Id == feedbackId);
            if (feedback == null)
            {
                return NotFound();
            }

            // Toggle IsFeatured property
            feedback.IsFeatured = !feedback.IsFeatured;

            // Check if the feedback is being featured
            if (feedback.IsFeatured)
            {
                // Get the count of featured feedbacks across all products
                var featuredCount = _context.Feedbacks.Count(f => f.IsFeatured);

                // Check if the maximum limit (3) has been reached
                if (featuredCount >= 3)
                {
                    // Optionally, you can add some notification to inform the user that the limit has been reached
                    // For example, you can use TempData to display a message on the next page load
                    TempData["MaxFeaturedLimitReached"] = "Maximum limit of featured feedbacks reached.";
                    // Rollback the feature toggle
                    feedback.IsFeatured = false;
                }
            }

            // Save changes to the database
            _context.SaveChanges();

            // Redirect to the ViewFeedbacks action
            return RedirectToAction("ViewFeedbacks", new { productId });
        }



        [HttpGet]
        public async Task<IActionResult> GetNotificationsAsync()
        {
            // Get the current logged-in user
            var currentUser = await _userManager.GetUserAsync(User);

            // Count new messages
            int newMessagesCount = await _context.Messages
                .Where(m => m.Conversation != null && m.SenderId == m.Conversation.InitiatorId && !m.IsRead)
                .CountAsync();

            // Count new orders based on the specified conditions
            int newOrdersCount = await _context.Orders
                .Where(o => (o.Date > currentUser.SessionEndTime && o.Date < currentUser.SessionStartTime) ||
                            (o.OrderStatus == OrderStatus.Accepted && o.PaymentStatus == PaymentStatus.Pending && !string.IsNullOrEmpty(o.UploadedImagePath)))
                .CountAsync();

            return Json(new { NewMessagesCount = newMessagesCount, NewOrdersCount = newOrdersCount });
        }

        public async Task<IActionResult> Sales()
        {
            // Fetch all orders by default if no date range is specified
            IQueryable<Order> ordersQuery = _context.Orders;



            // Perform further calculations or data retrieval based on the filtered orders
            var totalProducts = await _context.Products.CountAsync();
            var totalPendingOrders = await ordersQuery.CountAsync(order => order.OrderStatus == OrderStatus.Pending);
            var totalProductsSold = await ordersQuery
                .Where(order => order.OrderStatus == OrderStatus.Completed)
                .SelectMany(order => order.Products)
                .SumAsync(product => product.Quantity);

            // Calculate total income for the filtered orders
            var totalPriceSoldOfEachProduct = await ordersQuery
                .Where(order => order.OrderStatus == OrderStatus.Completed)
                .SelectMany(order => order.Products)
                .ToListAsync(); // Fetch data first

            var totalPriceSoldByProduct = totalPriceSoldOfEachProduct
                .GroupBy(product => product.ProductId) // Group in memory
                .Select(group => new {
                    ProductID = group.Key,
                    TotalPriceSold = group.Sum(product => product.Quantity * product.TotalPrice)
                })
                .ToDictionary(item => item.ProductID, item => item.TotalPriceSold);

            // Calculate overall sales per month for the filtered orders
            var overallSalesPerMonth = await ordersQuery
                .Where(order => order.OrderStatus == OrderStatus.Completed)
                .ToListAsync(); // Fetch data first

            var overallSales = overallSalesPerMonth
                    .GroupBy(order => new { order.Date.Year, order.Date.Month }) // Group in memory
                    .OrderBy(group => group.Key.Year)
                    .ThenBy(group => group.Key.Month)
                    .Select(group => new
                    {
                        Month = $"{group.Key.Year}-{group.Key.Month}",
                        TotalSales = group.Sum(order => order.Products.Sum(product => product.Quantity * product.TotalPrice))
                    })
                    .ToDictionary(item => item.Month, item => item.TotalSales);

            return View(new AdminDashboardViewModel
            {
                TotalProducts = totalProducts,
                TotalPendingOrders = totalPendingOrders,
                TotalProductsSold = totalProductsSold,
                TotalPriceSoldOfEachProduct = totalPriceSoldByProduct,
                OverallSalesPerMonth = overallSales
            });
        }

        public async Task<JsonResult> GetProductNames(List<int> productIds)
        {
            var productNames = await _context.Products
                .Where(p => productIds.Contains(p.ProductID))
                .Select(p => p.ProductName)
                .ToListAsync();

            return Json(productNames);
        }

        public IActionResult DownloadOrders()
        {
            var orders = _context.Orders.Select(order => new
            {
                order.OrderNumber,
                Date = order.Date.ToString("yyyy-MM-dd"), // Format the date as desired
                order.Customer.Id,
                order.Customer.UserName,
                order.Customer.Email,
                order.DeliveryAddress,
                order.PaymentMethod,
                order.DeliveryMode,
                order.OldPaymentStatus,
                order.OldOrderStatus,
                order.PaymentStatus,
                order.OrderStatus,
                Products = order.Products.Select(cp => new
                {
                    cp.ProductId,
                    cp.Product.ProductName,
                    cp.Quantity,
                    cp.SelectedSize,
                    TotalPrice = "P" + cp.TotalPrice.ToString("N", CultureInfo.InvariantCulture), // Add the peso sign here
                    cp.CustomDescription,
                    cp.CustomImagePath
                }).ToList(),
                TotalPrice = "P" + order.TotalPrice.ToString("N", CultureInfo.InvariantCulture), // Add the peso sign here
                order.UploadedImagePath,
                order.IsDeleted
            }).ToList();

            return DownloadExcel(orders, "orders.xlsx");
        }




        public IActionResult DownloadSalesPerProduct()
        {
            var products = _context.Products.ToList(); // Retrieve all products from the database

            var salesPerProduct = products.Select(product => new
            {
                ProductName = product.ProductName,
                TotalQuantity = _context.Orders
                    .Where(order => order.Products != null && order.Products.Any(cp => cp.ProductId == product.ProductID))
                    .SelectMany(order => order.Products.Where(cp => cp.ProductId == product.ProductID))
                    .Sum(cp => cp.Quantity),
                TotalSales = "P" + _context.Orders
                    .Where(order => order.Products != null && order.Products.Any(cp => cp.ProductId == product.ProductID))
                    .SelectMany(order => order.Products.Where(cp => cp.ProductId == product.ProductID))
                    .Sum(cp => cp.TotalPrice)
            }).ToList();

            return DownloadExcel(salesPerProduct, "sales_per_product.xlsx");
        }

        public class SalesData
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public string? Sales { get; set; }
            public string? TotalSales { get; set; }
            public int NumberOfProductsSold { get; set; } // New property for the number of products sold per month
            public int TotalNumberOfProductsSold { get; set; } // New property for the total number of products sold
        }

        public IActionResult DownloadTotalSalesPerMonth()
        {
            var totalSalesPerMonth = _context.Orders
                .GroupBy(order => new { order.Date.Year, order.Date.Month })
                .Select(group => new
                {
                    Year = group.Key.Year,
                    Month = group.Key.Month,
                    TotalSales = group.Sum(order => order.TotalPrice),
                    NumberOfProductsSold = group.SelectMany(order => order.Products).Sum(cp => cp.Quantity)
                })
                .OrderBy(group => group.Year)
                .ThenBy(group => group.Month)
                .ToList();

            // Calculate total sales and total number of products sold
            decimal totalSales = totalSalesPerMonth.Sum(x => x.TotalSales);
            int totalNumberOfProductsSold = totalSalesPerMonth.Sum(x => x.NumberOfProductsSold);

            // Prepare data for Excel
            var salesData = totalSalesPerMonth.Select(item => new SalesData
            {
                Year = item.Year,
                Month = item.Month,
                Sales = "P" + item.TotalSales.ToString("N", CultureInfo.InvariantCulture),
                NumberOfProductsSold = item.NumberOfProductsSold,
                TotalNumberOfProductsSold = totalNumberOfProductsSold,
                TotalSales = ""
            }).ToList();

            // Add total sales and total number of products sold to the first row
            salesData[0].TotalSales = "P" + totalSales.ToString("N", CultureInfo.InvariantCulture);
            salesData[0].TotalNumberOfProductsSold = totalNumberOfProductsSold;

            return DownloadExcel(salesData, "total_sales_per_month.xlsx");
        }

        // Method to generate CSV file and download
        private IActionResult DownloadExcel<T>(IEnumerable<T> data, string fileName)
        {
            using (var memoryStream = new MemoryStream())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(memoryStream))
                {
                    var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                    worksheet.Cells.LoadFromCollection(data, true);

                    // Auto-fit columns
                    worksheet.Cells.AutoFitColumns();

                    package.Save();
                }

                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }



    }
}
