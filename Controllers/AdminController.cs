using BasicDotNetPostgresWebsite.Models;
using BasicDotNetPostgresWebsite.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;
using System.Threading.Tasks;
using BasicDotNetPostgresWebsite.Services; // Assuming a Services folder will be created

namespace BasicDotNetPostgresWebsite.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // Endpoint to add a new user
    [HttpPost("users")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StoreUserResponse>> AddUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var newUser = await _adminService.CreateUserAsync(request, ct);
        return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, newUser);
    }

    // Endpoint to add a new product
    [HttpPost("products")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> AddProduct([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var newProduct = await _adminService.CreateProductAsync(request, ct);
        return CreatedAtAction(nameof(GetProduct), new { id = newProduct.Id }, newProduct);
    }

    // Placeholder for getting a user by ID
    [HttpGet("users/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreUserResponse>> GetUser(int id, CancellationToken ct)
    {
        var user = await _adminService.GetUserByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    // Placeholder for getting a product by ID
    [HttpGet("products/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetProduct(int id, CancellationToken ct)
    {
        var product = await _adminService.GetProductByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }
}