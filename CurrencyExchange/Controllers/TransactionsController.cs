using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CurrencyExchange.Data;
using CurrencyExchange.Models;
using System.IO;
using CsvHelper;
using FixerSharp;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace CurrencyExchange.Controllers
{
    public class TransactionsController : Controller
    {

        private readonly ApplicationDbContext _context;
        List<char> delimiters = new List<char> { '|', ';', '-', ',' };
        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }


        // GET: Transactions
        public async Task<IActionResult> Index()
        {
            return View(await _context.Transactions.ToListAsync());
        }

        // GET: Transactions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // GET: Transactions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Transactions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Source_Currency,Destination_Currency,Source_Amount,Destination_Amount,FX_Rate")] Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                _context.Add(transaction);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(transaction);
        }

        // GET: Transactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }
            return View(transaction);
        }

        // POST: Transactions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Source_Currency,Destination_Currency,Source_Amount,Destination_Amount,FX_Rate")] Transaction transaction)
        {
            if (id != transaction.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(transaction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransactionExists(transaction.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(transaction);
        }

        // GET: Transactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        public async Task<IActionResult> FileUpload(ICollection<IFormFile> files)
        {
            var uploads = Path.Combine(Path.GetTempPath(), "uploads");
            Directory.CreateDirectory(uploads);
            
            foreach (IFormFile file in files)
            {               
                if (file.Length > 0)
                {                    
                    var filePath = Path.Combine(uploads, file.FileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    char csvDelimiter = GetDelimiter(file);
                    await ReadCSV(filePath, csvDelimiter);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.ID == id);
        }

        public async Task ReadCSV(string file, char delimiter)
        {
            using (var reader = new StreamReader(file))
            using (var csv = new CsvReader(reader, new CsvHelper.Configuration.Configuration
            {
                HasHeaderRecord = true,
                HeaderValidated = null,
                MissingFieldFound = null,
                Delimiter = delimiter.ToString()
            }))
            {
                var records = csv.GetRecords<dynamic>();
                foreach (IDictionary<String, Object> record in records)
                {
                    // get values from csv
                    Transaction trans = new Transaction(record.ElementAt(1).Value.ToString().Replace(" ", ""), record.ElementAt(2).Value.ToString().Replace(" ", ""), record.ElementAt(3).Value.ToString().Replace(" ", ""));
                    //execute the exchange
                    trans = trans.Exchange(trans);
                    // create record in DB
                    await Create(trans);
                }
            }
        }

        public List<string> FindFiles(string filePath, string format)
        {
            return Directory.EnumerateFiles(filePath, "*." + format).ToList();
        }
        
        public char GetDelimiter(IFormFile file)
        {
            StringBuilder result = new StringBuilder();
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (reader.Peek() >= 0)
                    result.AppendLine(reader.ReadLine());
            }
            foreach (char delimiter in delimiters)
            {
                if (result.ToString().Contains(delimiter))
                {
                    return delimiter;
                }
            }
            return ',';
        }
    }
}
