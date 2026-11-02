using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using BasicDotNetPostgresWebsite.Models;

namespace BasicDotNetPostgresWebsite.DTOs;

/// <summary>Payload for creating a new user in the admin portal.</summary>
public sealed record CreateUserRequest
{
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string Email { get; init; }

    [Required]
    [MinLength(8)]
    public required string Password { get; init; }

    [Required]
    public required string Address { get; init; }
}

/// <summary>Payload for creating a new product in the admin portal.</summary>
public sealed record CreateProductRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [Required]
    [Range(0.01, 999999.99)]
    public required decimal Price { get; init; }

    [Required]
    public required decimal OriginalPrice { get; init; }

    [Required]
    [Range(1, 1000)]
    public required int StockQuantity { get; init; }

    [Required]
    public required int CategoryId { get; init; }

    [Required]
    public required string ImageUrl { get; init; }

    [Required]
    public required string Unit { get; init; }
}

/// <summary>Response model for a StoreUser.</summary>
public sealed record StoreUserResponse(
    int Id,
    string Name,
    string Email,
    bool IsAdmin,
    DateTimeOffset CreatedAt);

/// <summary>Response model for a Product.</summary>
public sealed record ProductResponse(
    int Id,
    string Name,
    decimal Price,
    Category Category,
    bool IsAvailable,
    DateTimeOffset CreatedAt);