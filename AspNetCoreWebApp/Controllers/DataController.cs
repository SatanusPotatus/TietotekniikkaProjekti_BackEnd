using AspNetCoreWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/data")]
[ApiController]
public class DataController : ControllerBase
{
    private readonly AppDbContext _context;

    public DataController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/data
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DataModel>>> GetAll()
    {
        return await _context.Data.ToListAsync();
    }

    // GET: api/data/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<DataModel>> GetById(int id)
    {
        var item = await _context.Data.FindAsync(id);
        if (item == null) return NotFound();
        return item;
    }

    // POST: api/data
    [HttpPost]
    public async Task<ActionResult<DataModel>> Create(DataModel model)
    {
        _context.Data.Add(model);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
    }

    // PUT: api/data/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, DataModel model)
    {
        if (id != model.Id) return BadRequest();

        _context.Entry(model).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Data.Any(e => e.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    // DELETE: api/data/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Data.FindAsync(id);
        if (item == null) return NotFound();

        _context.Data.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
