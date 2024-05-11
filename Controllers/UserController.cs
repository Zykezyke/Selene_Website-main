using SELENE_STUDIO.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using SELENE_STUDIO.Data;
using SELENE_STUDIO.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using System.Security.Claims;

namespace SELENE_STUDIO.Controllers {
    public class UserController : Controller {
        private readonly ILogger<UserController> _logger;
        private readonly UserManager<RegUser> _userManager;
        private readonly LogAppDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public UserController(LogAppDbContext context, ILogger<UserController> logger, UserManager<RegUser> userManager, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Home()
        {
            // Retrieve all products from the database
            var allProducts = _context.Products.Where(p => !p.isHidden).ToArray();

            // Calculate total price sold of each product
            var totalPriceSoldOfEachProduct = _context.Orders
                .Where(order => order.OrderStatus == OrderStatus.Shipped)
                .SelectMany(order => order.Products)
                .GroupBy(product => product.ProductId)
                .Select(group => new {
                    ProductID = group.Key,
                    TotalPriceSold = group.Sum(product => product.Quantity * product.TotalPrice)
                })
                .ToDictionary(x => x.ProductID, x => x.TotalPriceSold);

            // Order products by total price sold, descending
            var orderedProducts = allProducts
                .OrderByDescending(p => totalPriceSoldOfEachProduct.ContainsKey(p.ProductID) ? totalPriceSoldOfEachProduct[p.ProductID] : 0)
                .ToList();

            // Take top 4 products
            var topProducts = orderedProducts.Take(3).ToList();
            var featuredFeedbacks = _context.Feedbacks
            .Include(f => f.User)
            .Where(f => f.IsFeatured)
            .ToList();


            var viewModel = new HomeViewModel
            {
                TopProducts = topProducts,
                FeaturedFeedbacks = featuredFeedbacks
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Category1Async(int page = 1)
        {
            // Get the current logged-in user
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser != null)
            {
                // Set the ViewBag.CurrentUser with the current user's session start and end times
                ViewBag.CurrentUser = new { SessionStartTime = currentUser.SessionStartTime, SessionEndTime = currentUser.SessionEndTime };
            }

            // Number of products per page
            int pageSize = 9;

            // Get the total count of products
            var totalProducts = _context.Products.Count(p => !p.isHidden);

            // Calculate total pages
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            // Apply pagination
            var products = _context.Products
                                .Where(p => !p.isHidden)
                                .OrderBy(p => p.ProductID)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToArray();

            // Set ViewBag properties for pagination
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            // Return the view with paginated products
            return View(products);
        }



        public IActionResult Orders(int page = 1, int pageSize = 5)
        {
            var user = _userManager.GetUserAsync(User).Result;

            // Calculate the number of records to skip
            var skipAmount = (page - 1) * pageSize;

            // Retrieve orders for the current user with pagination and excluding deleted orders
            var orders = _context.Orders
                                 .Include(order => order.Products)
                                 .ThenInclude(product => product.Product)
                                 .Where(order => order.Customer.Id == user.Id && !order.IsDeleted) // Exclude deleted orders
                                 .OrderByDescending(order => order.Date) // Optional: Order by date, or any other suitable criteria
                                 .Skip(skipAmount)
                                 .Take(pageSize)
                                 .ToList();

            // Calculate total number of pages excluding deleted orders
            var totalOrders = _context.Orders.Count(order => order.Customer.Id == user.Id && !order.IsDeleted);
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            // Pass pagination data to the view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(orders);
        }


        public async Task<IActionResult> Contact()
        {
            var user = await _userManager.GetUserAsync(User);
            var model = new RegisterViewModel
            {
                FirstName = user.FirstName, // Assuming you have these properties in your ApplicationUser model
                LastName = user.LastName,
                Contact = user.PhoneNumber,
                Email = user.Email
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult InitiateConversation(int adminId)
        {
            // Check if the selected admin exists
            var admin = _context.Admins.FirstOrDefault(a => a.AdminId == adminId);
            if (admin == null)
            {
                TempData["ErrorMessage"] = "Admin not found."; // Store error message in TempData
                return RedirectToAction("ViewConversation", "User"); // Redirect to home or any appropriate action
            }

            // Get the current user
            var currentUser = _userManager.GetUserAsync(User).Result;

            // Check if the user has initiated conversations within the rate limit period
            var lastInitiatedConversations = _context.Conversations
                .Where(c => c.InitiatorId == currentUser.Id && c.CreatedAt >= DateTime.UtcNow.AddDays(-1)) // Limit to conversations initiated within the last hour
                .ToList();

            // Check if the user has exceeded the rate limit
            if (lastInitiatedConversations.Count >= 5) // Example rate limit: 10 conversations per minute
            {
                TempData["ErrorMessage"] = "Rate limit exceeded. Please try again later."; // Store error message in TempData
                return RedirectToAction("ViewConversation", "User"); // Redirect to home or any appropriate action
            }

            // Create a new conversation
            var conversation = new Conversation
            {
                InitiatorId = currentUser.Id,
                ReceiverId = adminId.ToString(), // Convert adminId to string
                IsActive = true,
                CreatedAt = DateTime.UtcNow // Set the CreatedAt property to the current timestamp
            };

            _context.Conversations.Add(conversation);
            _context.SaveChanges();

            // Redirect to view conversation action with the conversation id
            return RedirectToAction("SendMessage", "User", new { id = conversation.ConversationId });
        }


        public IActionResult ViewConversation(int page = 1, int pageSize = 5)
        {
            // Get the current user
            var currentUser = _userManager.GetUserAsync(User).Result;

            // Retrieve total count of conversations involving the current user
            var totalConversationsCount = _context.Conversations
                .Where(c => c.InitiatorId == currentUser.Id || c.ReceiverId == currentUser.Id)
                .Count();

            // Calculate total pages
            var totalPages = (int)Math.Ceiling((double)totalConversationsCount / pageSize);

            // Retrieve conversations for the current page
            var conversations = _context.Conversations
                .Where(c => c.InitiatorId == currentUser.Id || c.ReceiverId == currentUser.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Retrieve a list of admins
            var admins = _context.Admins.ToList();

            // Pass necessary data to the view
            ViewBag.Conversations = conversations;
            ViewBag.Admins = admins;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

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

                var currentTime = DateTime.UtcNow;
                var messagesWithinLastMinute = _context.Messages
                    .Count(m => m.SenderId == currentUser.Id && m.CreatedAt >= currentTime.AddMinutes(-1));

                if (messagesWithinLastMinute >= 10)
                {
                    return BadRequest("You have reached the maximum limit for sending messages. Please try again later.");
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

                // Return success response
                return Ok(new { success = true });
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
            var currentUser = _userManager.GetUserAsync(User).Result;

            // Mark incoming messages as read (except user's own messages)
            var messages = _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .Select(m => new {
                    m.MessageId,
                    m.Content,
                    m.ImageUrl,
                    m.IsRead,
                    SenderName = m.SenderId == currentUser.Id ? "You" : "Admin" // Assuming Admin is the other participant
                })
                .ToList();

            // Update isRead property for incoming messages (except user's own messages)
            foreach (var message in messages)
            {
                if (message.SenderName == "Admin" && !message.IsRead)
                {
                    var messageToUpdate = _context.Messages.Find(message.MessageId);
                    messageToUpdate.IsRead = true;
                }
            }
            _context.SaveChanges();

            return Json(new { messages });
        }



        public IActionResult About() {
            return View();
        }

        public async Task<IActionResult> Cart() {
            var user = _userManager.GetUserAsync(User).Result;

            var cart = await _context.UserCarts
                             .Include(cart => cart.Products)
                             .ThenInclude(cartProduct => cartProduct.Product)
                             .ThenInclude(product => product.AdditionalPrices) 
                             .Include(cart => cart.Customer)
                             .FirstOrDefaultAsync(cart => cart.Customer.Id == user.Id);


            if (cart == null) {
                cart = new UserCart {
                    Customer = user,
                    Products = new List<CartProduct>()
                };
                _context.UserCarts.Add(cart);
                await _context.SaveChangesAsync();
            }
            
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart()
        {
            var user = await _userManager.GetUserAsync(User);
            var productId = Convert.ToInt32(Request.Form["productId"]);
            var count = Convert.ToInt32(Request.Form["count"]);
            var selectedSize = Request.Form["size"];

            // Retrieve product and check if it exists
            var product = await _context.Products
                .Include(p => p.AdditionalPrices) // Include AdditionalPrices
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null)
            {
                return NotFound();
            }

            // Find the additional price for the selected size
            var additionalPrice = product.AdditionalPrices.FirstOrDefault(ap => ap.Size == selectedSize)?.Price ?? 0;

            // Calculate the total price including the additional price
            var totalPrice = additionalPrice;

            // Retrieve or create cart for the user
            var cart = await _context.UserCarts
                .Include(cart => cart.Products)
                .ThenInclude(cartProduct => cartProduct.Product)
                .Include(cart => cart.Customer)
                .FirstOrDefaultAsync(cart => cart.Customer.Id == user.Id);

            if (cart == null)
            {
                cart = new UserCart
                {
                    Customer = user,
                    Products = new List<CartProduct>()
                };
                _context.UserCarts.Add(cart);
            }

            // Calculate the total quantity across all sizes for the selected product in the cart
            var totalQuantity = cart.Products
                .Where(cp => cp.Product.ProductID == productId)
                .Sum(cp => cp.Quantity);


            // Check if the product with the selected size already exists in the cart
            var existingProduct = cart.Products.FirstOrDefault(cp => cp.Product.ProductID == productId && cp.SelectedSize == selectedSize);

            if (existingProduct != null)
            {
                existingProduct.Quantity += count;
            }
            else
            {
                // Add product with selected size to cart
                CartProduct cartProduct = new CartProduct
                {
                    Product = product,
                    Quantity = count,
                    SelectedSize = selectedSize,
                    TotalPrice = totalPrice // Save the total price including the additional price
                };
                cart.Products.Add(cartProduct);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Cart");
        }

        public async Task<IActionResult> RemoveFromCart(int id) {
            var user = _userManager.GetUserAsync(User).Result;
            var product = await _context.Products.FindAsync(id);
            
            if (product == null) {
                return NotFound();
            }
            
            var cart = await _context.UserCarts
                                     .Include(cart => cart.Products).ThenInclude(cartProduct => cartProduct.Product)
                                     .Include(cart => cart.Customer)
                                     .FirstOrDefaultAsync(cart => cart.Customer.Id == user.Id);
            if (cart == null) {
                return RedirectToAction("Cart");
            }
            
            if (cart.Products.Exists(cp => cp.Product.ProductID == product.ProductID)) {
                cart.Products.Remove(cart.Products.First(p => p.Product.ProductID == product.ProductID));
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Cart");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var productId = Convert.ToInt32(Request.Form["productId"]);
                var count = Convert.ToInt32(Request.Form["quantity"]);

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return NotFound();
                }

                var cart = await _context.UserCarts
                    .Include(cart => cart.Products)
                    .ThenInclude(cartProduct => cartProduct.Product)
                    .Include(cart => cart.Customer)
                    .FirstOrDefaultAsync(cart => cart.Customer.Id == user.Id);

                if (cart == null)
                {
                    return RedirectToAction("Cart");
                }

                // Check if the product exists in the cart
                var existingProduct = cart.Products.FirstOrDefault(cp => cp.ProductId == productId);

                if (existingProduct != null)
                {
                    // If the product is a custom product, update its quantity directly
                    if (!string.IsNullOrEmpty(existingProduct.CustomDescription) && !string.IsNullOrEmpty(existingProduct.CustomImagePath))
                    {
                        existingProduct.Quantity = count;
                    }
                    else // Otherwise, update the quantity of the associated product
                    {
                        existingProduct.Quantity = count;
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                TempData["ErrorMessage"] = "An error occurred while processing your request.";
                return RedirectToAction("Cart");
            }
        }


        public IActionResult ClearCart() {
            var user = _userManager.GetUserAsync(User).Result;
            var cart = _context.UserCarts
                               .Include(cart => cart.Products)
                               .Include(cart => cart.Customer)
                               .FirstOrDefault(cart => cart.Customer.Id == user.Id);
            if (cart == null) {
                return RedirectToAction("Cart");
            }
            cart.Products.Clear();
            _context.SaveChanges();
            return RedirectToAction("Cart");
        }



        public IActionResult Checkout()
        {
            // Retrieve the current user
            var user = _userManager.GetUserAsync(User).Result;

            // Retrieve the user's cart along with products and additional prices
            var userCart = _context.UserCarts
                                    .Include(cart => cart.Products).ThenInclude(cartProduct => cartProduct.Product).ThenInclude(product => product.AdditionalPrices)
                                    .Include(cart => cart.Customer)
                                    .FirstOrDefault(cart => cart.Customer.Id == user.Id);

            // Pre-fill the address field with the user's address
            ViewBag.UserAddress = user.Address; // Assuming user's address property name is 'Address'

            return View(userCart);
        }

        [HttpPost]
        public IActionResult OrderConfirm()
        {
            var user = _userManager.GetUserAsync(User).Result;
            var cart = _context.UserCarts
                               .Include(cart => cart.Products).ThenInclude(cartProduct => cartProduct.Product).ThenInclude(product => product.AdditionalPrices)
                               .Include(cart => cart.Customer)
                               .FirstOrDefault(cart => cart.Customer.Id == user.Id);
            if (cart == null)
            {
                return RedirectToAction("Cart");
            }

            // Calculate the total price of the order including additional prices
            decimal totalPrice = cart.Products.Sum(cp =>
            {
                var additionalPrice = cp.Product.AdditionalPrices.FirstOrDefault(ap => ap.Size == cp.SelectedSize)?.Price ?? 0;
                return (additionalPrice) * cp.Quantity;
            });

            Order order = new Order
            {
                Customer = user,
                Products = cart.Products,
                Date = DateTime.Now,
                OrderStatus = OrderStatus.Pending,
                TotalPrice = totalPrice,
                DeliveryAddress = Request.Form["address"],
                PaymentMethod = Request.Form["payment"],
                DeliveryMode = Request.Form["delivery"],
                PaymentStatus = PaymentStatus.Pending,
                IsDeleted = false,
            };
            _context.Entry(cart).State = EntityState.Detached;
            _context.Orders.Add(order);
            _context.UserCarts.Remove(cart);
            _context.SaveChanges();

            return View(order);
        }

        [HttpPost]
        public IActionResult CancelOrder(int orderId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderNumber == orderId);
            if (order == null)
            {
                return NotFound(); // Return 404 Not Found if the order is not found
            }

            // Only allow cancellation if the order status is "Processing"
            if (order.OrderStatus == OrderStatus.Pending)
            {
                order.OrderStatus = OrderStatus.Cancelled;
                _context.SaveChanges();
                TempData["CancelMessage"] = "Order cancelled successfully.";
            }
            else
            {
                TempData["CancelMessage"] = "Order cannot be cancelled as it is not in Processing status.";
            }

            return RedirectToAction("Orders");
        }

        [HttpPost]
        public IActionResult DeleteOrder(int orderId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderNumber == orderId);
            if (order == null)
            {
                return NotFound(); // Return 404 Not Found if the order is not found
            }

            // Only allow deletion if the order status is "Completed" or "Cancelled"
            if (order.OrderStatus == OrderStatus.Completed || order.OrderStatus == OrderStatus.Cancelled)
            {
                // You can add a property to the Order model to mark it as deleted
                // For example, set the IsDeleted property to true
                order.IsDeleted = true;

                _context.SaveChanges();
                TempData["DeleteMessage"] = "Order deleted successfully.";
            }
            else
            {
                TempData["DeleteMessage"] = "Order cannot be deleted as it is not in Completed or Cancelled status.";
            }

            return RedirectToAction("Orders");
        }


        public IActionResult OrderDetails(int id)
        {
            var user = _userManager.GetUserAsync(User).Result;
            var order = _context.Orders
                .Include(order => order.Products).ThenInclude(cartProduct => cartProduct.Product).ThenInclude(product => product.AdditionalPrices)
                .Include(order => order.Customer)
                .FirstOrDefault(order => order.Customer.Id == user.Id && order.OrderNumber == id);

            if (order == null)
            {
                return RedirectToAction("Orders");
            }

            // Update OldPaymentStatus and OldOrderStatus to the current ones
            order.OldPaymentStatus = order.PaymentStatus;
            order.OldOrderStatus = order.OrderStatus;

            // Save changes to the database
            _context.SaveChanges();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile image, int orderId)
        {
            try
            {
                var order = _context.Orders.FirstOrDefault(o => o.OrderNumber == orderId);
                if (order == null)
                {
                    return NotFound(); // Return 404 Not Found if the order is not found
                }

                // Check if an image file is uploaded
                if (image != null && image.Length > 0)
                {
                    // Check file size (limit to 3 MB)
                    if (image.Length > 3 * 1024 * 1024)
                    {
                        TempData["UploadMessage"] = "The uploaded image must be 3 MB or less.";
                        return RedirectToAction("Orders");
                    }

                    // Check file format (allow only image formats)
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(image.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["UploadMessage"] = "Only JPG, JPEG, PNG, and GIF formats are allowed.";
                        return RedirectToAction("Orders");
                    }

                    // Handle image upload
                    var fileName = Guid.NewGuid().ToString() + fileExtension;
                    var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    order.UploadedImagePath = "/uploads/" + fileName; // Update the path in the database
                    order.OldPaymentStatus = order.PaymentStatus;
                    order.OldOrderStatus = order.OrderStatus;
                    _context.SaveChanges();

                    TempData["UploadMessage"] = "Image uploaded successfully.";
                }

                return RedirectToAction("Orders");
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                TempData["UploadMessage"] = "An error occurred while uploading the image.";
                return StatusCode(500); // Return 500 Internal Server Error if an unexpected error occurs
            }
        }


        public IActionResult UserProfile() {
            var user = _userManager.GetUserAsync(User).Result;

            return View(user);
        }

        [HttpGet]
        public IActionResult UpdateUserProfile()
        {
            var user = _userManager.GetUserAsync(User).Result;

            var viewModel = new UpdateViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Contact = user.PhoneNumber,
                Address = GetAddress(user.Address),
                PostalCode = GetPostalCode(user.Address), 
                Email = user.Email
            };

            return View(viewModel);
        }

        private string GetAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return string.Empty;

            var parts = address.Split(new[] { ", Postal Code: " }, StringSplitOptions.None);
            return parts[0]; // First part contains the address without postal code
        }
        private string GetPostalCode(string address)
        {
            if (string.IsNullOrEmpty(address))
                return string.Empty;

            var parts = address.Split(new[] { ", Postal Code: " }, StringSplitOptions.None);
            return parts.Length > 1 ? parts[1] : string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserProfile(UpdateViewModel updatedUser)
        {
            if (!ModelState.IsValid)
            {
                // ModelState is invalid, return the view with validation errors
                return View(updatedUser);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Split postal code from the address if it exists
            var addressParts = updatedUser.Address?.Split(new[] { ", Postal Code: " }, StringSplitOptions.None);
            var address = addressParts?.FirstOrDefault() ?? updatedUser.Address;
            var postalCode = addressParts?.Skip(1).FirstOrDefault() ?? updatedUser.PostalCode;

            // Update user properties
            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.PhoneNumber = updatedUser.Contact;
            user.Address = $"{address}, Postal Code: {postalCode}";
            user.Email = updatedUser.Email;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _context.SaveChangesAsync();
                TempData["messages"] = "User details updated successfully.";
                return RedirectToAction("UserProfile");
            }
            else
            {
                // If the update failed, you might want to handle the error appropriately
                // For example, you can add model errors to display validation errors.
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(updatedUser);
            }
        }



        public async Task<IActionResult> ViewProduct(int id, int page = 1, int pageSize = 5)
        {
            var product = await _context.Products
                .Include(p => p.AdditionalPrices)
                .Include(p => p.Feedbacks)
                    .ThenInclude(f => f.User)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            // Check if the user is authenticated
            if (User.Identity.IsAuthenticated)
            {
                // Get the current logged-in user
                var currentUser = await _userManager.GetUserAsync(User);

                // Update session start or end time based on the product's DateCreated
                if (product.DateCreated > currentUser.SessionEndTime && product.DateCreated < currentUser.SessionStartTime)
                {
                    // Update session start time
                    currentUser.SessionStartTime = product.DateCreated;
                    currentUser.SessionEndTime = product.DateCreated;
                }

                // Save changes to the database
                await _context.SaveChangesAsync();

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.CanLeaveFeedback = await _context.Orders
                    .AnyAsync(o => o.Products.Any(p => p.ProductId == id && o.Customer.Id == userId && o.OrderStatus == OrderStatus.Completed));
            }
            else
            {
                ViewBag.CanLeaveFeedback = false;
            }

            // Paginate feedbacks
            var totalFeedbacks = product.Feedbacks.Count();
            var feedbacks = product.Feedbacks.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Feedbacks = feedbacks;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalFeedbacks / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            return View(product);
        }





        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult AddFeedback(int productId, string content, int rating)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account"); // Redirect to login if user is not authenticated
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orderExists = _context.Orders.Any(o => o.Products.Any(p => p.ProductId == productId && o.Customer.Id == userId && o.OrderStatus == OrderStatus.Completed));

            if (!orderExists)
            {
                // Redirect or display error message indicating that the user cannot leave feedback for this product
                return RedirectToAction("ViewProduct", new { id = productId });
            }

            // Check if the user has posted feedback within the rate limit period
            var lastPostedFeedbacks = _context.Feedbacks
                .Where(f => f.UserId == userId && f.Date >= DateTime.UtcNow.AddMinutes(-1)) // Limit to feedbacks posted within the last minute
                .ToList();

            // Check if the user has exceeded the rate limit
            if (lastPostedFeedbacks.Count >= 5) // Example rate limit: 5 feedbacks per minute
            {
                // Handle rate limit exceeded
                return BadRequest("Rate limit exceeded. Please try again later.");
            }

            var feedback = new Feedback
            {
                ProductId = productId,
                UserId = userId,
                Content = content.Replace("\r\n", "<br>").Replace("\n", "<br>"),
                Rating = rating,
                Date = DateTime.UtcNow, // Set the Date property to the current timestamp
                IsFeatured = false,
            };

            _context.Feedbacks.Add(feedback);
            _context.SaveChanges();

            return RedirectToAction("ViewProduct", new { id = productId });
        }



        [HttpPost]
        public async Task<IActionResult> AddCustomProduct(IFormCollection form, IFormFile customImage)
        {
            try
            {
                // Retrieve data from the form
                var productId = Convert.ToInt32(form["productId"]);
                var customDescription = form["customDescription"].ToString();
                var selectedSize = Request.Form["size"];

                // Retrieve product details
                var product = await _context.Products
                    .Include(p => p.AdditionalPrices)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);

                if (product == null)
                {
                    return NotFound();
                }

                // Find the additional price for the selected size
                var additionalPrice = product.AdditionalPrices.FirstOrDefault(ap => ap.Size == selectedSize)?.Price ?? 0;

                // Calculate the total price including the additional price
                var totalPrice = additionalPrice;
                var customImagePath = "";

                if (customImage != null && customImage.Length > 0)
                {
                    if (customImage.Length <= 3 * 1024 * 1024) // 3MB in bytes
                    {
                        // Define the directory where custom images will be stored
                        var uploadsDirectory = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");

                        // Ensure the directory exists; if not, create it
                        if (!Directory.Exists(uploadsDirectory))
                        {
                            Directory.CreateDirectory(uploadsDirectory);
                        }

                        // Generate a unique file name for the custom image
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(customImage.FileName);
                        var filePath = Path.Combine(uploadsDirectory, fileName);

                        // Save the uploaded file to the specified path
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await customImage.CopyToAsync(stream);
                        }

                        // Store the file path as a string
                        customImagePath = "/uploads/" + fileName; // Assuming 'uploads' is the folder in wwwroot where custom images are stored
                    }
                    else
                    {
         
                        return RedirectToAction(nameof(AddCustomProduct));
                    }
                }

                // Create a new CartProduct instance for the customized product
                var cartProduct = new CartProduct
                {
                    Product = product, // Reference to the original product
                    Quantity = 1, // Default quantity
                    SelectedSize = selectedSize, // Size selected by the user
                    TotalPrice = totalPrice, // Use product price as total price for now
                    CustomDescription = customDescription,
                    CustomImagePath = customImagePath, // Assign the file path to CustomImagePath
                };

                // Add the custom product to the cart
                var user = await _userManager.GetUserAsync(User);
                var cart = await _context.UserCarts
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Customer.Id == user.Id);

                if (cart == null)
                {
                    cart = new UserCart
                    {
                        Customer = user,
                        Products = new List<CartProduct>()
                    };
                    _context.UserCarts.Add(cart);
                }

                // Add the customized product to the cart
                cart.Products.Add(cartProduct);

                await _context.SaveChangesAsync();

             // Inform the user of successful addition

                // No need to redirect here as per your requirement
                // Redirect to cart page or wherever you want
                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                TempData["ErrorMessage"] = "An error occurred while processing your request.";
                return RedirectToAction(nameof(AddCustomProduct));
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetNotificationsAsync()
        {
            // Get the current logged-in user
            var currentUser = await _userManager.GetUserAsync(User);

            // Count new messages
            int newMessagesCount = await _context.Messages
                .Where(m => m.SenderId == "1" && !m.IsRead && m.Conversation.InitiatorId == currentUser.Id)
                .CountAsync();

            // Count new products
            int newProductsCount = await _context.Products
                .Where(p => p.DateCreated > currentUser.SessionEndTime && p.DateCreated < currentUser.SessionStartTime)
                .CountAsync();

            // Count orders with changed payment status or order status
            int changedOrdersCount = await _context.Orders
       .Where(o => o.Customer.Id == currentUser.Id &&
                   (o.OldPaymentStatus != o.PaymentStatus || o.OldOrderStatus != o.OrderStatus) &&
                   !o.IsDeleted) // Exclude deleted orders
       .CountAsync();

            return Json(new { NewMessagesCount = newMessagesCount, NewProductsCount = newProductsCount, ChangedOrdersCount = changedOrdersCount });
        }

    }
}