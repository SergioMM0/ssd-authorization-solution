using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ssd_authorization_solution.DTOs;
using ssd_authorization_solution.Entities;

namespace MyApp.Namespace;

[Route("api/[controller]")]
[ApiController]
public class ArticleController : ControllerBase
{
    private readonly AppDbContext ctx;

    public ArticleController(AppDbContext ctx)
    {
        this.ctx = ctx;
    }

    [HttpGet]
    public IEnumerable<ArticleDto> Get()
    {
        return ctx.Articles.Include(x => x.Author).Select(ArticleDto.FromEntity);
    }

    [HttpGet(":id")]
    public ArticleDto? GetById(int id)
    {
        return ctx
            .Articles.Include(x => x.Author)
            .Where(x => x.Id == id)
            .Select(ArticleDto.FromEntity)
            .SingleOrDefault();
    }

    [HttpPost]
    [Authorize(Roles = "Editor, Writer")]
    public ArticleDto Post([FromBody] ArticleFormDto dto)
    {
        var userName = HttpContext.User.Identity?.Name;
        var author = ctx.Users.Single(x => x.UserName == userName);
        var entity = new Article
        {
            Title = dto.Title,
            Content = dto.Content,
            Author = author,
            CreatedAt = DateTime.Now
        };
        var created = ctx.Articles.Add(entity).Entity;
        ctx.SaveChanges();
        return ArticleDto.FromEntity(created);
    }

    [HttpPut(":id")]
    [Authorize(Roles = "Editor, Writer")]
    public ArticleDto Put(int id, [FromBody] ArticleFormDto dto)
    {
        var userName = HttpContext.User.Identity?.Name;
        var userRoles = HttpContext.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
        
        var entity = ctx
            .Articles
            .Include(x => x.Author)
            .Single(x => x.Id == id);
        
        // Only allow writers to edit their own articles
        if (userRoles.Contains("Writer") && entity.Author.UserName != userName)
        {
            throw new UnauthorizedAccessException("Writers can only edit their own articles.");
        }
        
        entity.Title = dto.Title;
        entity.Content = dto.Content;
        var updated = ctx.Articles.Update(entity).Entity;
        ctx.SaveChanges();
        return ArticleDto.FromEntity(updated);
    }

    [HttpDelete(":id")]
    [Authorize(Roles = "Editor")]
    public void Delete(int id) {
        var entity = ctx.Articles.Single(x => x.Id == id);
        ctx.Articles.Remove(entity);
        ctx.SaveChanges();
    }
}